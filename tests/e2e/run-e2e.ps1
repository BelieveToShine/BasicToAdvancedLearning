<#
  End-to-end test runner, PowerShell edition (Windows PowerShell 5.1+ or pwsh 7 on any OS). Twin of run-e2e.sh: same checks, same report.
  Start with docs/testing/README.md. Usage:
    $env:BTAL_SQLSERVER = 'Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=True;Initial Catalog=BasicToAdvancedLearning'
    pwsh tests/e2e/run-e2e.ps1                       # everything
    pwsh tests/e2e/run-e2e.ps1 -Only SqlCrud,AdoNet
    pwsh tests/e2e/run-e2e.ps1 -SkipApi -SkipFrontend -SkipSqlScripts -NoSql
    (golden files are refreshed only with the bash runner: tests/e2e/run-e2e.sh --update-golden)
  Run ONE runner at a time (lock folder tests/e2e/.run.lock). Program.cs must have no uncommitted changes.
  Exit code: 0 = every executed check passed, 1 = at least one FAIL/DIFF, 2 = cannot start (preflight).
  Differences from the bash runner: console output is compared after folding line endings (CRLF -> LF) and every non-ASCII
  character to '?', because a Windows console may not print the lessons' arrows/dashes/degree signs in UTF-8.
  The API smoke test needs bash + curl (Git-Bash is fine); without them that check is SKIPPED.
#>
param(
    [string[]]$Only = @(), [switch]$SkipApi, [switch]$SkipFrontend, [switch]$SkipSqlScripts,
    [switch]$NoSql, [switch]$SkipDockerConfig
)
$ErrorActionPreference = 'Continue'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$E2E = Join-Path $Root 'tests/e2e'; $Act = Join-Path $E2E 'actual'; $Console = Join-Path $Root 'BasicToAdvancedLearning.Console'
$ProgramChanges = (& git -C $Root status --porcelain -- BasicToAdvancedLearning.Console/Program.cs 2>$null)
if ($ProgramChanges) { Write-Host 'BasicToAdvancedLearning.Console/Program.cs has uncommitted changes; the runner rewires and then restores it with git, which would destroy them. Commit or stash it first.'; exit 2 }
$Lock = Join-Path $E2E '.run.lock'
if (Test-Path $Lock) { Write-Host 'Another run is in progress (lock folder tests/e2e/.run.lock). If it is stale, delete that folder and retry.'; exit 2 }
New-Item -ItemType Directory -Path $Lock | Out-Null
$env:MSBUILDDISABLENODEREUSE = '1'; $env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'; $env:UseSharedCompilation = 'false'   # do not leave build servers running
$OrigArgs = ($PSBoundParameters.GetEnumerator() | ForEach-Object { if ($_.Value -is [switch]) { "-$($_.Key)" } else { "-$($_.Key) $($_.Value -join ',')" } }) -join ' '
if (Test-Path $Act) { Remove-Item -Recurse -Force -LiteralPath $Act }
New-Item -ItemType Directory -Force -Path $Act | Out-Null
$script:Rows = New-Object System.Collections.Generic.List[object]
function Add-Row([string]$Name, [string]$Result, [string]$Note) {
    $script:Rows.Add([pscustomobject]@{ Name = $Name; Result = $Result; Note = $Note })
    '{0,-8} {1,-38} {2}' -f $Result, $Name, $Note | Write-Host
}
function Fold([string]$s) { ((($s -replace "`r`n", "`n") -replace '[^\x00-\x7F]', '?').TrimEnd("`n")) }
function Invoke-Captured([string]$File, [string[]]$ArgList, [string]$WorkDir, [string]$StdinText, [int]$TimeoutMs) {
    # System.Diagnostics.Process with ReadToEndAsync keeps blank lines intact (Start-Process redirection drops them on Linux/macOS).
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $File; $psi.WorkingDirectory = $WorkDir
    if ($null -ne $psi.PSObject.Properties['ArgumentList']) { foreach ($a in $ArgList) { [void]$psi.ArgumentList.Add($a) } } else { $psi.Arguments = ($ArgList -join ' ') }   # Windows PowerShell 5.1 has no ArgumentList
    $psi.RedirectStandardInput = $true; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true; $psi.UseShellExecute = $false
    $p = [System.Diagnostics.Process]::Start($psi)
    $outTask = $p.StandardOutput.ReadToEndAsync(); $errTask = $p.StandardError.ReadToEndAsync()
    if ($StdinText) { $p.StandardInput.Write($StdinText) }
    $p.StandardInput.Close()
    $done = $p.WaitForExit($TimeoutMs)
    if (-not $done) { $p.Kill() }
    [pscustomobject]@{ Out = $outTask.Result; Err = $errTask.Result; Rc = $(if ($done) { $p.ExitCode } else { -1 }); TimedOut = (-not $done) }
}
function Restore-Program { Push-Location $Root; git checkout -q -- BasicToAdvancedLearning.Console/Program.cs 2>$null; Pop-Location }

Write-Host '== Preflight'
$HostName = [System.Net.Dns]::GetHostName(); $Date = Get-Date -Format 'yyyy-MM-dd'
$Sha = (& git -C $Root rev-parse --short HEAD 2>$null); if (-not $Sha) { $Sha = 'unknown' }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Write-Host 'dotnet not found - install the .NET 9 SDK'; exit 2 }
$DotnetV = (& dotnet --version)
if ((& dotnet --list-runtimes) -match 'Microsoft\.NETCore\.App 9\.') { $Roll = 'no (a .NET 9 runtime is installed)' }
else { $env:DOTNET_ROLL_FORWARD = 'Major'; $Roll = 'YES - no .NET 9 runtime found, using DOTNET_ROLL_FORWARD=Major (results are NOT net9 results)' }
$NodeV = if (Get-Command node -ErrorAction SilentlyContinue) { (& node --version) } else { 'none' }
Write-Host "host=$HostName dotnet=$DotnetV node=$NodeV commit=$Sha roll-forward: $Roll"
if (-not $NoSql -and -not $env:BTAL_SQLSERVER) { Write-Host 'BTAL_SQLSERVER is not set. Set it (see docs/testing/runbook.md step 2) or pass -NoSql.'; exit 2 }

try {
    Write-Host '== Build (whole solution, --no-incremental)'
    $buildOut = (& dotnet build (Join-Path $Root 'BasicToAdvancedLearning.sln') --no-incremental -v q 2>&1 | Out-String); $buildRc = $LASTEXITCODE
    $w = [regex]::Match($buildOut, '\d+ Warning\(s\)').Value; $e = [regex]::Match($buildOut, '\d+ Error\(s\)').Value
    if ($buildRc -eq 0 -and $w -eq '0 Warning(s)') { Add-Row 'build solution' 'PASS' "$w, $e" }
    else { $buildOut | Set-Content (Join-Path $Act 'build.txt'); Add-Row 'build solution' 'FAIL' "$w $e (rc=$buildRc) - see tests/e2e/actual/build.txt" }

    Write-Host '== Console lessons'
    $cat = ''
    if ($env:BTAL_SQLSERVER) { $m = [regex]::Match($env:BTAL_SQLSERVER, '(?i)(?:initial catalog|database)=([^;]*)'); if ($m.Success) { $cat = $m.Groups[1].Value } }
    if (-not $cat -or $cat -eq 'master') { $cat = 'BasicToAdvancedLearning' }
    $noConn = $false
    foreach ($line in Get-Content (Join-Path $E2E 'manifest.tsv')) {
        if (-not $line -or $line.StartsWith('#')) { continue }
        $f = $line -split "`t"; $T = $f[0]; $needs = $f[1]; $stdin = $f[2]
        if ($Only.Count -gt 0 -and $Only -notcontains $T) { continue }
        if ($needs -eq 'yes' -and $NoSql) { Add-Row "lesson $T" 'SKIP' 'needs SQL Server (-NoSql)'; continue }
        if ($needs -eq 'yes' -and $noConn) { Add-Row "lesson $T" 'SKIP' 'no connection (see the first failure)'; continue }
        $prog = "using BasicToAdvancedLearning.Interfaces;`nusing BasicToAdvancedLearning.$T;`nILearningTopic topic = new ${T}Example();`ntopic.Explain();`n"
        [System.IO.File]::WriteAllText((Join-Path $Console 'Program.cs'), $prog)
        $stdinText = if ($stdin -eq '-') { '' } else { ($stdin -replace '\\n', "`n") }
        $r = Invoke-Captured 'dotnet' @('run', '-v', 'q') $Console $stdinText 600000
        if ($r.TimedOut) { Add-Row "lesson $T" 'FAIL' 'timed out after 10 minutes'; continue }
        $rc = $r.Rc; $text = $r.Out; $errText = $r.Err
        $errFile = Join-Path $Act "$T.err"; Set-Content -LiteralPath $errFile -Value $errText -NoNewline
        $text = $text.Replace("${cat}_${T}", '<DB>')
        $actFile = Join-Path $Act "$T.txt"; Set-Content -LiteralPath $actFile -Value (Fold $text) -NoNewline
        if ($text -match 'Could not connect to SQL Server') { $noConn = $true; Add-Row "lesson $T" 'FAIL' ('cannot connect to SQL Server: ' + ($text -split "`n" | Where-Object { $_ -match 'Could not connect' } | Select-Object -First 1)); continue }
        if ($needs -eq 'yes' -and $text -match 'Set the BTAL_SQLSERVER') { Add-Row "lesson $T" 'FAIL' 'lesson skipped itself: BTAL_SQLSERVER missing'; continue }
        $gold = Join-Path $E2E "expected/$T.txt"
        if ($rc -ne 0 -or $errText -match 'Unhandled exception') { Add-Row "lesson $T" 'FAIL' "crashed (rc=$rc): see tests/e2e/actual/$T.err"; continue }
        if (-not (Test-Path $gold)) { Add-Row "lesson $T" 'WARN' 'ran OK but there is no golden file'; continue }
        $a = Fold ([System.IO.File]::ReadAllText($actFile)); $g = Fold ([System.IO.File]::ReadAllText($gold))
        if ($a -ceq $g) { Add-Row "lesson $T" 'PASS' "identical to golden ($(($a -split "`n").Count) lines)" }
        else {
            $al = $a -split "`n"; $gl = $g -split "`n"
            $d = Compare-Object $gl $al | ForEach-Object { "$($_.SideIndicator) $($_.InputObject)" }
            $d | Set-Content (Join-Path $Act "$T.diff")
            Add-Row "lesson $T" 'DIFF' "$($d.Count) changed lines: see tests/e2e/actual/$T.diff  (<= golden only, => this run only)"
        }
    }
    Restore-Program

    if ($SkipSqlScripts -or $NoSql -or $Only.Count -gt 0) { Add-Row 'sql scripts 01-04' 'SKIP' 'not run (-SkipSqlScripts, -NoSql or -Only)' }
    if (-not $SkipSqlScripts -and -not $NoSql -and $Only.Count -eq 0) {
        Write-Host '== SQL scripts (sql/01..04 in a throwaway database)'
        $sqlcmd = $null; $sqlArgs = @()
        if ($env:E2E_SQLCMD) { $parts = $env:E2E_SQLCMD -split ' '; $sqlcmd = $parts[0]; $sqlArgs = $parts[1..($parts.Count - 1)] }
        elseif (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
            $cs = $env:BTAL_SQLSERVER
            $srv = [regex]::Match($cs, '(?i)(?:server|data source)=([^;]*)').Groups[1].Value
            $usr = [regex]::Match($cs, '(?i)(?:user id|uid)=([^;]*)').Groups[1].Value
            $pw = [regex]::Match($cs, '(?i)(?:password|pwd)=([^;]*)').Groups[1].Value
            if ($srv -and $usr) { $sqlcmd = 'sqlcmd'; $sqlArgs = @('-C', '-S', $srv, '-U', $usr, '-P', $pw) }
        }
        if (-not $sqlcmd) { Add-Row 'sql scripts 01-04' 'SKIP' 'no sqlcmd (install it or set E2E_SQLCMD; see docs/testing/runbook.md)' }
        else {
            $tdb = 'BasicToAdvancedLearning_E2eSqlScripts'; $ok = $true; $det = ''
            & $sqlcmd @sqlArgs -b -Q "IF DB_ID('$tdb') IS NOT NULL BEGIN ALTER DATABASE [$tdb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$tdb]; END; CREATE DATABASE [$tdb]" *> (Join-Path $Act 'sql-create.txt')
            if ($LASTEXITCODE -ne 0) { $ok = $false; $det = 'create database failed (see tests/e2e/actual/sql-create.txt)' }
            foreach ($s in '01-bank-schema', '02-bank-seed', '03-verify-bank-schema', '01-bank-schema', '02-bank-seed', '03-verify-bank-schema', '04-reset-bank', '01-bank-schema', '03-verify-bank-schema') {
                if (-not $ok) { break }
                # the file is fed on stdin so the same code works for a local sqlcmd and for "docker exec -i ... sqlcmd"
                Get-Content -Raw (Join-Path $Root "sql/$s.sql") | & $sqlcmd @sqlArgs -b -d $tdb *>> (Join-Path $Act 'sql-scripts.txt')
                if ($LASTEXITCODE -ne 0) { $ok = $false; $det = "$s.sql failed (see tests/e2e/actual/sql-scripts.txt)" }
            }
            & $sqlcmd @sqlArgs -b -Q "ALTER DATABASE [$tdb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$tdb]" *> $null
            if ($ok) { Add-Row 'sql scripts 01-04 (idempotent)' 'PASS' 'schema, seed, verify, reset, schema again: all ran' } else { Add-Row 'sql scripts 01-04' 'FAIL' $det }
        }
    }

    if ($SkipApi -or $NoSql -or $Only.Count -gt 0) { Add-Row 'api smoke test' 'SKIP' 'not run (-SkipApi, -NoSql or -Only)' }
    if (-not $SkipApi -and -not $NoSql -and $Only.Count -eq 0) {
        Write-Host '== FullStackBank API + smoke.sh'
        $dll = Join-Path $Root 'FullStackBank/Api/bin/Debug/net9.0/FullStackBank.Api.dll'
        $bash = Get-Command bash -ErrorAction SilentlyContinue; $curl = Get-Command curl -ErrorAction SilentlyContinue
        if (-not (Test-Path $dll)) { Add-Row 'api smoke test' 'FAIL' 'API was not built (see the build result)' }
        elseif (-not $bash -or -not $curl) { Add-Row 'api smoke test' 'SKIP' 'needs bash and curl on PATH (Git-Bash)' }
        else {
            $env:ASPNETCORE_ENVIRONMENT = 'Development'
            $api = Start-Process -FilePath dotnet -ArgumentList @($dll, '--urls', 'http://localhost:5290') -WorkingDirectory (Join-Path $Root 'FullStackBank/Api') -NoNewWindow -PassThru `
                   -RedirectStandardOutput (Join-Path $Act 'api.log') -RedirectStandardError (Join-Path $Act 'api.err')
            $env:ASPNETCORE_ENVIRONMENT = $null
            $up = $false
            for ($i = 0; $i -lt 45; $i++) { try { if ((Invoke-WebRequest -UseBasicParsing http://localhost:5290/health -TimeoutSec 3).StatusCode -eq 200) { $up = $true; break } } catch { }; if ($api.HasExited) { break }; Start-Sleep 2 }
            if (-not $up) { Add-Row 'api smoke test' 'FAIL' 'API did not become healthy (see tests/e2e/actual/api.err)' }
            else {
                $smoke = (Join-Path $Root 'FullStackBank/smoke.sh') -replace '\\', '/'
                & bash $smoke http://localhost:5290 *> (Join-Path $Act 'smoke.txt'); $r1 = $LASTEXITCODE
                & bash $smoke http://localhost:5290 *> (Join-Path $Act 'smoke2.txt'); $r2 = $LASTEXITCODE
                if ($r1 -eq 0 -and $r2 -eq 0) { Add-Row 'api smoke test (twice)' 'PASS' ((Get-Content (Join-Path $Act 'smoke2.txt'))[-1]) } else { Add-Row 'api smoke test' 'FAIL' 'see tests/e2e/actual/smoke.txt and smoke2.txt' }
            }
            if (-not $api.HasExited) { $api.Kill() }
        }
    }

    if ($SkipFrontend -or $Only.Count -gt 0) { Add-Row 'frontend tests' 'SKIP' 'not run (-SkipFrontend or -Only)' }
    if (-not $SkipFrontend -and $Only.Count -eq 0) {
        Write-Host '== Frontend (vitest + build)'
        if ($NodeV -eq 'none') { Add-Row 'frontend tests' 'SKIP' 'node is not installed' }
        else {
            Push-Location (Join-Path $Root 'frontend')
            & npm install --no-audit --no-fund *> (Join-Path $Act 'npm.txt')
            & npx vitest run *> (Join-Path $Act 'vitest.txt'); $rc = $LASTEXITCODE
            $ts = (Get-Content (Join-Path $Act 'vitest.txt') | Where-Object { $_ -match '^\s+Tests ' } | Select-Object -First 1)
            if ($rc -eq 0) { Add-Row 'frontend vitest' 'PASS' "$ts".Trim() } else { Add-Row 'frontend vitest' 'FAIL' "$ts (see tests/e2e/actual/vitest.txt)" }
            & npx vite build *> (Join-Path $Act 'vite-build.txt')
            if ($LASTEXITCODE -eq 0) { Add-Row 'frontend vite build' 'PASS' 'ok' } else { Add-Row 'frontend vite build' 'FAIL' 'see tests/e2e/actual/vite-build.txt' }
            Pop-Location
        }
    }

    if (-not $SkipDockerConfig -and $Only.Count -eq 0 -and (Get-Command docker -ErrorAction SilentlyContinue)) {
        Push-Location (Join-Path $Root 'FullStackBank/deploy')
        & docker compose --env-file .env.example config -q *> (Join-Path $Act 'compose.txt')
        if ($LASTEXITCODE -eq 0) { Add-Row 'docker compose config' 'PASS' '.env.example parses (no containers started; full stack test: docs/testing/runbook.md)' } else { Add-Row 'docker compose config' 'FAIL' 'see tests/e2e/actual/compose.txt' }
        Pop-Location
    }
}
finally { Restore-Program; if (Test-Path $Lock) { Remove-Item -Recurse -Force -LiteralPath $Lock } }

# ---- report
$count = { param($r) @($script:Rows | Where-Object { $_.Result -eq $r }).Count }
$P = (& $count 'PASS') + (& $count 'UPDATED'); $F = & $count 'FAIL'; $D = & $count 'DIFF'; $S = & $count 'SKIP'; $WN = & $count 'WARN'
$repDir = Join-Path $Root 'docs/testing/results'; New-Item -ItemType Directory -Force -Path $repDir | Out-Null
$rep = Join-Path $repDir "$Sha-$HostName.md"; $n = 2
while (Test-Path $rep) { $rep = Join-Path $repDir "$Sha-$HostName-$n.md"; $n++ }   # never overwrite an earlier report
$Dirty = @(& git -C $Root status --porcelain 2>$null | Where-Object { $_ -notmatch ' (docs/testing/results/|tests/e2e/actual/)' }).Count
$sqlVer = if ($NoSql) { 'none' } elseif ($env:E2E_SQL_VERSION) { $env:E2E_SQL_VERSION } else { '(not recorded: put the output of SELECT @@VERSION here)' }
$md = @("# E2E result — $Date on $HostName (commit $Sha)", '',
        "- commit: ``$Sha``  ·  dotnet: ``$DotnetV``  ·  node: ``$NodeV``  ·  roll-forward used: $Roll",
        "- command: ``tests/e2e/run-e2e.ps1 $OrigArgs``  ·  uncommitted changes in the working tree when the report was written: $Dirty file(s) (0 = the commit above is exactly what was tested)",
        "- SQL Server: $sqlVer", "- totals: PASS $P · DIFF $D · FAIL $F · WARN $WN · SKIP $S", '',
        '| Check | Result | Notes |', '|---|---|---|')
foreach ($r in $script:Rows) { $md += "| $($r.Name) | $($r.Result) | $(($r.Note) -replace '\|', '\|') |" }
$md += ''; $md += 'Diffs and raw outputs: `tests/e2e/actual/` (git-ignored). Triage every DIFF/FAIL with `docs/testing/triage-guide.md`.'
Set-Content -LiteralPath $rep -Value $md
Write-Host ''; Write-Host "PASS $P · DIFF $D · FAIL $F · WARN $WN · SKIP $S   -> report: docs/testing/results/$(Split-Path -Leaf $rep)"
if ($F -eq 0 -and $D -eq 0) { exit 0 } else { exit 1 }
