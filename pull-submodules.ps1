# Pull all Git submodules and update them to the latest tracking branch

Write-Host "Updating and initializing Git submodules recursively..." -foregroundColor Cyan
git submodule update --init --recursive

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to initialize/update git submodules."
    exit $LASTEXITCODE
}

Write-Host "Setting remote HEAD for all submodules..." -foregroundColor Cyan
git submodule foreach git remote set-head origin -a

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to set remote HEAD for submodules."
    exit $LASTEXITCODE
}

Write-Host "Fetching and updating submodules to their remote tracking branches..." -foregroundColor Cyan
git submodule update --recursive --remote

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to update submodules to remote branch version."
    exit $LASTEXITCODE
}

Write-Host "All submodules successfully updated!" -foregroundColor Green

