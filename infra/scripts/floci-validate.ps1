<#
.SYNOPSIS
  Deploys, checks and destroys a stage against a local Floci emulator (https://floci.io). No AWS account is used.
.DESCRIPTION
  1. Starts a fresh Floci container (Docker socket mounted; AppSync schema validation runs in a sidecar container).
  2. Creates a stand-in public hosted zone, bootstraps CDK and deploys all stacks for the stage. Floci requires the
     cognito_groups argument on @aws_cognito_user_pools (AWS does not), so a copy of the schema that lists all four
     role groups is deployed instead of the real schema.
  3. Checks stack status, tags, alias records, and publishes the web client to the emulated bucket.
  4. Destroys the stage and checks the shared hosted zone survives with only its NS/SOA records.
#>
param(
    [string]$Stage = 'dev',
    [string]$Image = 'floci/floci:latest',
    [string]$ContainerName = 'amendment-floci',
    [switch]$SkipWebPublish,
    [switch]$KeepRunning
)
$ErrorActionPreference = 'Stop'
$cdkDir = Resolve-Path (Join-Path $PSScriptRoot '..\Amendment.Cdk')
$outputsFile = Join-Path $cdkDir 'cdk-outputs-floci.json'
$prefix = "Amendment-$Stage"
$stages = Get-Content (Join-Path $cdkDir 'Config\stages.json') -Raw | ConvertFrom-Json
$zoneName = $stages.$Stage.domain.hostedZoneName
$siteDomain = $stages.$Stage.domain.siteDomainName

function Assert([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "FLOCI CHECK FAILED: $Message" }
    Write-Host "  ok: $Message"
}
function Invoke-Checked([scriptblock]$Command) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "Command failed with exit code ${LASTEXITCODE}: $Command" }
}
function Remove-FlociContainer {
    if (docker ps -aq --filter "name=^$ContainerName$") { docker rm -f $ContainerName | Out-Null }
}

$envNames = 'AWS_ENDPOINT_URL', 'AWS_REGION', 'AWS_DEFAULT_REGION', 'AWS_ACCESS_KEY_ID', 'AWS_SECRET_ACCESS_KEY', 'AWS_PROFILE', 'AWS_SESSION_TOKEN'
$savedEnv = @{}
foreach ($n in $envNames) { $savedEnv[$n] = [Environment]::GetEnvironmentVariable($n, 'Process') }

try {
    Write-Host "Starting Floci ($Image)"
    Remove-FlociContainer
    Invoke-Checked { docker run -d --name $ContainerName -p 4566:4566 -v /var/run/docker.sock:/var/run/docker.sock $Image | Out-Null }
    $deadline = (Get-Date).AddSeconds(60)
    while (-not (Test-NetConnection localhost -Port 4566 -InformationLevel Quiet -WarningAction SilentlyContinue)) {
        if ((Get-Date) -gt $deadline) { throw 'Floci did not start listening on port 4566.' }
        Start-Sleep 1
    }

    $env:AWS_ENDPOINT_URL = 'http://localhost:4566'
    $env:AWS_REGION = $stages.$Stage.region
    $env:AWS_DEFAULT_REGION = $stages.$Stage.region
    $env:AWS_ACCESS_KEY_ID = 'test'
    $env:AWS_SECRET_ACCESS_KEY = 'test'
    Remove-Item Env:AWS_PROFILE, Env:AWS_SESSION_TOKEN -ErrorAction SilentlyContinue

    $zoneId = (aws route53 create-hosted-zone --name $zoneName --caller-reference "floci-$(Get-Random)" --query HostedZone.Id --output text) -replace '^/hostedzone/', ''
    Assert ($LASTEXITCODE -eq 0 -and $zoneId) "created stand-in hosted zone $zoneName ($zoneId)"

    $repoRoot = Resolve-Path (Join-Path $cdkDir '..\..')
    Invoke-Checked { dotnet publish (Join-Path $repoRoot 'src\Amendment.Api.Lambda\Amendment.Api.Lambda.csproj') -c Release -o (Join-Path $repoRoot 'artifacts\lambda\api') --nologo -v q }

    # Cognito group names from IdentityStack.RoleGroups (D13)
    $groups = '"SystemAdministrator", "ScreenController", "AmendmentEditor", "Translator"'
    $flociSchema = Join-Path $repoRoot 'artifacts\floci\amendment.graphql'
    New-Item -ItemType Directory -Force -Path (Split-Path $flociSchema) | Out-Null
    [IO.File]::WriteAllText($flociSchema,
        ((Get-Content (Join-Path $cdkDir 'Schema\amendment.graphql') -Raw) -replace '@aws_cognito_user_pools(?!\s*\()', "@aws_cognito_user_pools(cognito_groups: [$groups])"))

    Push-Location $cdkDir
    try {
        $account = aws sts get-caller-identity --query Account --output text
        Invoke-Checked { npx cdk bootstrap "aws://$account/$($env:AWS_REGION)" }
        Invoke-Checked { npx cdk deploy --all -c "stage=$Stage" -c "hostedZoneId=$zoneId" -c "schemaFile=$flociSchema" --require-approval never --outputs-file $outputsFile }
    }
    finally { Pop-Location }

    Write-Host 'Checking deployed resources'
    foreach ($part in 'Data', 'Identity', 'Api', 'Web', 'Observability') {
        $status = aws cloudformation describe-stacks --stack-name "$prefix-$part" --query 'Stacks[0].StackStatus' --output text
        Assert ($status -match '^(CREATE|UPDATE)_COMPLETE$') "$prefix-$part is $status"
    }
    $tableArn = aws dynamodb describe-table --table-name "AmendmentData-$Stage" --query Table.TableArn --output text
    $tags = aws dynamodb list-tags-of-resource --resource-arn $tableArn --output json | ConvertFrom-Json
    Assert (($tags.Tags | Where-Object { $_.Key -eq 'Application' -and $_.Value -eq 'Amendment' }) -ne $null) 'table has tag Application=Amendment'
    Assert (($tags.Tags | Where-Object { $_.Key -eq 'Environment' -and $_.Value -eq $Stage }) -ne $null) "table has tag Environment=$Stage"
    $runtime = aws lambda get-function-configuration --function-name "$prefix-api" --query Runtime --output text
    Assert ($runtime -eq 'dotnet10') "API Lambda $prefix-api exists with runtime dotnet10"
    $records = aws route53 list-resource-record-sets --hosted-zone-id $zoneId --output json | ConvertFrom-Json
    $aliases = @($records.ResourceRecordSets | Where-Object { $_.Name -eq "$siteDomain." -and $_.AliasTarget })
    Assert ($aliases.Count -eq 2) "A/AAAA alias records exist for $siteDomain"
    $signIn = @($records.ResourceRecordSets | Where-Object { $_.Name -eq "auth.$siteDomain." -and $_.AliasTarget })
    Assert ($signIn.Count -eq 1) "A alias record exists for the sign-in domain auth.$siteDomain"

    if (-not $SkipWebPublish) {
        & (Join-Path $PSScriptRoot 'publish-web.ps1') -Stage $Stage -Region $env:AWS_REGION -OutputsFile $outputsFile -PublishDir (Join-Path $cdkDir "..\..\artifacts\web-floci")
        $bucket = (Get-Content $outputsFile -Raw | ConvertFrom-Json)."$prefix-Web".SiteBucketName
        $keys = aws s3api list-objects-v2 --bucket $bucket --query 'Contents[].Key' --output json | ConvertFrom-Json
        Assert ($keys -contains 'index.html' -and $keys -contains 'amendment-config.json') 'web client and frontend config uploaded'
    }

    Write-Host "Destroying $prefix"
    & (Join-Path $PSScriptRoot 'destroy-stage.ps1') -Stage $Stage -CdkContext "hostedZoneId=$zoneId" -Force
    $remaining = aws cloudformation list-stacks --query "StackSummaries[?starts_with(StackName,'$prefix-') && StackStatus!='DELETE_COMPLETE'].StackName" --output text
    Assert ([string]::IsNullOrWhiteSpace($remaining) -or $remaining -eq 'None') "all $prefix stacks deleted"
    $records = aws route53 list-resource-record-sets --hosted-zone-id $zoneId --output json | ConvertFrom-Json
    Assert ($LASTEXITCODE -eq 0) 'shared hosted zone still exists after destroy'
    $types = @($records.ResourceRecordSets | ForEach-Object { $_.Type } | Sort-Object -Unique)
    Assert (($types -join ',') -eq 'NS,SOA') 'only NS/SOA records remain in the shared zone'

    Write-Host "Floci validation passed for stage '$Stage'." -ForegroundColor Green
}
finally {
    foreach ($n in $envNames) { [Environment]::SetEnvironmentVariable($n, $savedEnv[$n], 'Process') }
    if (-not $KeepRunning) { Remove-FlociContainer }
}
exit 0
