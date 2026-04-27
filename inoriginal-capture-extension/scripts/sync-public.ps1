$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$public = Join-Path $root "public"

New-Item -ItemType Directory -Force $public | Out-Null

$files = @(
  "manifest.json",
  "background.js",
  "content.js",
  "offscreen.html",
  "offscreen.js"
)

foreach ($file in $files) {
  Copy-Item -LiteralPath (Join-Path $root $file) -Destination (Join-Path $public $file) -Force
}
