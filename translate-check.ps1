# 翻译辅助脚本 - 使用机器翻译作为基础
# Translation Helper Script - Using machine translation as base

# 注意：这是一个基础翻译，建议由母语使用者审核和优化
# Note: This is a baseline translation, should be reviewed by native speakers

Write-Host "winC2D Translation Helper" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Current Translation Status:" -ForegroundColor Yellow
Write-Host "? English (en) - 100% Complete" -ForegroundColor Green
Write-Host "? Chinese (zh-CN) - 100% Complete" -ForegroundColor Green  
Write-Host "? Japanese (ja) - In Progress..." -ForegroundColor Yellow
Write-Host "? Korean (ko) - Pending..." -ForegroundColor Yellow
Write-Host "? French (fr) - Pending..." -ForegroundColor Yellow
Write-Host "? German (de) - Pending..." -ForegroundColor Yellow
Write-Host "? Spanish (es) - Pending..." -ForegroundColor Yellow
Write-Host "? Russian (ru) - Pending..." -ForegroundColor Yellow
Write-Host ""
Write-Host "Building project to generate satellite assemblies..." -ForegroundColor Cyan

# 构建项目
& dotnet build --configuration Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Build successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Language files generated:" -ForegroundColor Cyan
    Get-ChildItem -Path "bin\Release\net6.0-windows" -Directory | Where-Object { $_.Name.Length -eq 2 -or $_.Name -like "*-*" } | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor White
    }
} else {
    Write-Host "? Build failed!" -ForegroundColor Red
}

Write-Host ""
Write-Host "To contribute translations:" -ForegroundColor Yellow
Write-Host "1. Edit the corresponding Strings.xx.resx file" -ForegroundColor White
Write-Host "2. Translate all <value> tags" -ForegroundColor White
Write-Host "3. Test by running: dotnet run" -ForegroundColor White
Write-Host "4. Submit a Pull Request on GitHub" -ForegroundColor White
Write-Host ""
Write-Host "See TRANSLATION_GUIDE.md for detailed instructions." -ForegroundColor Cyan
