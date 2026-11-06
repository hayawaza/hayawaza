<#
.SYNOPSIS
    Hayawaza MSIX build script
.PARAMETER Version
    Package version (e.g. 1.0.3.0)
.PARAMETER PublisherId
    Partner Center Publisher ID (CN=xxxxxxxx-...)
.EXAMPLE
    .\build_msix.ps1 -Version "1.0.3.0" -PublisherId "CN=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
#>
param(
    [string]$Version     = "1.0.3.0",
    [string]$PublisherId = "CN=PUBLISHER_ID_PLACEHOLDER",
    [string]$OutDir      = (Join-Path $PSScriptRoot "..\..\_outputs\hayawaza-msix"),
    [string]$MakeAppx    = (Join-Path $env:LOCALAPPDATA "Temp\claude\msix-tools\buildtools\bin\10.0.22621.0\x64\makeappx.exe")
)

$ErrorActionPreference = "Stop"

$root      = $PSScriptRoot
$proj      = Join-Path $root "src\ShortcutOverlay\ShortcutOverlay.csproj"
$pubDir    = Join-Path $root "publish-msix"
$outDir    = [System.IO.Path]::GetFullPath($OutDir)
$makeappx  = $MakeAppx
$manifest  = Join-Path $root "src\ShortcutOverlay\Package.appxmanifest"
$msixName  = "Hayawaza_${Version}_x64.msix"

Write-Output "=== Hayawaza MSIX Build ==="
Write-Output "Version : $Version"
Write-Output "Publisher: $PublisherId"

# Create output dirs (clean publish-msix every time)
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
if (Test-Path $pubDir) { Remove-Item $pubDir -Recurse -Force }
New-Item -ItemType Directory -Path $pubDir -Force | Out-Null

# [1] dotnet publish
Write-Output "`n[1/4] Publishing..."
dotnet publish $proj -c Release -r win-x64 --self-contained false `
    -o $pubDir `
    /p:Version=$Version `
    --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# [2] Patch manifest: replace publisher ID and version (makeappx requires AppxManifest.xml)
Write-Output "[2/4] Patching manifest..."
$manifestDst = Join-Path $pubDir "AppxManifest.xml"
(Get-Content $manifest -Raw) `
    -replace 'CN=PUBLISHER_ID_PLACEHOLDER', $PublisherId `
    -replace 'Version="1\.0\.\d+\.0"', "Version=`"$Version`"" `
    | Set-Content $manifestDst -Encoding UTF8

# [2b] Copy store icons (PNG only) into publish dir
$storeSrc = Join-Path $root "src\ShortcutOverlay\Assets\store"
$storeDst = Join-Path $pubDir "Assets\store"
New-Item -ItemType Directory -Path $storeDst -Force | Out-Null
Get-ChildItem $storeSrc -Filter "*.png" | Copy-Item -Destination $storeDst -Force

# [3] Package MSIX
Write-Output "[3/4] Packaging MSIX..."
$msixOut = Join-Path $outDir $msixName
if (Test-Path $makeappx) {
    & $makeappx pack /d $pubDir /p $msixOut /nv /o
    if ($LASTEXITCODE -ne 0) { throw "makeappx failed" }
} else {
    throw "makeappx.exe not found: $makeappx"
}

# [4] Create .msixupload (wrap msix in zip)
Write-Output "[4/4] Creating .msixupload..."
$uploadOut = Join-Path $outDir "Hayawaza_${Version}_x64.msixupload"
Compress-Archive -Path $msixOut -DestinationPath ($uploadOut -replace '\.msixupload$', '.zip') -Force
Move-Item ($uploadOut -replace '\.msixupload$', '.zip') $uploadOut -Force

Write-Output "`n=== Complete ==="
Write-Output "MSIX    : $msixOut"
Write-Output "Upload  : $uploadOut"
Get-ChildItem $outDir | Select-Object Name, @{n='Size(MB)';e={[math]::Round($_.Length/1MB,2)}} | Format-Table