<#
.SYNOPSIS
  Runs the deployed GraphQL API tests (DeployedApiTests) against a non-production stage as throwaway Cognito users.
.DESCRIPTION
  Decision D18. Creates one test user per role group plus one without a group, signs them in through the
  non-production API test client (ADMIN_USER_PASSWORD_AUTH), runs the tests and always deletes the users.
  Passwords and tokens are random, never printed and never passed on a command line: AWS CLI input goes through
  temporary JSON files and tokens reach the test process through process-scoped environment variables.
  With -Deploy, publishes the API Lambda and runs `cdk deploy --all` for the stage first.
#>
param(
    [string]$Stage = 'dev',
    [string]$Region = 'us-east-1',
    [switch]$Deploy
)
$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$cdkDir = Join-Path $repoRoot 'infra\Amendment.Cdk'
$env:AWS_REGION = $Region

$stages = Get-Content (Join-Path $cdkDir 'Config\stages.json') -Raw | ConvertFrom-Json
if (-not $stages.$Stage) { throw "Unknown stage '$Stage'." }
if ($stages.$Stage.isProduction) { throw "Refusing to create test users in production stage '$Stage'." }
$prefix = "Amendment-$Stage"

function Invoke-Checked([scriptblock]$Command) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "Command failed with exit code ${LASTEXITCODE}: $Command" }
}

# Runs an AWS CLI command whose input contains a secret, passing the input through a temporary file.
function Invoke-AwsWithInput([string[]]$Command, [hashtable]$Body, [string[]]$Extra = @()) {
    $file = Join-Path ([IO.Path]::GetTempPath()) "amendment-$([guid]::NewGuid().ToString('N')).json"
    try {
        $Body | ConvertTo-Json -Depth 5 | Set-Content -Path $file -Encoding ascii
        $out = aws @Command --cli-input-json "file://$file" @Extra
        if ($LASTEXITCODE -ne 0) { throw "aws $($Command -join ' ') failed." }
        $out
    }
    finally { Remove-Item $file -Force -ErrorAction SilentlyContinue }
}

function Get-Output([string]$StackName, [string]$Key) {
    $value = aws cloudformation describe-stacks --stack-name $StackName --query "Stacks[0].Outputs[?OutputKey=='$Key'].OutputValue" --output text
    if ($LASTEXITCODE -ne 0 -or -not $value -or $value -eq 'None') { throw "Output $Key not found on $StackName." }
    $value
}

function New-TestPassword {
    $bytes = [byte[]]::new(24)
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    'Aa1-' + [Convert]::ToBase64String($bytes).Replace('+', 'x').Replace('/', 'y').TrimEnd('=')
}

if ($Deploy) {
    Invoke-Checked { dotnet publish (Join-Path $repoRoot 'src\Amendment.Api.Lambda\Amendment.Api.Lambda.csproj') -c Release -o (Join-Path $repoRoot 'artifacts\lambda\api') --nologo -v q }
    Push-Location $cdkDir
    try { Invoke-Checked { npx cdk deploy --all -c "stage=$Stage" --require-approval never } }
    finally { Pop-Location }
}

$userPoolId = Get-Output "$prefix-Identity" 'UserPoolId'
$clientId = Get-Output "$prefix-Identity" 'TestClientId'
$graphqlUrl = Get-Output "$prefix-Api" 'GraphqlUrl'

# Token variable suffix (read by DeployedApiTests) => Cognito group ('' = signed in without a role).
$users = [ordered]@{ ADMIN = 'SystemAdministrator'; EDITOR = 'AmendmentEditor'; TRANSLATOR = 'Translator'; SCREEN = 'ScreenController'; NOROLE = '' }
$runId = [guid]::NewGuid().ToString('N').Substring(0, 8)
$created = @()
$tokenVars = @()
try {
    foreach ($role in $users.Keys) {
        $username = "api-test-$($role.ToLowerInvariant())-$runId"
        Invoke-Checked { aws cognito-idp admin-create-user --user-pool-id $userPoolId --username $username --message-action SUPPRESS --output text --query User.Username | Out-Null }
        $created += $username
        $password = New-TestPassword
        Invoke-AwsWithInput @('cognito-idp', 'admin-set-user-password') @{ UserPoolId = $userPoolId; Username = $username; Password = $password; Permanent = $true } | Out-Null
        if ($users[$role]) {
            Invoke-Checked { aws cognito-idp admin-add-user-to-group --user-pool-id $userPoolId --username $username --group-name $users[$role] }
        }
        # Access token, as sent by the Blazor client (CognitoAccessTokenHandler).
        $token = Invoke-AwsWithInput @('cognito-idp', 'admin-initiate-auth') @{
            UserPoolId = $userPoolId; ClientId = $clientId; AuthFlow = 'ADMIN_USER_PASSWORD_AUTH'
            AuthParameters = @{ USERNAME = $username; PASSWORD = $password }
        } @('--query', 'AuthenticationResult.AccessToken', '--output', 'text')
        $password = $null
        $var = "AMENDMENT_API_TEST_TOKEN_$role"
        [Environment]::SetEnvironmentVariable($var, $token, 'Process')
        $tokenVars += $var
        Write-Host "  created test user $username ($(if ($users[$role]) { $users[$role] } else { 'no group' }))"
    }
    # The public projector token (decision D21) is not a secret; it ships in the web app.
    [Environment]::SetEnvironmentVariable('AMENDMENT_API_TEST_TOKEN_PUBLIC', (Get-Output "$prefix-Api" 'PublicToken'), 'Process')
    $tokenVars += 'AMENDMENT_API_TEST_TOKEN_PUBLIC'

    $env:AMENDMENT_API_TEST_URL = $graphqlUrl
    Invoke-Checked { dotnet test (Join-Path $repoRoot 'test\Amendment.Infrastructure.Tests\Amendment.Infrastructure.Tests.csproj') -c Release --filter 'FullyQualifiedName~DeployedApiTests' --nologo }
    Write-Host "Deployed API tests passed for stage '$Stage'." -ForegroundColor Green
}
finally {
    foreach ($var in $tokenVars + 'AMENDMENT_API_TEST_URL') { [Environment]::SetEnvironmentVariable($var, $null, 'Process') }
    foreach ($username in $created) {
        aws cognito-idp admin-delete-user --user-pool-id $userPoolId --username $username
        if ($LASTEXITCODE -eq 0) { Write-Host "  deleted test user $username" }
        else { Write-Warning "Could not delete test user $username; delete it manually." }
    }
}
