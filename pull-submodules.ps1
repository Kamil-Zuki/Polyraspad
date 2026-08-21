# Pull all Git submodules and update them to the latest tracking branch
# Resilient: network failures on individual submodules do not abort the whole run.

Write-Host "Updating and initializing Git submodules recursively..." -ForegroundColor Cyan
git submodule update --init --recursive

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to initialize/update git submodules."
    exit $LASTEXITCODE
}

Write-Host "Setting remote HEAD for all submodules (best-effort)..." -ForegroundColor Cyan
git submodule foreach "git remote set-head origin -a || true"

Write-Host "Fetching and updating each submodule to latest remote master (per-submodule, resilient)..." -ForegroundColor Cyan

# Get list of submodule paths
$submodules = (git config --file .gitmodules --get-regexp "submodule\..*\.path") -split "`n" |
    ForEach-Object { ($_ -split "\s+")[-1].Trim() } |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$failed = @()
foreach ($sub in $submodules) {
    Write-Host "  Updating '$sub' ..." -ForegroundColor DarkCyan
    Push-Location $sub
    git fetch origin master 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        git checkout master 2>&1 | Out-Null
        git merge --ff-only origin/master 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "  '$sub': could not fast-forward (may need manual merge)."
            $failed += $sub
        } else {
            Write-Host "  '$sub': OK" -ForegroundColor Green
        }
    } else {
        Write-Warning "  '$sub': network error, skipping (will retry next run)."
        $failed += $sub
    }
    Pop-Location
}

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Warning "Submodules that could not be updated (network timeout or merge conflict):"
    $failed | ForEach-Object { Write-Warning "  - $_" }
    Write-Host "All other submodules were updated successfully." -ForegroundColor Yellow
} else {
    Write-Host "All submodules successfully updated!" -ForegroundColor Green
}
