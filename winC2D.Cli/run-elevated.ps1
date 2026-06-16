# winC2D elevated execution wrapper
# Usage: pwsh -File run-elevated.ps1 migrate --source "C:\Program Files\App" --target "D:\MigratedApps" --yes
#
# Uses gsudo (bundled) for inline elevation. If gsudo.exe is not found in the same directory,
# automatically downloads it via winget.

param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$cliPath = Join-Path $scriptDir "winC2D.Cli.exe"
$gsudoPath = Join-Path $scriptDir "gsudo.exe"

if (-not (Test-Path $cliPath)) {
    Write-Error "winC2D.Cli.exe not found at: $cliPath"
    exit 1
}

# Ensure gsudo is available (bundled or system-wide)
$gsudoExe = $null
if (Test-Path $gsudoPath) {
    $gsudoExe = $gsudoPath
} else {
    $sysGsudo = Get-Command gsudo -ErrorAction SilentlyContinue
    if ($sysGsudo) {
        $gsudoExe = $sysGsudo.Source
    } else {
        Write-Host "gsudo not found. Installing via winget..."
        winget install gerardog.gsudo --accept-package-agreements --accept-source-agreements
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to install gsudo. Please install manually: winget install gerardog.gsudo"
            exit 1
        }
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" +
                    [System.Environment]::GetEnvironmentVariable("Path", "User")
        $gsudoExe = (Get-Command gsudo -ErrorAction Stop).Source
    }
}

# Run the CLI elevated via gsudo (inline, output goes to caller's stdout)
& $gsudoExe $cliPath @Args
exit $LASTEXITCODE
