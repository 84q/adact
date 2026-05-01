#!/usr/bin/env pwsh
<#
.SYNOPSIS
    ADACT の Layer 別カバレッジレポートを生成する。

.DESCRIPTION
    coverlet + reportgenerator を使い、指定した Layer（Unit/Integration/Smoke/E2E/All）の
    テストを実行し、cobertura 形式のカバレッジを収集して HTML/TextSummary レポートを出力する。

.PARAMETER Layer
    実行対象のテスト Layer。Unit / Integration / Smoke / E2E / All のいずれか。
    デフォルトは All。

.EXAMPLE
    .\scripts\coverage.ps1 -Layer Unit
    .\scripts\coverage.ps1 -Layer Integration
    .\scripts\coverage.ps1
#>
param(
    [ValidateSet("Unit", "Integration", "Smoke", "E2E", "All")]
    [string]$Layer = "All"
)

$ErrorActionPreference = "Stop"

# Layer 別の xUnit Trait filter
$filters = @{
    "Unit"        = "Layer=Unit"
    "Integration" = "Layer=Integration"
    "Smoke"       = "Layer=Smoke"
    "E2E"         = "Layer=E2E"
    "All"         = "Layer=Unit|Layer=Integration|Layer=Smoke|Layer=E2E"
}

$filter = $filters[$Layer]
$resultsDir = "TestResults/$Layer"

# 古い結果をクリーン
if (Test-Path $resultsDir) {
    Remove-Item -Recurse -Force $resultsDir
}

Write-Host "Running tests: filter='$filter', results-dir='$resultsDir'" -ForegroundColor Cyan

# テスト実行 + カバレッジ収集
# .runsettings で共通除外設定を適用
dotnet test adact.sln `
    --filter $filter `
    --collect:"XPlat Code Coverage" `
    --results-directory $resultsDir `
    --settings .runsettings

$reportDir = Join-Path $resultsDir "coverage-html"
$reports = "$resultsDir/**/coverage.cobertura.xml"

$rg = Get-Command reportgenerator -ErrorAction SilentlyContinue
if (-not $rg) {
    Write-Warning "reportgenerator not found. Install with: dotnet tool install --global dotnet-reportgenerator-globaltool"
    Write-Host "Raw coverage files are available at: $resultsDir"
    exit 0
}

Write-Host "Generating coverage report..." -ForegroundColor Cyan
reportgenerator `
    -reports:$reports `
    -targetdir:$reportDir `
    -reporttypes:"Html;TextSummary" `
    -assemblyfilters:"+Adact.*;+adact;-Adact.*.Tests"

Write-Host "`nCoverage report generated: $reportDir/index.html" -ForegroundColor Green

if (Test-Path "$reportDir/Summary.txt") {
    Write-Host "`n--- Summary ---" -ForegroundColor Green
    Get-Content "$reportDir/Summary.txt"
}
