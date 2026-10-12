#!/bin/bash
# One-shot database set-up, run by the "db-init" service in docker-compose.yml (image: SQL Server, which contains
# sqlcmd). It is the "deployment step" that the API itself deliberately does not do in production.
# Every script is safe to run again, so `docker compose up` twice does no harm.
#   -b  stop with an error code if a script fails (so the API never starts on a half-built database)
#   -I  QUOTED_IDENTIFIER ON (needed by some of the objects EF Core creates)
#   -C  trust the server's self-signed certificate (fine inside a private compose network, see README)
set -euo pipefail
# An array (not one long string) keeps a password with unusual characters in one piece.
SQLCMD=(/opt/mssql-tools18/bin/sqlcmd -S db -U sa -P "$MSSQL_SA_PASSWORD" -C -b -I)

"${SQLCMD[@]}" -d master -v DB_NAME="$DB_NAME" -i /db/00-create-database.sql
"${SQLCMD[@]}" -d "$DB_NAME" -i /db/01-schema.sql
"${SQLCMD[@]}" -d master -v DB_NAME="$DB_NAME" APP_USER="$APP_DB_USER" APP_PASSWORD="$APP_DB_PASSWORD" -i /db/02-app-login.sql
if [ "${SEED_DEMO_DATA:-false}" = "true" ]; then
  "${SQLCMD[@]}" -d "$DB_NAME" -i /db/03-demo-data.sql
fi
echo "database $DB_NAME is ready"
