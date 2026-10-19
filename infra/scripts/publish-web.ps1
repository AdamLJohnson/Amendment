<#
.SYNOPSIS
  Publishes the Blazor WebAssembly client to a stage's S3 bucket and invalidates CloudFront.
.DESCRIPTION
  Reads stack outputs written by `cdk deploy --outputs-file`, generates the nonsecret frontend config
  (wwwroot/amendment-config.json), uploads the static files and invalidates the distribution.
  Uses the ambient AWS credentials/profile (or AWS_ENDPOINT_URL for Floci).
#>
param(
    [string]$Stage = 'dev',
    [string]$Region = 'us-east-1',
    [string]$OutputsFile = (Join-Path $PSScriptRoot "..\Amendment.Cdk\cdk-outputs-$Stage.json"),
    [string]$PublishDir = (Join-Path $PSScriptRoot "..\..\artifacts\web-$Stage"),
    [switch]$SkipInvalidation
)
$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')

function Invoke-Checked([scriptblock]$Command) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "Command failed with exit code ${LASTEXITCODE}: $Command" }
}

$outputs = Get-Content $OutputsFile -Raw | ConvertFrom-Json
$prefix = "Amendment-$Stage"
$web = $outputs."$prefix-Web"
$identity = $outputs."$prefix-Identity"
$api = $outputs."$prefix-Api"
if (-not $web -or -not $identity -or -not $api) { throw "Outputs for $prefix-Web/Identity/Api not found in $OutputsFile." }

# Publish from clean Release outputs into an empty folder; the folder is synced to S3 as-is.
foreach ($project in 'Amendment.Client', 'Amendment.Client.Repository', 'Amendment.Shared') {
    foreach ($dir in 'bin', 'obj') {
        $path = Join-Path $repoRoot "src\$project\$dir\Release"
        if (Test-Path $path) { Remove-Item $path -Recurse -Force }
    }
}
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
Invoke-Checked { dotnet publish (Join-Path $repoRoot 'src\Amendment.Client\Amendment.Client.csproj') -c Release -o $PublishDir --nologo -v q }
$wwwroot = Join-Path $PublishDir 'wwwroot'

$frontendConfig = [ordered]@{
    stage            = $Stage
    region           = $Region
    siteUrl          = $web.SiteUrl
    graphqlUrl       = $api.GraphqlUrl
    userPoolId       = $identity.UserPoolId
    userPoolClientId = $identity.UserPoolClientId
    signInDomain     = $identity.SignInDomainName
    publicToken      = $api.PublicToken
}
[IO.File]::WriteAllText((Join-Path $wwwroot 'amendment-config.json'), ($frontendConfig | ConvertTo-Json))
Write-Host "Wrote amendment-config.json for stage '$Stage'."

$bucket = "s3://$($web.SiteBucketName)"
Invoke-Checked { aws s3 sync $wwwroot $bucket --delete --exclude '*.br' --exclude '*.gz' --cache-control 'no-cache' --only-show-errors }
Invoke-Checked { aws s3 cp $wwwroot $bucket --recursive --exclude '*' --include '*.wasm' --content-type 'application/wasm' --cache-control 'no-cache' --metadata-directive REPLACE --only-show-errors }
Write-Host "Uploaded $wwwroot to $bucket."

if (-not $SkipInvalidation) {
    Invoke-Checked { aws cloudfront create-invalidation --distribution-id $web.DistributionId --paths '/*' --query 'Invalidation.Id' --output text }
}
Write-Host "Published to $($web.SiteUrl)"
