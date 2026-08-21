# Run AggregatorService (BFF) with VocabularyService and authorization-module.
# Start dependencies first, then BFF. Each service runs in its own console window.
# Stop: close each window or Ctrl+C in that window.

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

# MinIO for media uploads (VocabularyService Storage:Endpoint = http://localhost:9000)
# Start only MinIO-related containers; other infra can be managed separately.
Write-Host "Starting MinIO (docker compose: minio + minio-init)..."
Push-Location $Root
try {
    docker compose up -d minio minio-init | Out-Host
}
finally {
    Pop-Location
}

# Wait until MinIO API is reachable to avoid first-upload races.
$minioReady = $false
for ($i = 0; $i -lt 20; $i++) {
    try {
        Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:9000/minio/health/live" -TimeoutSec 2 | Out-Null
        $minioReady = $true
        break
    }
    catch {
        Start-Sleep -Milliseconds 750
    }
}
if (-not $minioReady) {
    throw "MinIO is not reachable at http://localhost:9000. Check Docker Desktop / docker compose logs."
}

# Inclusive FSRS (gRPC, http://localhost:40051). Use venv so the same Python has required packages.
$InclusiveDir = "$Root\inclusive"
$VenvPython = "$InclusiveDir\.venv\Scripts\python.exe"
$VenvPip = "$InclusiveDir\.venv\Scripts\pip.exe"

if (-not (Test-Path $VenvPython)) {
    Write-Host "Creating venv in inclusive using CPython..."

    # Prefer Python 3.12 via py launcher.
    & py -3.12 -m venv "$InclusiveDir\.venv"

    if (-not (Test-Path $VenvPython)) {
        Write-Host "Fallback to standard python..."
        & python -m venv "$InclusiveDir\.venv"
    }

    # Install dependencies.
    if (Test-Path $VenvPip) {
        & "$VenvPip" install -r "$InclusiveDir\requirements.txt"
    }
    else {
        Write-Host "Error: Failed to create venv." -ForegroundColor Red
    }
}
Start-Process -FilePath "cmd" -ArgumentList "/k", "title Inclusive FSRS (40051) && `"$VenvPython`" main.py" -WorkingDirectory $InclusiveDir
Start-Sleep -Seconds 2

# VocabularyService (gRPC, http://localhost:5117)
Start-Process -FilePath "cmd" -ArgumentList "/k", "title VocabularyService (5117) && dotnet run --launch-profile http" -WorkingDirectory "$Root\VocabularyService"
Start-Sleep -Seconds 2

# authorization-module (gRPC, http://localhost:5027)
Start-Process -FilePath "cmd" -ArgumentList "/k", "title authorization-module (5027) && dotnet run --project authorization-module.API.csproj --launch-profile http" -WorkingDirectory "$Root\authorization-module\authorization-module.API"
Start-Sleep -Seconds 2

# AggregatorService (BFF, http://localhost:5206)
Start-Process -FilePath "cmd" -ArgumentList "/k", "title AggregatorService BFF (5206) && dotnet run --launch-profile http" -WorkingDirectory "$Root\AggregatorService"

Write-Host "Started: MinIO (9000/9001), Inclusive FSRS (40051), VocabularyService (5117), authorization-module (5027), AggregatorService (5206). Close each window to stop."
