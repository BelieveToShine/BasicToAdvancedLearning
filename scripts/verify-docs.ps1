<#
.SYNOPSIS
  Verifies the markdown doc set: every relative link resolves, every doc is reachable from
  docs/README.md, spec categories are indexed, and no forbidden patterns (dates in names, ticket
  numbers inside specs) slip in.
.EXAMPLE
  powershell -File scripts/verify-docs.ps1
#>
$ErrorActionPreference = 'Stop'
$repo  = Split-Path -Parent $PSScriptRoot
$fails = 0; $warns = 0
function Pass([string]$m) { Write-Host "  PASS  $m" -ForegroundColor Green }
function Warn([string]$m) { Write-Host "  WARN  $m" -ForegroundColor Yellow; $script:warns++ }
function Fail([string]$m) { Write-Host "  FAIL  $m" -ForegroundColor Red;    $script:fails++ }

$mdFiles = @(Get-Item (Join-Path $repo 'CLAUDE.md')) + @(Get-ChildItem (Join-Path $repo 'docs') -Recurse -Filter '*.md')

Write-Host "`n=== Relative links resolve ===" -ForegroundColor Cyan
$linkCount = 0
foreach ($f in $mdFiles) {
    $text = Get-Content -Encoding UTF8 $f.FullName -Raw
    $text = [regex]::Replace($text, '(?s)```.*?```', '')            # ignore fenced code blocks
    foreach ($m in [regex]::Matches($text, '\]\(([^)\s]+)\)')) {
        $target = $m.Groups[1].Value
        if ($target -match '^(https?:|mailto:|#)') { continue }
        $target = ($target -split '#')[0]
        if (-not $target) { continue }
        $linkCount++
        $resolved = Join-Path $f.DirectoryName $target
        if (-not (Test-Path $resolved)) { Fail "$($f.FullName.Substring($repo.Length + 1)) -> broken link: $target" }
    }
}
if ($fails -eq 0) { Pass "$linkCount relative link(s) across $($mdFiles.Count) file(s)" }

Write-Host "`n=== Reachability from docs/README.md ===" -ForegroundColor Cyan
$readme = Get-Content -Encoding UTF8 (Join-Path $repo 'docs/README.md') -Raw
foreach ($f in $mdFiles) {
    $rel = $f.FullName.Substring((Join-Path $repo 'docs').Length + 1).Replace('\', '/')
    if ($f.Name -eq 'CLAUDE.md' -or $f.Name -eq 'README.md') { continue }
    if ($rel -like 'superpowers/specs/*/*' ) { continue }            # category docs are indexed by the specs README
    if ($rel -like 'todo/*') { continue }                            # working docs are transient by design
    if ($readme -notmatch [regex]::Escape($f.Name)) { Warn "docs/$rel is not mentioned in docs/README.md" }
}
Pass 'reachability checked'

Write-Host "`n=== Spec categories indexed ===" -ForegroundColor Cyan
$specRoot  = Join-Path $repo 'docs/superpowers/specs'
$specIndex = Get-Content -Encoding UTF8 (Join-Path $specRoot 'README.md') -Raw
foreach ($cat in Get-ChildItem $specRoot -Directory) {
    if (-not (Test-Path (Join-Path $cat.FullName 'overview.md'))) { Fail "spec category '$($cat.Name)' has no overview.md" }
    if ($specIndex -notmatch [regex]::Escape("$($cat.Name)/")) { Fail "spec category '$($cat.Name)' missing from specs/README.md index" }
}
Pass 'every category has overview.md and an index row'

Write-Host "`n=== Stale facts: done-topic counts agree with the ROADMAP tracker ===" -ForegroundColor Cyan
$tick = [string][char]0x2705
$roadmapLines = Get-Content -Encoding UTF8 (Join-Path $repo 'docs/ROADMAP.md')
$doneNumbers = @($roadmapLines | Where-Object { $_ -match '^\|\s*\d+\s*\|' -and $_.Contains($tick) } | ForEach-Object { [int]([regex]::Match($_, '^\|\s*(\d+)').Groups[1].Value) })
$doneMax = if ($doneNumbers.Count) { ($doneNumbers | Measure-Object -Maximum).Maximum } else { 0 }
$claims = @(
    @{ file = 'docs/basic-to-advanced-learning-overview.md'; rx = 'Currently done:[^\d]*1\S(\d+)' },
    @{ file = 'docs/superpowers/specs/README.md';            rx = 'Covers topics 1\S(\d+)' }
)
foreach ($c in $claims) {
    $m = [regex]::Match((Get-Content -Encoding UTF8 (Join-Path $repo $c.file) -Raw), $c.rx)
    if (-not $m.Success)                           { Warn "$($c.file): could not find the 'topics 1-N' claim to compare (pattern changed?)" }
    elseif ([int]$m.Groups[1].Value -ne $doneMax)  { Fail "$($c.file) says topics 1-$($m.Groups[1].Value) but ROADMAP has topics up to $doneMax marked done" }
}
$shipped = [regex]::Matches((Get-Content -Encoding UTF8 (Join-Path $specRoot 'learning-topics/overview.md') -Raw), '(?m)^\|\s*(\d+)\s*\|\s*[A-Za-z]').Count
if ($shipped -ne $doneNumbers.Count) { Fail "learning-topics/overview.md shipped-topics table has $shipped rows but ROADMAP has $($doneNumbers.Count) done topics" }
if (-not $fails) { Pass "docs agree: $($doneNumbers.Count) topic(s) done (highest #$doneMax)" }

Write-Host "`n=== Hygiene ===" -ForegroundColor Cyan
foreach ($f in $mdFiles) { if ($f.Name -match '\d{4}-\d{2}-\d{2}') { Fail "date in file name: $($f.Name)" } }
foreach ($f in Get-ChildItem $specRoot -Recurse -Filter '*.md') {
    if ((Get-Content -Encoding UTF8 $f.FullName -Raw) -match '\b[A-Z]{2,}-\d{2,}\b') { Warn "possible ticket number inside spec: $($f.Name)" }
}
foreach ($f in $mdFiles) { if ((Get-Content -Encoding UTF8 $f.FullName -Raw) -match 'TODO:') { Warn "open TODO in $($f.FullName.Substring($repo.Length + 1))" } }
Pass 'hygiene checked (open TODOs are listed above as WARN so none are forgotten)'

Write-Host "`nResult: $fails FAIL, $warns WARN" -ForegroundColor $(if ($fails) { 'Red' } elseif ($warns) { 'Yellow' } else { 'Green' })
exit $(if ($fails) { 1 } else { 0 })
