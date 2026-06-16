param(
    [switch]$Build
)

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$csproj = Join-Path $projectRoot "iAdmin.Client.csproj"
$exePath = Join-Path $projectRoot "bin\Debug\net10.0-windows\iAdmin.Client.exe"

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
