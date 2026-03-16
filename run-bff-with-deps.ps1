# Run AggregatorService (BFF) with VocabularyService and authorization-module.
# Start dependencies first, then BFF. Each service runs in its own console window.
# Stop: close each window or Ctrl+C in that window.

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

# Inclusive FSRS (gRPC, http://localhost:40051). Используем venv, чтобы один и тот же Python имел пакеты.
$InclusiveDir = "$Root\inclusive"
$VenvPython = "$InclusiveDir\.venv\Scripts\python.exe"
$VenvPip = "$InclusiveDir\.venv\Scripts\pip.exe"

if (-not (Test-Path $VenvPython)) {
    Write-Host "Creating venv in inclusive using CPython..."
    
    # Явный запуск лаунчера py.exe для версии 3.12
    & py -3.12 -m venv "$InclusiveDir\.venv"
    
    if (-not (Test-Path $VenvPython)) {
        Write-Host "Fallback to standard python..."
        & python -m venv "$InclusiveDir\.venv"
    }

    # Ставим зависимости
    if (Test-Path $VenvPip) {
        & "$VenvPip" install -r "$InclusiveDir\requirements.txt"
    } else {
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

Write-Host "Started: Inclusive FSRS (40051), VocabularyService (5117), authorization-module (5027), AggregatorService (5206). Close each window to stop."
