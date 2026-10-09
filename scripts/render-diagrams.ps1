<#
.SYNOPSIS
  Renders a topic's SVG diagrams to PNG with headless Edge so the manual review (layer 3 in
  docs/rules/verification.md) can actually LOOK at them. Output goes to a temp folder, never the repo.
.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts/render-diagrams.ps1 -Topic OOP
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts/render-diagrams.ps1 -Topic OOP -Only 'Memory/4*'
.NOTES
  Prints one line per PNG written. Open/read each PNG and tick the visual checklist. A diagram you did
  not look at must be reported as "not visually verified", never as checked.
#>
param(
    [Parameter(Mandatory = $true)][string]$Topic,
    [string]$Only = '*',
    [string]$OutDir
)

$ErrorActionPreference = 'Stop'
$repo    = Split-Path -Parent $PSScriptRoot
$diagDir = Join-Path $repo "ArchitectureDiagrams\$Topic"
if (-not (Test-Path $diagDir)) { Write-Host "No such topic folder: $diagDir"; exit 2 }
if (-not $OutDir) { $OutDir = Join-Path ([IO.Path]::GetTempPath()) "bta-render\$Topic" }
New-Item -ItemType Directory -Force $OutDir | Out-Null

$edge = @("${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe", "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $edge) { Write-Host 'Microsoft Edge not found - open the SVGs in any browser by hand instead.'; exit 2 }

$profileDir = Join-Path $OutDir '_profile'
$written = 0; $failed = 0
foreach ($svg in Get-ChildItem $diagDir -Recurse -Filter '*.svg') {
    $rel = $svg.FullName.Substring($diagDir.Length + 1).Replace('\', '/')
    if ($rel -notlike $Only) { continue }
    $vb = [regex]::Match((Get-Content -Encoding UTF8 $svg.FullName -Raw), 'viewBox="0 0 (\d+(?:\.\d+)?) (\d+(?:\.\d+)?)"')
    $w = if ($vb.Success) { [int][math]::Ceiling([double]$vb.Groups[1].Value) } else { 1050 }
    $h = if ($vb.Success) { [int][math]::Ceiling([double]$vb.Groups[2].Value) } else { 700 }
    $png = Join-Path $OutDir (($rel -replace '[\\/]', '_') -replace '\.svg$', '.png')
    if (Test-Path $png) { Remove-Item $png -Force }
    $url  = 'file:///' + ($svg.FullName -replace '\\', '/')
    $argv = @('--headless=new', '--disable-gpu', '--hide-scrollbars', "--user-data-dir=$profileDir", "--window-size=$w,$h", "--screenshot=$png", $url)
    $p = Start-Process -FilePath $edge -ArgumentList $argv -PassThru -WindowStyle Hidden
    if (-not $p.WaitForExit(30000)) { $p.Kill() }
    if (Test-Path $png) { Write-Host "  $png"; $written++ } else { Write-Host "  FAILED to render $rel"; $failed++ }
}
Write-Host "Rendered $written diagram(s) to $OutDir ($failed failed). Now LOOK at each one against the layer-3 checklist."
exit $(if ($failed) { 1 } else { 0 })
