$ErrorActionPreference = "Stop"
$submodules = @("AggregatorService", "VocabularyService", "authorization-module", "polyraspad-frontend", "inclusive", "BillingService")

foreach ($sub in $submodules) {
    if (-not (Test-Path $sub)) {
        Write-Host "Submodule $sub not found, skipping."
        continue
    }

    Write-Host "----------------------------------------"
    Write-Host "Processing $sub..."
    Push-Location $sub
    
    # Check current branch
    $branch = git branch --show-current
    if ([string]::IsNullOrWhiteSpace($branch)) {
        Write-Host "$sub is in detached HEAD. Moving changes to master..."
        # We are detached. Let's create a temporary branch to hold current state
        git checkout -b temp_detached
        git checkout master
        git merge temp_detached
        git branch -d temp_detached
    } elseif ($branch -ne "master") {
        Write-Host "$sub is on branch $branch. Moving changes to master..."
        $currentBranch = $branch
        git checkout master
        git merge $currentBranch
    } else {
        Write-Host "$sub is already on master."
    }
    
    # Check for uncommitted changes
    $status = git status --porcelain
    if (![string]::IsNullOrWhiteSpace($status)) {
        Write-Host "Uncommitted changes found in $sub. Committing..."
        git add .
        git commit -m "chore: auto-commit local changes"
    } else {
        Write-Host "No uncommitted changes in $sub."
    }
    
    # Push to master
    Write-Host "Pushing $sub to origin master..."
    git push origin master
    
    Pop-Location
}

Write-Host "----------------------------------------"
Write-Host "Processing Root repository..."
$branch = git branch --show-current
if ($branch -ne "master") {
    Write-Host "Root is on $branch, moving to master..."
    $currentBranch = $branch
    git checkout master
    git merge $currentBranch
}

$status = git status --porcelain
if (![string]::IsNullOrWhiteSpace($status)) {
    Write-Host "Uncommitted changes found in Root. Committing..."
    git add .
    git commit -m "chore: update submodules and local changes"
} else {
    Write-Host "No uncommitted changes in Root."
}

Write-Host "Pushing Root to origin master..."
git push origin master
Write-Host "Done!"
