param (
    [string]$Type = "chore",
    [string]$Scope = "",
    [string]$Message = "auto-update repositories"
)

# Формируем сообщение в формате Conventional Commits
$CommitMsg = if ([string]::IsNullOrWhiteSpace($Scope)) {
    "${Type}: $Message"
} else {
    "${Type}(${Scope}): $Message"
}

Write-Host "========================================" -ForegroundColor Magenta
Write-Host "1. PROCESSING SUBMODULES" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

Write-Host "Switching submodules to 'master' and pulling latest changes..." -foregroundColor Cyan
git submodule foreach "git checkout master && git pull origin master || true"

Write-Host "Staging and committing changes in submodules..." -foregroundColor Cyan
git submodule foreach "git add . && git commit -m '$CommitMsg' || true"

Write-Host "Pushing submodules to remote master..." -foregroundColor Cyan
git submodule foreach "git push origin master || true"


Write-Host "========================================" -ForegroundColor Magenta
Write-Host "2. PROCESSING ROOT REPOSITORY" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

Write-Host "Switching Root repository to 'master' and pulling latest changes..." -foregroundColor Cyan
git checkout master
git pull --no-recurse-submodules origin master

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to pull root repository from origin master."
    exit $LASTEXITCODE
}

# Сохраняем корневые изменения (включая обновленные ссылки на сабмодули)
$status = git status --porcelain
if (![string]::IsNullOrWhiteSpace($status)) {
    Write-Host "Committing changes in Root repository..." -foregroundColor Cyan
    git add .
    git commit -m "$CommitMsg"
} else {
    Write-Host "No uncommitted changes in Root." -foregroundColor Yellow
}

Write-Host "Pushing Root to origin master..." -foregroundColor Cyan
git push origin master

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to push root repository to origin master."
    exit $LASTEXITCODE
}

Write-Host "========================================" -ForegroundColor Green
Write-Host "ALL REPOSITORIES SUCCESSFULLY COMMITTED AND PUSHED!" -ForegroundColor Green
