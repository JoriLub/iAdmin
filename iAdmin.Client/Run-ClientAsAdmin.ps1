param(
    [switch]$Build,
    [string]$ApiBaseUrl
)

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$csproj = Join-Path $projectRoot "iAdmin.Client.csproj"
$exePath = Join-Path $projectRoot "bin\Debug\net10.0-windows\iAdmin.Client.exe"
$settingsPath = Join-Path $projectRoot "appsettings.json"

if (-not [string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
    if (-not (Test-Path $settingsPath)) {
        throw "appsettings.json not found at $settingsPath"
    }

    Write-Host "Updating Api.BaseUrl in appsettings.json to: $ApiBaseUrl"
    $json = Get-Content $settingsPath -Raw | ConvertFrom-Json
    if ($null -eq $json.Api) {
        $json | Add-Member -NotePropertyName Api -NotePropertyValue ([pscustomobject]@{})
    }
    $json.Api.BaseUrl = $ApiBaseUrl
    ($json | ConvertTo-Json -Depth 10) | Set-Content -Path $settingsPath -Encoding UTF8
}

if ($Build -or -not (Test-Path $exePath)) {
    Write-Host "Building iAdmin.Client..."
    dotnet build $csproj

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed. Client was not started."
        exit $LASTEXITCODE
    }
}

Write-Host "Starting iAdmin.Client with Administrator rights (UAC prompt)..."
Start-Process -FilePath $exePath -WorkingDirectory $projectRoot -Verb RunAs
