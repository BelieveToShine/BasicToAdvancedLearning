#!/usr/bin/env bash
# End-to-end test runner (Linux / macOS / Git-Bash). Windows PowerShell twin: run-e2e.ps1 (same checks, same report).
# Start with docs/testing/README.md. Usage:
#   export BTAL_SQLSERVER='Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=True;Initial Catalog=BasicToAdvancedLearning'
#   tests/e2e/run-e2e.sh                     # everything
#   tests/e2e/run-e2e.sh --only SqlCrud,AdoNet
#   tests/e2e/run-e2e.sh --skip-api --skip-frontend --skip-sqlscripts --no-sql
#   tests/e2e/run-e2e.sh --skip-docker-config
#   tests/e2e/run-e2e.sh --update-golden     # DEV ONLY: overwrite tests/e2e/expected/*.txt with this run's output
# Run ONE runner at a time (a lock folder tests/e2e/.run.lock enforces it). Program.cs must have no uncommitted changes.
# Exit code: 0 = every executed check passed, 1 = at least one FAIL/DIFF, 2 = cannot start (preflight).
set -u
ORIG_ARGS="$*"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
E2E="$ROOT/tests/e2e"; ACT="$E2E/actual"; CONSOLE="$ROOT/BasicToAdvancedLearning.Console"
ONLY=""; SKIP_API=0; SKIP_FE=0; SKIP_SQLS=0; NOSQL=0; UPDATE=0; DOCKER_CFG=1
while [ $# -gt 0 ]; do case "$1" in
  --only) ONLY="$2"; shift;; --skip-api) SKIP_API=1;; --skip-frontend) SKIP_FE=1;; --skip-sqlscripts) SKIP_SQLS=1;;
  --no-sql) NOSQL=1;; --update-golden) UPDATE=1;; --skip-docker-config) DOCKER_CFG=0;;
  -h|--help) sed -n 2,15p "$0"; exit 0;; *) echo "unknown option $1"; exit 2;; esac; shift; done

if [ -n "$(cd "$ROOT" && git status --porcelain -- BasicToAdvancedLearning.Console/Program.cs 2>/dev/null)" ]; then
  echo "BasicToAdvancedLearning.Console/Program.cs has uncommitted changes; the runner rewires and then restores it with git, which would destroy them. Commit or stash it first."; exit 2; fi
LOCK="$E2E/.run.lock"
if ! mkdir "$LOCK" 2>/dev/null; then echo "Another run is in progress (lock folder tests/e2e/.run.lock). If it is stale, delete that folder and retry."; exit 2; fi
# Start every run with an empty actual/ folder (git-ignored). The :? guards stop the shell if a variable is ever empty.
rm -rf "${ACT:?}"; mkdir -p "${ACT:?}"
declare -a NAMES RESULTS NOTES
add() { NAMES+=("$1"); RESULTS+=("$2"); NOTES+=("$3"); printf '%-8s %-38s %s\n' "$2" "$1" "$3"; }
API_PID=""
restore() { (cd "$ROOT" && git checkout -q -- BasicToAdvancedLearning.Console/Program.cs 2>/dev/null); [ -n "$API_PID" ] && kill "$API_PID" 2>/dev/null; rmdir "$LOCK" 2>/dev/null; return 0; }
trap restore EXIT

echo "== Preflight"
HOST="$(hostname 2>/dev/null || echo unknown)"; DATE="$(date +%Y-%m-%d)"; SHA="$(cd "$ROOT" && git rev-parse --short HEAD 2>/dev/null || echo unknown)"
command -v dotnet >/dev/null || { echo "dotnet not found - install the .NET 9 SDK"; exit 2; }
DOTNET_V="$(dotnet --version)"
if dotnet --list-runtimes | grep -q "Microsoft.NETCore.App 9\."; then ROLL="no (a .NET 9 runtime is installed)"; else export DOTNET_ROLL_FORWARD=Major; ROLL="YES - no .NET 9 runtime found, using DOTNET_ROLL_FORWARD=Major (results are NOT net9 results)"; fi
export MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false   # do not leave build servers running
NODE_V="$(node --version 2>/dev/null || echo none)"
DIRTY="$(cd "$ROOT" && git status --porcelain 2>/dev/null | grep -v -E ' (docs/testing/results/|tests/e2e/actual/)' | wc -l | tr -d ' ')"
echo "host=$HOST dotnet=$DOTNET_V node=$NODE_V commit=$SHA roll-forward: $ROLL"
if [ "$NOSQL" = 0 ] && [ -z "${BTAL_SQLSERVER:-}" ]; then echo "BTAL_SQLSERVER is not set. Set it (see docs/testing/runbook.md step 2) or pass --no-sql."; exit 2; fi

echo "== Build (whole solution, --no-incremental)"
BUILD_OUT="$(cd "$ROOT" && dotnet build BasicToAdvancedLearning.sln --no-incremental -v q 2>&1)"; BUILD_RC=$?
W="$(echo "$BUILD_OUT" | grep -Eo '[0-9]+ Warning\(s\)' | head -1)"; E="$(echo "$BUILD_OUT" | grep -Eo '[0-9]+ Error\(s\)' | head -1)"
if [ $BUILD_RC = 0 ] && [ "$W" = "0 Warning(s)" ]; then add "build solution" PASS "$W, $E"; else add "build solution" FAIL "$W $E (rc=$BUILD_RC) - see tests/e2e/actual/build.txt"; echo "$BUILD_OUT" > "$ACT/build.txt"; fi

echo "== Console lessons"
NOCONN=0
# Initial Catalog from BTAL_SQLSERVER (SqlLab uses <Initial Catalog>_<Folder>, or BasicToAdvancedLearning_<Folder> when it is empty or master)
CAT="$(echo "${BTAL_SQLSERVER:-}" | tr ';' '\n' | grep -i -m1 -E '^(initial catalog|database)=' | cut -d= -f2-)"
if [ -z "$CAT" ] || [ "$CAT" = master ]; then CAT=BasicToAdvancedLearning; fi
while IFS=$'\t' read -r T NEEDS STDIN PHASE; do
  case "$T" in ''|\#*) continue;; esac
  if [ -n "$ONLY" ] && ! echo ",$ONLY," | grep -q ",$T,"; then continue; fi
  if [ "$NEEDS" = yes ] && [ "$NOSQL" = 1 ]; then add "lesson $T" SKIP "needs SQL Server (--no-sql)"; continue; fi
  if [ "$NOCONN" = 1 ] && [ "$NEEDS" = yes ]; then add "lesson $T" SKIP "no connection (see the first failure)"; continue; fi
  printf 'using BasicToAdvancedLearning.Interfaces;\nusing BasicToAdvancedLearning.%s;\nILearningTopic topic = new %sExample();\ntopic.Explain();\n' "$T" "$T" > "$CONSOLE/Program.cs"
  if [ "$STDIN" = "-" ]; then IN=/dev/null; else printf "$STDIN" > "$ACT/$T.in"; IN="$ACT/$T.in"; fi
  ( cd "$CONSOLE" && timeout 600 dotnet run -v q < "$IN" > "$ACT/$T.txt" 2>&1 ); RC=$?
  # The database name appears in some SQL Server error messages and depends on the reader's Initial Catalog: normalise it.
  if [ -n "$CAT" ]; then sed -i.bak "s/${CAT}_${T}/<DB>/g" "$ACT/$T.txt"; rm -f "${ACT:?}/${T:?}.txt.bak"; fi
  if grep -q "Could not connect to SQL Server" "$ACT/$T.txt"; then NOCONN=1; add "lesson $T" FAIL "cannot connect to SQL Server: $(grep -m1 'Could not connect' "$ACT/$T.txt" | cut -c1-150)"; continue; fi
  if grep -q "Set the BTAL_SQLSERVER" "$ACT/$T.txt" && [ "$NEEDS" = yes ]; then add "lesson $T" FAIL "lesson skipped itself: BTAL_SQLSERVER missing"; continue; fi
  if [ "$UPDATE" = 1 ]; then cp "$ACT/$T.txt" "$E2E/expected/$T.txt"; add "lesson $T" UPDATED "golden file overwritten"; continue; fi
  if [ $RC != 0 ] || grep -q "Unhandled exception" "$ACT/$T.txt"; then add "lesson $T" FAIL "crashed (rc=$RC): see tests/e2e/actual/$T.txt"; continue; fi
  if [ ! -f "$E2E/expected/$T.txt" ]; then add "lesson $T" WARN "ran OK but there is no golden file"; continue; fi
  if diff -u "$E2E/expected/$T.txt" "$ACT/$T.txt" > "$ACT/$T.diff"; then add "lesson $T" PASS "identical to golden ($(wc -l < "$ACT/$T.txt") lines)"
  else add "lesson $T" DIFF "$(grep -c '^[-+][^-+]' "$ACT/$T.diff") changed lines: see tests/e2e/actual/$T.diff"; fi
done < "$E2E/manifest.tsv"
(cd "$ROOT" && git checkout -q -- BasicToAdvancedLearning.Console/Program.cs)

if [ "$SKIP_SQLS" = 1 ] || [ "$NOSQL" = 1 ] || [ -n "$ONLY" ]; then add "sql scripts 01-04" SKIP "not run (--skip-sqlscripts, --no-sql or --only)"; fi
if [ "$SKIP_SQLS" = 0 ] && [ "$NOSQL" = 0 ] && [ -z "$ONLY" ]; then
  echo "== SQL scripts (sql/01..04 in a throwaway database)"
  # sqlcmd command: E2E_SQLCMD wins (e.g. "docker exec -i sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P ..."),
  # otherwise derived from BTAL_SQLSERVER (SQL authentication only) plus a local sqlcmd.
  SQLCMD=""
  if [ -n "${E2E_SQLCMD:-}" ]; then SQLCMD="$E2E_SQLCMD"
  elif command -v sqlcmd >/dev/null; then
    CS="$BTAL_SQLSERVER"; SRV="$(echo "$CS" | tr ';' '\n' | grep -i -m1 -E '^(server|data source)=' | cut -d= -f2-)"
    USR="$(echo "$CS" | tr ';' '\n' | grep -i -m1 -E '^(user id|uid)=' | cut -d= -f2-)"; PW="$(echo "$CS" | tr ';' '\n' | grep -i -m1 -E '^(password|pwd)=' | cut -d= -f2-)"
    if [ -n "$SRV" ] && [ -n "$USR" ]; then SQLCMD="sqlcmd -C -S $SRV -U $USR -P $PW"; fi
  fi
  if [ -z "$SQLCMD" ]; then add "sql scripts 01-04" SKIP "no sqlcmd (install it or set E2E_SQLCMD; see docs/testing/runbook.md)"
  else
    TDB="BasicToAdvancedLearning_E2eSqlScripts"; OK=1; DET=""
    $SQLCMD -b -Q "IF DB_ID('$TDB') IS NOT NULL BEGIN ALTER DATABASE [$TDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$TDB]; END; CREATE DATABASE [$TDB]" > "$ACT/sql-create.txt" 2>&1 || { OK=0; DET="create database failed (see tests/e2e/actual/sql-create.txt)"; }
    for f in 01-bank-schema 02-bank-seed 03-verify-bank-schema 01-bank-schema 02-bank-seed 03-verify-bank-schema 04-reset-bank 01-bank-schema 03-verify-bank-schema; do
      [ $OK = 1 ] || break
      # the file is fed on stdin so the same code works for a local sqlcmd and for "docker exec -i ... sqlcmd"
      $SQLCMD -b -d "$TDB" < "$ROOT/sql/$f.sql" >> "$ACT/sql-scripts.txt" 2>&1 || { OK=0; DET="$f.sql failed (see tests/e2e/actual/sql-scripts.txt)"; }
    done
    $SQLCMD -b -Q "ALTER DATABASE [$TDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$TDB]" >/dev/null 2>&1
    if [ $OK = 1 ]; then add "sql scripts 01-04 (idempotent)" PASS "schema, seed, verify, reset, schema again: all ran"; else add "sql scripts 01-04" FAIL "$DET"; fi
  fi
fi

if [ "$SKIP_API" = 1 ] || [ "$NOSQL" = 1 ] || [ -n "$ONLY" ]; then add "api smoke test" SKIP "not run (--skip-api, --no-sql or --only)"; fi
if [ "$SKIP_API" = 0 ] && [ "$NOSQL" = 0 ] && [ -z "$ONLY" ]; then
  echo "== FullStackBank API + smoke.sh"
  DLL="$ROOT/FullStackBank/Api/bin/Debug/net9.0/FullStackBank.Api.dll"
  if [ ! -f "$DLL" ]; then add "api smoke test" FAIL "API was not built (see the build result)"
  else
    ( cd "$ROOT/FullStackBank/Api" && ASPNETCORE_ENVIRONMENT=Development exec dotnet "$DLL" --urls http://localhost:5290 > "$ACT/api.log" 2>&1 ) & API_PID=$!
    UP=0; for i in $(seq 1 45); do curl -fs http://localhost:5290/health >/dev/null 2>&1 && { UP=1; break; }; kill -0 "$API_PID" 2>/dev/null || break; sleep 2; done
    if [ $UP = 0 ]; then add "api smoke test" FAIL "API did not become healthy: $(tail -3 "$ACT/api.log" | tr '\n' ' ' | cut -c1-200)"
    else
      if bash "$ROOT/FullStackBank/smoke.sh" http://localhost:5290 > "$ACT/smoke.txt" 2>&1 && bash "$ROOT/FullStackBank/smoke.sh" http://localhost:5290 > "$ACT/smoke2.txt" 2>&1; then add "api smoke test (twice)" PASS "$(tail -1 "$ACT/smoke2.txt")"
      else add "api smoke test" FAIL "$(grep -h -m3 -E 'FAIL|not ok' "$ACT/smoke.txt" "$ACT/smoke2.txt" 2>/dev/null | cut -c1-160 | tr '\n' ' ')"; fi
    fi
    kill "$API_PID" 2>/dev/null; wait "$API_PID" 2>/dev/null; API_PID=""
  fi
fi

if [ "$SKIP_FE" = 1 ] || [ -n "$ONLY" ]; then add "frontend tests" SKIP "not run (--skip-frontend or --only)"; fi
if [ "$SKIP_FE" = 0 ] && [ -z "$ONLY" ]; then
  echo "== Frontend (vitest + build)"
  if [ "$NODE_V" = none ]; then add "frontend tests" SKIP "node is not installed"
  else
    ( cd "$ROOT/frontend" && npm install --no-audit --no-fund > "$ACT/npm.txt" 2>&1 && npx vitest run > "$ACT/vitest.txt" 2>&1 ); RC=$?
    TS="$(grep -E '^ +Tests ' "$ACT/vitest.txt" 2>/dev/null | sed 's/\x1b\[[0-9;]*m//g' | tr -s ' ')"
    if [ $RC = 0 ]; then add "frontend vitest" PASS "$TS"; else add "frontend vitest" FAIL "$TS (see tests/e2e/actual/vitest.txt)"; fi
    if ( cd "$ROOT/frontend" && npx vite build > "$ACT/vite-build.txt" 2>&1 ); then add "frontend vite build" PASS "ok"; else add "frontend vite build" FAIL "see tests/e2e/actual/vite-build.txt"; fi
  fi
fi

if [ "$DOCKER_CFG" = 1 ] && [ -z "$ONLY" ] && command -v docker >/dev/null 2>&1; then
  if ( cd "$ROOT/FullStackBank/deploy" && docker compose --env-file .env.example config -q ) > "$ACT/compose.txt" 2>&1; then add "docker compose config" PASS ".env.example parses (no containers started; full stack test: docs/testing/runbook.md)"
  else add "docker compose config" FAIL "see tests/e2e/actual/compose.txt"; fi
fi

# ---- report
P=0; F=0; D=0; S=0; WN=0
for r in "${RESULTS[@]}"; do case "$r" in PASS|UPDATED) P=$((P+1));; FAIL) F=$((F+1));; DIFF) D=$((D+1));; SKIP) S=$((S+1));; WARN) WN=$((WN+1));; esac; done
mkdir -p "$ROOT/docs/testing/results"; REP="$ROOT/docs/testing/results/$SHA-$HOST.md"; N=2
while [ -e "$REP" ]; do REP="$ROOT/docs/testing/results/$SHA-$HOST-$N.md"; N=$((N+1)); done   # never overwrite an earlier report
{
  echo "# E2E result — $DATE on $HOST (commit $SHA)"; echo
  echo "- commit: \`$SHA\`  ·  dotnet: \`$DOTNET_V\`  ·  node: \`$NODE_V\`  ·  roll-forward used: $ROLL"
  echo "- command: \`tests/e2e/run-e2e.sh $ORIG_ARGS\`  ·  uncommitted changes in the working tree when the run started: $DIRTY file(s) (0 = the commit above is exactly what was tested)"
  echo "- SQL Server: $( [ "$NOSQL" = 1 ] && echo none || echo "${E2E_SQL_VERSION:-(not recorded: put the output of SELECT @@VERSION here)}")"
  echo "- totals: PASS $P · DIFF $D · FAIL $F · WARN $WN · SKIP $S"; echo
  echo "| Check | Result | Notes |"; echo "|---|---|---|"
  for i in "${!NAMES[@]}"; do echo "| ${NAMES[$i]} | ${RESULTS[$i]} | ${NOTES[$i]//|/\\|} |"; done
  echo; echo "Diffs and raw outputs: \`tests/e2e/actual/\` (git-ignored). Triage every DIFF/FAIL with \`docs/testing/triage-guide.md\`."
} > "$REP"
dotnet build-server shutdown >/dev/null 2>&1 || true
echo; echo "PASS $P · DIFF $D · FAIL $F · WARN $WN · SKIP $S   -> report: ${REP#$ROOT/}"
if [ $F = 0 ] && [ $D = 0 ]; then exit 0; else exit 1; fi
