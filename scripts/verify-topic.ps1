<#
.SYNOPSIS
  Mechanical verification of one lesson topic (or all) against docs/rules/diagram-standards.md.
.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-topic.ps1 -Topic Collections
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-topic.ps1 -All -SkipBuild
.NOTES
  FAIL = rule broken (exit code 1). WARN = heuristic, needs a human look. PASS = checked and fine.
  This script cannot judge visual quality (text fit, label clearance, clarity) - that is the manual
  review in docs/rules/verification.md.
#>
param(
    [string]$Topic,
    [switch]$All,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repo      = Split-Path -Parent $PSScriptRoot
$codeRoot  = Join-Path $repo 'BasicToAdvancedLearning.Console'
$diagRoot  = Join-Path $repo 'ArchitectureDiagrams'
$script:fails = 0
$script:warns = 0

function Pass([string]$m) { Write-Host "  PASS  $m" -ForegroundColor Green }
function Warn([string]$m) { Write-Host "  WARN  $m" -ForegroundColor Yellow; $script:warns++ }
function Fail([string]$m) { Write-Host "  FAIL  $m" -ForegroundColor Red;    $script:fails++ }

$memoryColours = @('b6d7a8', 'f9cb9c', 'd5c9ea')   # locked Stack / Heap / Static fills
$reserved = @('return','else','new','var','using','namespace','public','private','internal','static','class','break','continue','case','default','throw','in','is','as','out','ref')

function Test-Topic([string]$name) {
    Write-Host "`n=== Topic: $name ===" -ForegroundColor Cyan
    $codeDir = Join-Path $codeRoot $name
    $diagDir = Join-Path $diagRoot $name
    if (-not (Test-Path $codeDir)) { Fail "code folder missing: BasicToAdvancedLearning.Console/$name"; return }
    if (-not (Test-Path $diagDir)) { Fail "diagram folder missing: ArchitectureDiagrams/$name"; return }

    # --- code files -------------------------------------------------------------------------
    $exampleFile = Get-ChildItem $codeDir -Filter '*Example.cs' | Select-Object -First 1
    # The demo is the non-adapter file that defines the public ExplainX() methods (a topic may also hold helper-class files).
    $demoFile    = Get-ChildItem $codeDir -Filter '*.cs' | Where-Object { $_.Name -notlike '*Example.cs' -and (Get-Content -Encoding UTF8 $_.FullName -Raw) -match 'public\s+void\s+Explain\w+\s*\(' } | Select-Object -First 1
    if (-not $exampleFile -or -not $demoFile) { Fail "need one *Example.cs and one demo .cs in the folder"; return }
    Pass "found $($demoFile.Name) + $($exampleFile.Name)"

    $demoText    = Get-Content $demoFile.FullName -Raw
    $exampleText = Get-Content $exampleFile.FullName -Raw

    foreach ($f in @($demoFile, $exampleFile)) {
        $ns = [regex]::Match((Get-Content $f.FullName -Raw), '(?m)^\s*namespace\s+([\w\.]+)').Groups[1].Value
        if ($ns -notmatch "^BasicToAdvancedLearning\.$name$") { Fail "$($f.Name): namespace '$ns' should be BasicToAdvancedLearning.$name" }
        elseif ($ns -split '\.' -contains 'Console')          { Fail "$($f.Name): namespace has a 'Console' segment" }
        else { Pass "$($f.Name): namespace $ns" }
    }
    if ($exampleText -notmatch ':\s*ILearningTopic') { Fail "$($exampleFile.Name) does not implement ILearningTopic" } else { Pass "adapter implements ILearningTopic" }

    # Methods in the order Explain() calls them (this defines the diagram numbering).
    $called  = [regex]::Matches($exampleText, '\.Explain(\w+)\s*\(') | ForEach-Object { $_.Groups[1].Value }
    $defined = [regex]::Matches($demoText, 'public\s+void\s+Explain(\w+)\s*\(') | ForEach-Object { $_.Groups[1].Value }
    if (-not $called) { Fail "no ExplainX() calls found in $($exampleFile.Name)"; return }
    foreach ($m in $defined) { if ($called -notcontains $m) { Fail "Explain$m is defined but never called by the adapter" } }
    foreach ($m in $called)  { if ($defined -notcontains $m) { Fail "adapter calls Explain$m but the demo does not define it" } }
    Pass "$($called.Count) method(s) in call order: $($called -join ', ')"

    # --- diagram set ------------------------------------------------------------------------
    $expected = @("$name.svg", 'Flow/0-Overview.svg', 'Memory/0-Overview.svg')
    $i = 1
    foreach ($m in $called) { $expected += "Flow/$i-$m.svg"; $expected += "Memory/$i-$m.svg"; $i++ }
    foreach ($e in $expected) {
        if (-not (Test-Path (Join-Path $diagDir $e))) { Fail "missing diagram: ArchitectureDiagrams/$name/$e" }
    }
    $actual = Get-ChildItem $diagDir -Recurse -Filter '*.svg' | ForEach-Object { $_.FullName.Substring($diagDir.Length + 1).Replace('\', '/') }
    foreach ($a in $actual) { if ($expected -notcontains $a) { Warn "unexpected diagram (name/number mismatch with call order?): $a" } }
    if (-not ($expected | Where-Object { -not (Test-Path (Join-Path $diagDir $_)) })) { Pass "all $($expected.Count) required diagrams exist, numbering matches call order" }

    # --- per-diagram checks -----------------------------------------------------------------
    foreach ($e in $expected) {
        $p = Join-Path $diagDir $e
        if (-not (Test-Path $p)) { continue }
        $xml = New-Object System.Xml.XmlDocument
        try { $xml.Load($p) } catch { Fail "$e is not well-formed XML: $($_.Exception.Message)"; continue }

        $texts = ($xml.GetElementsByTagName('text') | ForEach-Object { $_.InnerText }) -join ' '
        $raw   = Get-Content $p -Raw
        if ($raw -notmatch 'font-family="Arial') { Warn "${e}: font-family Arial not found" }

        if ($e -like 'Flow/*') {
            $diag = 0; $drift = 0
            foreach ($ln in $xml.GetElementsByTagName("line")) {
                if (-not ($ln.x1 -and $ln.x2 -and $ln.y1 -and $ln.y2)) { continue }
                $dx = [math]::Abs([double]$ln.x2 - [double]$ln.x1); $dy = [math]::Abs([double]$ln.y2 - [double]$ln.y1)
                if ($dx -gt 2 -and $dy -gt 2) { $diag++ } elseif (($dx -gt 0 -and $dy -gt 0)) { $drift++ }
            }
            if ($diag -gt 0) { Fail "${e}: $diag diagonal <line> connector(s) (right-angle routing only)" }
            if ($drift -gt 0) { Warn "${e}: $drift near-straight <line>(s) with 1-2px drift - snap to exact horizontal/vertical" }
            foreach ($pa in $xml.GetElementsByTagName('path')) {
                if ($pa.ParentNode.LocalName -eq 'marker') { continue }   # arrowheads are triangles by design
                $d = $pa.GetAttribute('d')
                if ($d -match '[CcQqSsTtAa]') { continue }          # curves: decorative (flags, arcs) - not routing
                $nums = [regex]::Matches($d, '[ML]\s*(-?[\d\.]+)[ ,]+(-?[\d\.]+)')
                for ($k = 1; $k -lt $nums.Count; $k++) {
                    if ($nums[$k].Groups[1].Value -ne $nums[$k-1].Groups[1].Value -and $nums[$k].Groups[2].Value -ne $nums[$k-1].Groups[2].Value) {
                        Warn "${e}: path has a diagonal segment ($d)"; break
                    }
                }
            }
        }
        if ($e -like 'Memory/*') {
            foreach ($c in $memoryColours) { if ($raw -notmatch $c) { Warn "${e}: locked legend colour #$c missing (OK only for a documented exception, e.g. Memory/2-Parameters)" } }
            if ($e -notlike 'Memory/0-*') {
                foreach ($w in @('Stack', 'Heap', 'Static')) { if ($texts -notmatch $w) { Fail "${e}: no '$w' column text" } }
                if ($texts -notmatch 'Main') { Warn "${e}: Program.cs (Main) frame not mentioned" }
                if ($texts -notmatch 'Explain\(\)') { Warn "${e}: Explain() frame not mentioned" }
            }
        }
    }
    Pass "XML + per-diagram rules checked for $($expected.Count) file(s)"

    # --- cross-check: variable names in code appear in the method's Memory diagram -----------
    $i = 1
    foreach ($m in $called) {
        $mp = Join-Path $diagDir "Memory/$i-$m.svg"
        # Extract the method body by brace matching, so any indentation / namespace style works.
        $body = ''
        $start = [regex]::Match($demoText, "public\s+void\s+Explain$m\s*\(\)\s*\{")
        if ($start.Success) {
            $depth = 1; $pos = $start.Index + $start.Length
            while ($pos -lt $demoText.Length -and $depth -gt 0) {
                $ch = $demoText[$pos]
                if ($ch -eq '{') { $depth++ } elseif ($ch -eq '}') { $depth-- }
                $pos++
            }
            $body = $demoText.Substring($start.Index + $start.Length, $pos - ($start.Index + $start.Length) - 1)
        }
        if (-not $body) { Warn "could not extract the body of Explain$m (needs a parameterless public void method) - variable cross-check skipped" }
        if ((Test-Path $mp) -and $body) {
            $svgText = (Get-Content $mp -Raw)
            $vars = @()
            $vars += [regex]::Matches($body, '(?m)^\s*(?:const\s+)?[\w<>\[\],\?]+\s+([a-z]\w*)\s*(?:=|;)') | ForEach-Object { $_.Groups[1].Value }
            $vars += [regex]::Matches($body, '(?:foreach|for)\s*\(\s*[\w<>\[\],]+\s+(\w+)') | ForEach-Object { $_.Groups[1].Value }
            $vars = $vars | Where-Object { $reserved -notcontains $_ } | Select-Object -Unique
            $missing = $vars | Where-Object { $svgText -notmatch "\b$_\b" }
            if ($missing) { Warn "Memory/$i-$m.svg does not mention code variable(s): $($missing -join ', ')" }
            else          { Pass "Memory/$i-$m.svg mentions all $(@($vars).Count) local variable name(s)" }
        }
        $i++
    }

    # --- wiring + tracker ---------------------------------------------------------------------
    $prog = Get-Content (Join-Path $codeRoot 'Program.cs') -Raw
    $exName = [IO.Path]::GetFileNameWithoutExtension($exampleFile.Name)
    if ($prog -match "new\s+$exName\s*\(") { Pass "Program.cs runs $exName" } else { Warn "Program.cs does not currently run $exName (fine unless this is the topic being finished)" }

    $row = (Get-Content -Encoding UTF8 (Join-Path $repo "docs/ROADMAP.md")) | Where-Object { $_ -match [regex]::Escape("``$name/``") } | Select-Object -First 1
    if (-not $row)                { Fail "docs/ROADMAP.md has no tracker row mentioning ``$name/``" }
    elseif ($row -notmatch [string][char]0x2705) { Warn "ROADMAP row for $name is not marked done yet" }
    else                          { Pass "ROADMAP tracker row marked done" }
}

if ($All) {
    $topics = Get-ChildItem $codeRoot -Directory | Where-Object { $_.Name -ne 'Interfaces' -and $_.Name -notin 'bin', 'obj' } | ForEach-Object Name
} elseif ($Topic) { $topics = @($Topic) }
else { Write-Host 'Usage: verify-topic.ps1 -Topic <Name> | -All [-SkipBuild]'; exit 2 }

foreach ($t in $topics) { Test-Topic $t }

if (-not $SkipBuild) {
    Write-Host "`n=== dotnet build ===" -ForegroundColor Cyan
    $out = & dotnet build (Join-Path $repo 'BasicToAdvancedLearning.sln') --nologo -v q 2>&1 | Out-String
    $buildExit = $LASTEXITCODE
    $w = [regex]::Match($out, '(\d+) Warning\(s\)'); $er = [regex]::Match($out, '(\d+) Error\(s\)')
    if (-not $w.Success -or -not $er.Success) { Fail "could not read the build summary (exit code $buildExit) - run dotnet build manually" }
    else {
        if ([int]$w.Groups[1].Value -gt 0)  { Fail "build has $($w.Groups[1].Value) warning(s)" } else { Pass "0 warnings" }
        if ([int]$er.Groups[1].Value -gt 0) { Fail "build has $($er.Groups[1].Value) error(s)" }   else { Pass "0 errors" }
        if ($buildExit -ne 0 -and [int]$er.Groups[1].Value -eq 0) { Fail "dotnet build exit code $buildExit despite 0 errors reported" }
    }
}

Write-Host "`nResult: $script:fails FAIL, $script:warns WARN" -ForegroundColor $(if ($script:fails) { 'Red' } elseif ($script:warns) { 'Yellow' } else { 'Green' })
Write-Host 'Reminder: this does not judge text fit, label clearance or clarity - do the manual review in docs/rules/verification.md.'
exit $(if ($script:fails) { 1 } else { 0 })
