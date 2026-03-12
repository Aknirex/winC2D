# Extract changelog entry for a specific version from CHANGELOG.md
# Usage: .\extract-changelog.ps1 -Version "4.0.0"

param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

$changelogPath = "CHANGELOG.md"

if (-not (Test-Path $changelogPath)) {
    Write-Error "CHANGELOG.md not found at: $(Get-Location)"
    exit 1
}

$content = Get-Content $changelogPath -Raw
$lines = $content -split "`n"

$versionPattern = "^## \[$Version\]"
$nextVersionPattern = "^## \["
$inVersion = $false
$versionContent = @()

foreach ($line in $lines) {
    if ($line -match $versionPattern) {
        $inVersion = $true
        continue  # Skip the version header line
    }
    
    if ($inVersion) {
        if ($line -match $nextVersionPattern) {
            break  # Stop at next version section
        }
        
        # Skip empty lines at the end and the --- separator
        if ($line -match "^---$") {
            break
        }
        
        $versionContent += $line
    }
}

if ($versionContent.Count -eq 0) {
    Write-Error "Version [$Version] not found in CHANGELOG.md"
    exit 1
}

# Clean up and format the output
$output = $versionContent -join "`n"
$output = $output.TrimStart() -replace '^\s*$[\r\n]{1,}', ''  # Remove leading empty lines
$output = $output -replace '[\r\n]{2,}', "`n"  # Normalize multiple newlines to single

# Format as bullet points for GitHub Release
$lines = $output -split "`n"
$formattedLines = @()

foreach ($line in $lines) {
    $trimmed = $line.Trim()
    
    if ($trimmed -match "^###") {
        # Section header like "### Added"
        $formattedLines += ""
        $formattedLines += $trimmed
    }
    elseif ($trimmed -match "^-") {
        # Already a bullet point
        $formattedLines += $trimmed
    }
    elseif ($trimmed -ne "") {
        # Convert to bullet point if it's content
        if (-not $trimmed.StartsWith("#")) {
            $formattedLines += "- $trimmed"
        }
    }
}

$result = $formattedLines -join "`n"
$result = $result.Trim()

Write-Output $result
