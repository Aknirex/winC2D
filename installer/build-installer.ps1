# winC2D Installer Builder
# Prerequisites: Inno Setup 6 (https://jrsoftware.org/isinfo.php), .NET 8 SDK
# Usage: pwsh -File build-installer.ps1
#
# Steps:
# 1. Publish .NET projects
# 2. Copy auxiliary files
# 3. Download gsudo.exe (MIT licensed)
# 4. Build the Inno Setup installer

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $root
$publishDir = Join-Path $root "publish"

Write-Host "=== Step 1: Publishing .NET projects ==="

# Clean
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

# Publish winC2D.App (single-file)
dotnet publish "$root\winC2D.App\winC2D.App.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$publishDir\app"

# Publish winC2D.Cli (single-file)
dotnet publish "$root\winC2D.Cli\winC2D.Cli.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$publishDir\cli"

Write-Host "=== Step 2: Copying auxiliary files ==="

Copy-Item "$root\winC2D.Cli\run-elevated.ps1" "$publishDir\cli\" -Force
Copy-Item "$root\docs\README.ai.md" "$publishDir\" -Force
Copy-Item "$root\README.md" "$publishDir\" -Force
Copy-Item "$root\LICENSE" "$publishDir\" -Force
Copy-Item "$root\CHANGELOG.md" "$publishDir\" -Force

# Portable agent skill (the installer links this canonical copy into each
# user-selected agent directory)
$skillDir = Join-Path $publishDir "winc2d-skill"
New-Item -ItemType Directory -Force $skillDir | Out-Null
Copy-Item "$root\installer\winc2d-skill\SKILL.md" $skillDir -Force

Write-Host "=== Step 3: Downloading gsudo.exe ==="

$gsudoDest = Join-Path $publishDir "gsudo.exe"

if (-not (Test-Path $gsudoDest)) {
    Write-Host "Downloading gsudo.exe..."
    Invoke-WebRequest -Uri "https://github.com/gerardog/gsudo/releases/latest/download/gsudo.exe" -OutFile $gsudoDest
    Write-Host "Downloaded to: $gsudoDest"
} else {
    Write-Host "gsudo.exe already present."
}

Write-Host "=== Step 4: Building installer ==="

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $iscc) {
    $isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $isccPath) {
        $iscc = $isccPath
    } else {
        Write-Error "Inno Setup 6 not found. Install from: https://jrsoftware.org/isinfo.php"
        exit 1
    }
}

& $iscc "$root\installer\setup.iss"

Write-Host ""
Write-Host "=== Done! ==="
Write-Host "Installer: $root\installer\Output\winC2D-Setup-*.exe"
