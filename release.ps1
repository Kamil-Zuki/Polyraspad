param (
    [string]$DefaultVersion = "0.0.0"
)

$changelogPath = "CHANGELOG.md"

if (!(Test-Path $changelogPath)) {
    Write-Host "CHANGELOG.md not found!" -ForegroundColor Red
    exit 1
}

$content = Get-Content $changelogPath -Raw
$lines = $content -split "`r?`n"

$unreleasedStartIndex = -1
$unreleasedEndIndex = -1
$lastVersion = $DefaultVersion
$bumpType = "patch"

for ($i = 0; $i -lt $lines.Length; $i++) {
    $line = $lines[$i]
    if ($line -match "^## \[Unreleased\]") {
        $unreleasedStartIndex = $i
    }
    elseif ($unreleasedStartIndex -ne -1 -and $unreleasedEndIndex -eq -1 -and $line -match "^## \[([0-9]+\.[0-9]+\.[0-9]+)\]") {
        $unreleasedEndIndex = $i - 1
        $lastVersion = $matches[1]
    }
}

if ($unreleasedStartIndex -eq -1) {
    Write-Host "No [Unreleased] section found in CHANGELOG.md. Make sure it exists." -ForegroundColor Red
    exit 1
}

if ($unreleasedEndIndex -eq -1) {
    $unreleasedEndIndex = $lines.Length - 1
}

$unreleasedContent = $lines[($unreleasedStartIndex+1)..$unreleasedEndIndex]

$hasChanges = $false
foreach ($line in $unreleasedContent) {
    if ($line -match "^- ") {
        $hasChanges = $true
    }
    
    if ($line -match "^### Removed" -or $line -match "BREAKING CHANGE") {
        $bumpType = "major"
        break
    }
    elseif ($line -match "^### Added" -or $line -match "^### Changed") {
        if ($bumpType -ne "major") {
            $bumpType = "minor"
        }
    }
}

if (!$hasChanges) {
    Write-Host "No changes found in [Unreleased] section. Nothing to release." -ForegroundColor Yellow
    exit 0
}

$versionParts = $lastVersion.Split('.')
$major = [int]$versionParts[0]
$minor = [int]$versionParts[1]
$patch = [int]$versionParts[2]

if ($bumpType -eq "major") {
    $major++
    $minor = 0
    $patch = 0
} elseif ($bumpType -eq "minor") {
    $minor++
    $patch = 0
} else {
    $patch++
}

$newVersion = "$major.$minor.$patch"
$dateStr = (Get-Date).ToString("yyyy-MM-dd")

Write-Host "Determined bump type: $bumpType" -ForegroundColor Cyan
Write-Host "Bumping version: $lastVersion -> $newVersion" -ForegroundColor Green

$newChangelog = @()
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($i -eq $unreleasedStartIndex) {
        $line = $lines[$i]
        $extraText = ""
        if ($line -match "^## \[Unreleased\](.*)") {
            $extraText = $matches[1]
        }
        
        $newChangelog += "## [Unreleased]"
        $newChangelog += ""
        $newChangelog += "## [$newVersion] - $dateStr$extraText"
    } else {
        $newChangelog += $lines[$i]
    }
}

$newChangelog -join "`r`n" | Set-Content $changelogPath -Encoding UTF8

$CommitMsg = "chore(release): v$newVersion"

Write-Host "========================================" -ForegroundColor Magenta
Write-Host "1. PROCESSING SUBMODULES" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

Write-Host "Switching submodules to 'master' and pulling latest changes..." -foregroundColor Cyan
git submodule foreach "git checkout master && git pull origin master || true"

Write-Host "Staging and committing changes in submodules..." -ForegroundColor Cyan
git submodule foreach "git add . && git commit -m '$CommitMsg' || true"

Write-Host "Pushing submodules to remote master..." -ForegroundColor Cyan
git submodule foreach "git push origin master"


Write-Host "========================================" -ForegroundColor Magenta
Write-Host "2. PROCESSING ROOT REPOSITORY" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

git checkout master
git pull origin master

Write-Host "Staging Root changes..." -ForegroundColor Cyan
git add .
git commit -m "$CommitMsg"

Write-Host "Creating Git Tag v$newVersion..." -ForegroundColor Cyan
git tag "v$newVersion"

Write-Host "Pushing Root and tags to origin master..." -ForegroundColor Cyan
git push origin master --tags

Write-Host "========================================" -ForegroundColor Green
Write-Host "RELEASE v$newVersion SUCCESSFULLY COMMITTED AND PUSHED!" -ForegroundColor Green
