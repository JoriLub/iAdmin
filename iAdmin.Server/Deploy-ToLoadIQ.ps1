param(
    [string]$Configuration = "Release",
    [string]$TargetPath = "\\serverweb\Data\WEB\PROD\LOADIQ.NL\IADMIN.API"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Join-Path $projectRoot "iAdmin.Server.csproj"
$publishPath = Join-Path $projectRoot "publish\prod"

Write-Host "[1/4] Publishing iAdmin.Server ($Configuration)..."
dotnet publish $projectFile -c $Configuration -o $publishPath

if ($LASTEXITCODE -ne 0) {
    throw "Publish failed."
}

if (-not (Test-Path $TargetPath)) {
    throw "Target path does not exist: $TargetPath"
}

Write-Host "[2/4] Syncing publish output to target..."
robocopy $publishPath $TargetPath /MIR /R:2 /W:1

if ($LASTEXITCODE -gt 7) {
    throw "Robocopy failed with exit code $LASTEXITCODE"
}

Write-Host "[3/4] Done copying files."
Write-Host "[4/4] IMPORTANT: Recycle IIS Application Pool / Website if needed."
Write-Host "Deployment completed: $TargetPath"
