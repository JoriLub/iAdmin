param(
    [string]$Message,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "         iAdmin - Upload naar GitHub Task              " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# Check git status
Write-Host "[1/5] Controleer git status..." -ForegroundColor Yellow
$status = git -C $projectRoot status --porcelain

if ([string]::IsNullOrWhiteSpace($status)) {
    Write-Host "Ok: Geen wijzigingen gevonden. Repository is up-to-date." -ForegroundColor Green
    exit 0
}

Write-Host "Ok: Wijzigingen gevonden:" -ForegroundColor Green
$status | ForEach-Object { Write-Host "  $_" }
Write-Host ""

# Stage all changes
Write-Host "[2/5] Stage alle wijzigingen..." -ForegroundColor Yellow
git -C $projectRoot add -A

if ($LASTEXITCODE -ne 0) {
    Write-Error "Git add mislukt"
    exit 1
}
Write-Host "Ok: Wijzigingen gestaged." -ForegroundColor Green
Write-Host ""

# Create commit message
Write-Host "[3/5] Maak commit message..." -ForegroundColor Yellow
$defaultMessage = "Update iAdmin: $(Get-Date -Format 'yyyy-MM-dd HH:mm')"

if ([string]::IsNullOrWhiteSpace($Message)) {
    $Message = $defaultMessage
}

Write-Host "Commit message: '$Message'" -ForegroundColor Cyan
Write-Host ""

# Commit
Write-Host "[4/5] Maak commit..." -ForegroundColor Yellow
git -C $projectRoot commit -m $Message

if ($LASTEXITCODE -ne 0) {
    Write-Error "Git commit mislukt"
    exit 1
}
Write-Host "Ok: Commit gemaakt." -ForegroundColor Green
Write-Host ""

# Push to GitHub
Write-Host "[5/5] Push naar GitHub..." -ForegroundColor Yellow
$pushArgs = @("push", "-u", "origin", "main")
if ($Force) {
    $pushArgs += "--force"
    Write-Host "WARNING: Force push ingeschakeld." -ForegroundColor Yellow
}

git -C $projectRoot @pushArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Git push mislukt. Controleer je internet en GitHub credentials."
    exit 1
}
Write-Host "Ok: Push naar GitHub succesvol." -ForegroundColor Green
Write-Host ""

Write-Host "========================================================" -ForegroundColor Green
Write-Host "         Upload naar GitHub voltooid!                 " -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Repository geupload naar GitHub." -ForegroundColor Cyan
