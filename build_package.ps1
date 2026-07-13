# build_package.ps1 — Hayawaza packaging script
# Usage: .\build_package.ps1 [-Version "1.0.0"] [-Channel "stable"]

param(
    [string]$Version  = "1.0.0",
    [string]$Channel  = "stable",
    [string]$ReleaseNotes = ""
)

$ErrorActionPreference = "Stop"

$root      = $PSScriptRoot
$proj      = Join-Path $root "src\ShortcutOverlay"
$publishDir = Join-Path $root "publish"
$outputDir  = Join-Path (Split-Path (Split-Path $root -Parent) -Parent) "_outputs\hayawaza"

Write-Host "=== Hayawaza Build Pipeline ===" -ForegroundColor Cyan
Write-Host "Version : $Version"
Write-Host "Channel : $Channel"

# Step 1: Clean
Write-Host "`n[1/3] Cleaning..." -ForegroundColor Yellow
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force -Confirm:$false }
# 前バージョンの Velopack 成果物を削除（再パッケージ用）
$staleFiles = Get-ChildItem $outputDir -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match "^Hayawaza-|^RELEASES|^releases|^assets\." }
if ($staleFiles) { $staleFiles | Remove-Item -Force -Confirm:$false -ErrorAction SilentlyContinue }
New-Item $publishDir -ItemType Directory | Out-Null

# Step 2: Framework-dependent publish
Write-Host "[2/3] Publishing (framework-dependent)..." -ForegroundColor Yellow
dotnet publish "$proj\ShortcutOverlay.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:Version=$Version `
    -p:FileVersion="$Version.0" `
    -o $publishDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Step 3: Velopack pack
Write-Host "[3/3] Packaging with Velopack..." -ForegroundColor Yellow

$icon = Join-Path $proj "Assets\icon.ico"
$vpkArgs = @(
    "pack",
    "--packId",      "Hayawaza",
    "--packVersion", $Version,
    "--packDir",     $publishDir,
    "--outputDir",   $outputDir,
    "--channel",     $Channel,
    "--mainExe",     "Hayawaza.exe"
)
if (Test-Path $icon) { $vpkArgs += @("--icon", $icon) }
if ($ReleaseNotes)   { $vpkArgs += @("--releaseNotes", $ReleaseNotes) }

vpk @vpkArgs

if ($LASTEXITCODE -ne 0) { throw "vpk pack failed" }

Write-Host "`n=== Complete ===" -ForegroundColor Green
Write-Host "Output: $outputDir"
Get-ChildItem $outputDir | Select-Object Name, @{n="Size(MB)";e={[math]::Round($_.Length/1MB,2)}}
