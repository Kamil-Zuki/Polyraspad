# Run AggregatorService (BFF) with VocabularyService and authorization-module.
# Start dependencies first, then BFF. Each service runs in its own console window.
# Stop: close each window or Ctrl+C in that window.

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

# VocabularyService (gRPC, http://localhost:5117)
Start-Process -FilePath "dotnet" -ArgumentList "run", "--launch-profile", "http" -WorkingDirectory "$Root\VocabularyService"
Start-Sleep -Seconds 2

# authorization-module (gRPC, http://localhost:5027)
Start-Process -FilePath "dotnet" -ArgumentList "run", "--launch-profile", "http" -WorkingDirectory "$Root\authorization-module\authorization-module.API"
Start-Sleep -Seconds 2

# AggregatorService (BFF, http://localhost:5206)
Start-Process -FilePath "dotnet" -ArgumentList "run", "--launch-profile", "http" -WorkingDirectory "$Root\AggregatorService"

Write-Host "Started: VocabularyService (5117), authorization-module (5027), AggregatorService (5206). Close each window to stop."
