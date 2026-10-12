# Deploying the bank with Docker (FullStackBank/deploy)

Until now you started the bank by hand: `dotnet run` for the API, `npm run dev` for the React lessons, and a SQL Server you
installed yourself. **Deploying** means putting the finished program on a machine where people can use it and keeping it running.
This folder does that on **one** machine with **Docker** and **docker compose**, in the way real deployments are done, just small.

You need: Docker with the `docker compose` command (Docker Desktop includes both) and about 5 GB of free disk (the base images are large).
You do **not** need .NET, Node or SQL Server installed: they live inside the containers.

Words used below
- **Image**: a read-only package holding a program and everything it needs (made by a *build*). Like a recipe's finished, frozen dish.
- **Container**: a running copy of an image. You can start many containers from one image.
- **Volume**: a folder managed by Docker that outlives its container. The database files live in one.
- **docker compose**: starts several containers together from one file (`docker-compose.yml`) and puts them on a private network
  where each container finds the others by its service name (`db`, `api`, `web`).

## What is in this folder

| File | What it is for |
|---|---|
| `Dockerfile.api` | recipe for the API image (build with the .NET SDK, run on the small ASP.NET runtime image, as a non-root user) |
| `Dockerfile.web` | recipe for the web image (build the React app with Node, serve the files with nginx) |
| `nginx.conf` | nginx settings: serve the React files, forward `/api/` to the API |
| `docker-compose.yml` | the four services, their order, ports, volume and health checks |
| `.gitattributes` | keeps `db/init.sh` on LF line endings (it runs in Linux; CRLF from a Windows checkout would break it) |
| `.env.example` | the settings (with placeholders) you copy to `.env`. **`.env` is git-ignored; never commit it.** |
| `db/` | SQL scripts + `init.sh` that set up the database (the "deployment step"); `01-schema.sql` was generated from the EF migration |
| `Dockerfile.*.dockerignore` | what must NOT be sent to a build |

```
 your browser --> localhost:8080 --> [ web: nginx ] --/api/--> [ api: ASP.NET Core, port 8080 ] --> [ db: SQL Server, port 1433 ]
                                       |  serves React files                                            |
                                       |                                                                volume "sqldata"
                  only this port is published to your computer; api and db are reachable inside the compose network only
```

Diagrams for each step below: `ArchitectureDiagrams/Deployment/`.

## Step 1 - Configuration and secrets (`.env`)

```bash
cd FullStackBank/deploy
cp .env.example .env        # then open .env and replace every CHANGE_ME
```

The rule behind this ("12-factor config"): **code and images are the same everywhere; settings that differ per machine come
from the environment when the program starts.** So:

- passwords are **not** in the Dockerfile, not in `docker-compose.yml`, not in the image: only in `.env`;
- `.env` is listed in the repository's `.gitignore`; `.env.example` has placeholders only and is committed so people know what to set;
- compose reads `.env` automatically and fills `${NAME}` in `docker-compose.yml`. A missing required value stops compose with a
  message such as `required variable MSSQL_SA_PASSWORD is missing a value` instead of starting with an empty password.

**The placeholders are valid passwords, so the stack starts even if you change nothing. Do not do that for anything but a quick try-out:** put your own random passwords in `.env`, for example `openssl rand -base64 24` (SQL Server wants 8+ characters with 3 of 4 kinds: upper case, lower case, digits, symbols; some characters from `-base64` such as `+ / =` are fine, but remove any `;`, quotes, `$`, `#` or spaces, which this setup passes through shell commands and a connection string).

Settings: `MSSQL_SA_PASSWORD` (the SQL Server admin), `APP_DB_USER` / `APP_DB_PASSWORD` (the login the API uses),
`DB_BASE_NAME`, `SEED_DEMO_DATA` and `WEB_PORT` (the port on your computer, default 8080). The port is published on `127.0.0.1` only, so only your own computer can open the site; to let other machines in, remove `127.0.0.1:` from the `ports:` line in `docker-compose.yml` (and think about HTTPS first).
Two details: the API appends `_FullStackBank` to the database name it is given (so `Bank` becomes `Bank_FullStackBank`; the
compose file builds the same name for the setup script), and SQL Server rejects weak admin passwords.

## Step 2 - Build the API image (`Dockerfile.api`)

```bash
docker compose build api
```

A **multi-stage** Dockerfile has several `FROM` lines:

1. `build` stage: starts from `mcr.microsoft.com/dotnet/sdk:9.0` (compiler, NuGet, about 1.25 GB on disk), restores packages, runs
   `dotnet publish -c Release` into `/out`.
2. `runtime` stage: starts again from the much smaller `mcr.microsoft.com/dotnet/aspnet:9.0` and copies only `/out`.
   The SDK stays behind. (We measured: the finished API image is about 350 MB, the SDK image about 1.25 GB.)

Things to notice in the file:
- the `.csproj` is copied and restored **before** the rest of the source. Docker caches every step ("layer"); when only your C# changes,
  the slow package download is reused. (Rebuilding with nothing changed took under 2 seconds and reported every step as `CACHED`.)
- `ENV ASPNETCORE_URLS=http://+:8080`: inside a container the API must listen on all addresses. Without it, `Program.cs` falls back to
  `http://localhost:5080`, which only the container itself can reach (we checked: the log says `Now listening on: http://localhost:5080`).
  Port 8080 rather than 80 because a non-root user may not use ports below 1024 by default.
- `USER $APP_UID`: the process runs as the user `app` (id 1654), not root (`docker exec <api container> id` prints `uid=1654(app)`).
- **no secrets**: the image's environment contains only `ASPNETCORE_*` and .NET settings; the connection string arrives at run time.

## Step 3 - Build the web image (`Dockerfile.web`, `nginx.conf`)

```bash
docker compose build web
```

Stage 1 uses `node:22-alpine`: `npm ci` (installs exactly what `package-lock.json` says) then `npm run build`, which produces plain
HTML/JS/CSS files in `dist/`. Stage 2 uses `nginx:alpine` and copies only those files plus `nginx.conf`. Node is not in the final image
(about 94 MB). The build context is the repository root, and `Dockerfile.web.dockerignore` lets only `frontend/` and `nginx.conf` in.

`nginx.conf` does two jobs:
- `location /api/ { proxy_pass http://api:8080; ... }` (with `Host $http_host`, which keeps the port you typed; `$host` would drop it and links such as the `Location` header of a created account would lack the port) forwards API calls to the container named `api`. The browser sees **one** address,
  so no CORS setup is needed and the API port is never opened to the outside. The React code calls `fetch('/api/accounts')`: a relative URL.
  It contains no server address, which is why the same build works on any machine.
- `location / { try_files $uri /index.html; }` serves the built files. **Gotcha:** every other URL, including `/health`, gets
  `index.html` with status 200, so `http://localhost:8080/health` is **not** the API's health check (it is not forwarded).

## Step 4 - Start the stack (`docker-compose.yml`)

```bash
docker compose up --build        # add -d to run in the background
```

Compose starts the services in an order enforced by `depends_on` **conditions** (plain `depends_on` only waits for a container to *start*,
which is much too early for a database):

| Service | Image | What it does | Starts when |
|---|---|---|---|
| `db` | `mcr.microsoft.com/mssql/server:2022-latest` | SQL Server; files in volume `sqldata`; **no published port** | first. Health check: `sqlcmd ... SELECT 1` succeeds |
| `db-init` | same image (it contains `sqlcmd`) | runs `db/init.sh` once: create database, create tables, create the API's login, optionally insert the demo data; then exits | `db` is **healthy** |
| `api` | built from `Dockerfile.api` | the API on port 8080, **no published port** | `db-init` **completed successfully** (exit code 0) |
| `web` | built from `Dockerfile.web` | nginx on port 80, published as `localhost:${WEB_PORT}` | `api` is **healthy** |

Health checks are commands Docker runs every few seconds inside the container; the container is `healthy` when the command succeeds.
- `db`: `sqlcmd -Q "SELECT 1"`. Docker needs about 15-20 s on a first start.
- `api`: the `aspnet` image has neither `curl` nor `wget`, so the check uses bash's built-in TCP access to send `GET /health` to
  `127.0.0.1:8080` and looks for status 200. The API's `/health` also opens a connection to the database, so "healthy" means "can serve".
- `web`: `wget http://127.0.0.1/` (busybox wget ships in the nginx image). It says `127.0.0.1`, not `localhost`, because `localhost`
  can resolve to the IPv6 address `::1` and this nginx listens on IPv4 only.

### Why the database set-up is a separate step (and the API does not migrate in production)

`Program.cs` creates the database and applies the EF Core migration at startup **only when the environment is `Development`**.
The images run as `Production` (the default for a published app, and set explicitly in `Dockerfile.api`). We ran the same image both ways
against a database that did not exist yet:

| `ASPNETCORE_ENVIRONMENT` | Login | Result |
|---|---|---|
| `Production` | `sa` | the API starts, but `/health` answers 503 and `/api/accounts` answers 500: it did **not** create the database |
| `Development` | `sa` | the API created the database, migrated it and added the demo data; `/api/accounts` answered with the 3 accounts |
| `Development` | `bank_app` (this stack's login) | the API crashed at startup: `CREATE DATABASE permission denied` |

Why production works differently: the API's own database login should only be able to read and write rows (we verified that `bank_app` gets
`CREATE TABLE permission denied`); several copies of the API starting together would race to migrate the same database; and a
failed migration would keep the API from starting with nobody having reviewed the SQL. So the schema is changed by a deployment step:
`db/01-schema.sql` is the output of `dotnet ef migrations script --idempotent` (every part is guarded by "was this migration already applied?",
so running it twice does no harm), and `db-init` runs it with the admin login. `bank_app` can still INSERT, UPDATE and DELETE rows (the API needs that): its guarantee is *no schema changes*, not *no data loss*. The login keeps SQL Server's password policy on, so a weak `APP_DB_PASSWORD` is rejected, and if the login already exists `db-init` sets it to the current `.env` value again, so changing `APP_DB_PASSWORD` and running `docker compose up` works. After you add a migration, regenerate it:

```bash
cd FullStackBank/Api
BTAL_SQLSERVER='Server=x;User Id=x;Password=x;Initial Catalog=x' dotnet ef migrations script --idempotent -o ../deploy/db/01-schema.sql
```
(The variable only has to exist; `dotnet ef` does not connect when generating a script. Remove the byte-order mark from the first line if your
tool adds one.) The demo data (`db/03-demo-data.sql`) is only for learning: set `SEED_DEMO_DATA=false` for anything real.

## Step 5 - Check it end to end, then tear it down

```bash
docker compose ps                         # db, api, web should say "healthy"; db-init "Exited (0)" is correct
curl -i http://localhost:8080/            # 200, the React page (Server: nginx)
curl http://localhost:8080/api/accounts   # JSON list of the 3 demo accounts, via nginx -> api -> db
```
Open `http://localhost:8080/` in a browser and use the "Deployment" lesson's check button. Other useful commands:

```bash
docker compose logs -f api                # follow one service's log
docker compose down                       # stop and remove the containers; the volume (your data) stays
docker compose up -d                      # same data again (the db-init scripts are safe to repeat; nothing is duplicated)
docker compose down -v                    # also deletes the volume: the database is gone
```
What we saw: a deposit of 100.50 into account 1 (balance 5000.00 -> 5100.50) was still there after `down` + `up`, and the demo
rows were not duplicated; `down -v` removed the data.

If the API is stopped while nginx runs, `/api/...` answers **502 Bad Gateway**: nginx is fine, the thing behind it is not.

## Development vs production: what changes in this project

| | Development | This container stack |
|---|---|---|
| database creation + migration | by the API at startup | by the `db-init` step |
| database login | whatever you put in `BTAL_SQLSERVER` | `bank_app`: read/write rows only |
| API address | `http://localhost:5080` | internal `http://api:8080`, not published |
| React | Vite dev server, `/api` proxied by Vite | built files, `/api` proxied by nginx |
| error pages | the API returns the same generic 500 ProblemDetails in every environment (it does not use the developer exception page) | same |

### About HTTPS

This stack serves **plain HTTP**: `ASPNETCORE_URLS` has only an `http://` address, and the API does not call `UseHttpsRedirection`
or `UseHsts` (we searched the code). Anything typed into a login form would travel readable over the network, so a real deployment
puts **TLS (HTTPS)** in front: usually at nginx or a load balancer ("TLS termination"), with a certificate for your domain,
and the API stays on HTTP inside the private network. We did **not** build or test that here. Related: `nginx.conf` sends `X-Forwarded-For`
and `X-Forwarded-Proto` to the API, but the API does not read them yet (that needs the ForwardedHeaders middleware), so it sees nginx as
its client. The SQL connection uses `TrustServerCertificate=True`, which accepts SQL Server's self-signed certificate: acceptable on a private
compose network for learning, not something to copy to a database across the internet.

A harmless line in the API log, `Overriding HTTP_PORTS '8080' and HTTPS_PORTS ''. Binding to values defined by URLS instead 'http://+:8080'.`, appears because the base image sets `ASPNETCORE_HTTP_PORTS` and our `ASPNETCORE_URLS` takes precedence.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `required variable ... is missing a value` | you did not create `.env` (Step 1) |
| `db` never becomes healthy | `MSSQL_SA_PASSWORD` is too weak for SQL Server (8+ characters, 3 of 4 character kinds; read `docker compose logs db`), or you changed it after the first start (see the last row) |
| `service "db-init" didn't complete successfully` | the setup scripts failed: `docker compose logs db-init` (we triggered this on purpose with an invalid database name; `api` and `web` were then not started) |
| `Bind for 0.0.0.0:8080 failed: port is already allocated` | something on your computer uses `WEB_PORT`; change it in `.env` |
| page loads but "API not reachable" / 502 | `docker compose ps`: is `api` healthy? `docker compose logs api` |
| changed C# or React code but nothing changed | images are not rebuilt automatically: `docker compose up --build` |
| you changed `MSSQL_SA_PASSWORD` in `.env` after the first start | SQL Server keeps the old admin password in the volume (`APP_DB_PASSWORD` is re-applied by `db-init`, the admin one is not). We tried it: with a new `MSSQL_SA_PASSWORD` the `db` container never became healthy and `api`/`web` could not start. Put the old value back, or `docker compose down -v` and start again (this deletes the data) |

## Not covered (on purpose)

HTTPS certificates, security headers (for example Content-Security-Policy) and other nginx hardening beyond `server_tokens off`, a container registry and CI/CD, running several copies of the API behind a load balancer, backups of the volume,
secrets managers (`.env` is the simple end of the scale; cloud platforms have dedicated secret stores), logging and monitoring.
