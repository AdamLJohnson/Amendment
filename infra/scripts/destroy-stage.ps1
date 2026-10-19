<#
.SYNOPSIS
  Intentionally destroys a non-production stage: empties the site bucket, then runs `cdk destroy --all`.
.DESCRIPTION
  Refuses to run for production stages. The shared Route 53 hosted zone is imported (never owned) by the
  stacks, so only the stage's own alias records are removed.
#>
param(
    [string]$Stage = 'dev',
    [string[]]$CdkContext = @(),
    [switch]$Force
)
$ErrorActionPreference = 'Stop'
$cdkDir = Resolve-Path (Join-Path $PSScriptRoot '..\Amendment.Cdk')

$stages = Get-Content (Join-Path $cdkDir 'Config\stages.json') -Raw | ConvertFrom-Json
if (-not $stages.$Stage) { throw "Unknown stage '$Stage'." }
if ($stages.$Stage.isProduction) { throw "Refusing to destroy production stage '$Stage'." }

$webStack = "Amendment-$Stage-Web"
$webStackExists = aws cloudformation list-stacks --query "StackSummaries[?StackName=='$webStack' && StackStatus!='DELETE_COMPLETE'].StackName" --output text
$bucket = if ($webStackExists -and $webStackExists -ne 'None') {
    aws cloudformation describe-stacks --stack-name $webStack --query "Stacks[0].Outputs[?OutputKey=='SiteBucketName'].OutputValue" --output text
}
if ($bucket -and $bucket -ne 'None') {
    Write-Host "Emptying s3://$bucket"
    aws s3 rm "s3://$bucket" --recursive --only-show-errors
    if ($LASTEXITCODE -ne 0) { throw "Failed to empty s3://$bucket." }
}

$cdkArgs = @('cdk', 'destroy', '--all', '-c', "stage=$Stage") + ($CdkContext | ForEach-Object { @('-c', $_) })
if ($Force) { $cdkArgs += '--force' }
$npx = (Get-Command npx -CommandType Application | Select-Object -First 1).Source
Push-Location $cdkDir
try {
    & $npx @cdkArgs
    if ($LASTEXITCODE -ne 0) { throw "cdk destroy failed with exit code $LASTEXITCODE." }
}
finally { Pop-Location }
