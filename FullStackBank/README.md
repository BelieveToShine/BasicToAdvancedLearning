# FullStackBank - the "real" bank Web API

The API that the full-stack lessons (Phase 5/6, topics 25-27) call from the React front end. It is **its own
program**, separate from the console lessons: a deployed API is a process that keeps running by itself, so you start
it once and leave it running while you use the front end, `curl` or `smoke.sh`.

- ASP.NET Core (controllers), .NET 9, EF Core 9 with SQL Server, async/await throughout
- **No authentication, authorisation, HTTPS or CORS** (the Vite dev proxy makes the browser see one origin, so CORS is not needed). This is a local learning API only: never deploy it as it is.
- Listens on `http://localhost:5080` (the Vite dev-server proxy in `frontend/vite.config.js` points there)
- Same bank as the lessons: Customers, Accounts, Transactions

## API contract (stable - the React lessons are written against exactly this)

Base URL `http://localhost:5080`. JSON, camelCase. Dates are UTC ISO-8601 ending in `Z`. Money (`balance`, `amount`) is a decimal with at most 2 fractional digits, serialised as e.g. `5000.00`. A JSON number cannot carry "2 decimals" in JavaScript: `5000.00` parses to the number `5000` (format it with `toFixed(2)` for display).

**Account** (list items, `GET /api/accounts/{id}`, and the 201 body of `POST /api/accounts`) - exactly these 5 properties:

| property | type | notes |
|---|---|---|
| `accountId` | number (int) | |
| `customerName` | string | |
| `accountNumber` | string | unique, 1-20 chars: letters, digits, `-` |
| `accountType` | string | `"Savings"` or `"Current"` |
| `balance` | number | e.g. `5000.00` |

**Transaction** (list items, `GET .../transactions/{transactionId}`, and the 201 body of `POST .../transactions`) - exactly these 4 properties:

| property | type | notes |
|---|---|---|
| `transactionId` | number (int64) | |
| `kind` | string | `"Deposit"` or `"Withdrawal"` |
| `amount` | number | always positive; `kind` gives the direction |
| `createdAt` | string | UTC, e.g. `"2026-10-12T17:50:16.7149148Z"` |

Request bodies: `POST /api/accounts` takes `{ "customerId": 1, "accountNumber": "ACC-2001", "accountType": "Current" }`
(new accounts start at balance 0); `POST /api/accounts/{id}/transactions` takes `{ "kind": "Deposit", "amount": 100.50 }`.
After a transaction, read the new balance with `GET /api/accounts/{id}` (the transaction body does not repeat it).

Status codes: 200 reads; 201 + `Location` for both POSTs; 204 for DELETE; 400 invalid input; 404 unknown account/transaction/URL;
409 duplicate account number, delete of an account that has transactions, or a simultaneous change; 422 insufficient funds; 500 unexpected;
`GET /health` 200 `Healthy` / 503 `Unhealthy` (text/plain).

**Every error** is `application/problem+json` with `type`, `title`, `status`, `traceId` and (except plain framework 404/405) `detail`:

```json
// 404 / 409 / 422 / 500 (title varies: "Account not found", "Account has transactions", "Duplicate value", "Insufficient funds", "An unexpected error occurred")
{ "type": "https://tools.ietf.org/html/rfc4918#section-11.2", "title": "Insufficient funds", "status": 422,
  "detail": "Account 4 has a balance of 0.00; a withdrawal of 10.00 is not allowed.", "traceId": "00-..." }

// 400 validation: extra "errors" object, key = camelCase request field, value = array of messages
{ "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1", "title": "One or more validation errors occurred.", "status": 400,
  "errors": { "amount": ["amount must be between 0.01 and 1,000,000,000,000.00."] }, "traceId": "00-..." }
```
Possible `errors` keys: `customerId`, `accountNumber`, `accountType` (POST /api/accounts), `kind`, `amount` (POST transactions), `type` (`?type=`);
a value of the wrong JSON type or format gives a JSON-path key such as `$.amount` with the message `The value for 'amount' has the wrong type or format.`; text that is not JSON gives key `$` (`The request body is not valid JSON.`); an empty or missing body gives key `body`. The 500 body never contains exception text, SQL or a stack trace.

## Run it

1. Have a SQL Server you may create databases on, and set the connection string in an environment variable
   (never in a file that is committed):

   ```bash
   export BTAL_SQLSERVER='Server=localhost,1433;User Id=sa;Password=<yours>;TrustServerCertificate=True;Initial Catalog=BasicToAdvancedLearning'
   ```

   The API uses its **own database**, named after the catalog in the variable plus `_FullStackBank`
   (here: `BasicToAdvancedLearning_FullStackBank`). Without the variable the API prints one line and exits with code 1.

2. Start it:

   ```bash
   cd FullStackBank/Api
   dotnet run
   ```

   `dotnet run` uses `Properties/launchSettings.json`, which sets the **Development** environment. In Development
   (and only there) startup creates the database if missing, applies the EF migration `InitialCreate`, and seeds 3 customers
   with 3 accounts when the Customers table is empty (Asha Rao `ACC-1001` Savings 5000.00, Ben Carter `ACC-1002`
   Current 1200.50, Chen Li `ACC-1003` Savings 300.00, each with an opening deposit). Restarting does not duplicate the data.

3. Check it, then run the smoke test (second terminal):

   ```bash
   curl http://localhost:5080/health      # Healthy
   FullStackBank/smoke.sh                 # exits 0 when every endpoint behaves as documented
   ```

   `smoke.sh [base-url]` (or `BASE_URL=...`) needs only bash and curl. Each run leaves one extra account behind,
   because an account with transactions cannot be deleted (that rule is one of the checks).

Another port: `dotnet run --urls http://localhost:5090` (then set `API_URL=http://localhost:5090` for Vite).

## Endpoints

JSON in and out, property names in camelCase. `{id}` is an integer.

| Method and path | Success | Errors |
|---|---|---|
| `GET /health` | 200 `Healthy` (also checks the database) | 503 `Unhealthy` |
| `GET /api/accounts` | 200, list. Optional `?type=Savings` or `?type=Current` | 400 unknown `type` |
| `GET /api/accounts/{id}` | 200 | 404 |
| `POST /api/accounts` | 201 + `Location` header + the account | 400 invalid body or unknown `customerId`; 409 account number already exists |
| `DELETE /api/accounts/{id}` | 204, no body | 404; 409 the account has transactions |
| `GET /api/accounts/{id}/transactions` | 200, newest first | 404 |
| `GET /api/accounts/{id}/transactions/{transactionId}` | 200 | 404 |
| `POST /api/accounts/{id}/transactions` | 201 + `Location` + the transaction | 400 invalid body (unknown `kind`, `amount` below 0.01, above 1,000,000,000,000.00 or with more than 2 decimals; wrong JSON type; empty body); 415 body sent without `Content-Type: application/json`; 404 unknown account; 422 insufficient funds; 409 concurrent change |

Bodies:

```jsonc
// POST /api/accounts            (a new account always starts at balance 0; money arrives through transactions)
{ "customerId": 1, "accountNumber": "ACC-2001", "accountType": "Current" }   // accountType: "Savings" | "Current"

// POST /api/accounts/{id}/transactions   (amount is always positive; kind says which way the money moves)
{ "kind": "Deposit", "amount": 100.50 }                                     // kind: "Deposit" | "Withdrawal"
```

### Errors: always ProblemDetails

Every error answer (also an unknown URL, a wrong method, malformed JSON, a server fault) is `application/problem+json`:

```json
{ "type": "...", "title": "Insufficient funds", "status": 422, "detail": "Account 4 has a balance of 0.00; a withdrawal of 10.00 is not allowed.", "traceId": "00-..." }
```

Invalid requests (400) are produced by `[ApiController]` automatically, before the action runs, and carry an `errors`
object keyed by the **camelCase field name**, one array of messages per field:

```json
{ "title": "One or more validation errors occurred.", "status": 400,
  "errors": { "accountNumber": ["accountNumber must be 1-20 characters: letters, digits or '-'."],
              "accountType": ["accountType must be 'Savings' or 'Current'."] }, "traceId": "00-..." }
```

(If the JSON itself cannot be parsed, or a value has the wrong JSON type, the key is a JSON path such as `$` or `$.accountType`.)

## Design choices worth reading in the code

- **DTOs, never entities** (`Dtos/`): the JSON contract does not change when a table does.
- **Validation** with data annotations on the DTOs; `[ApiController]` returns the 400 for you. Only rules that need the
  database (customer exists, funds available) are written in the controller.
- **422 vs 409.** 422 "Insufficient funds": the request is valid and the account exists, but a *business rule* (no overdraft)
  refuses it. 409 is used here for conflicts about the data's identity or a simultaneous change: duplicate account number,
  deleting an account that has history, two requests changing the same account at once. Other APIs return 409 for
  insufficient funds too; both are defensible, the point is to choose one rule and use it consistently.
- **One SaveChanges per money movement.** The balance `UPDATE` and the transaction `INSERT` happen in one call, which EF Core
  runs inside one database transaction: both happen or neither does.
- **Concurrent requests.** `Account.RowVersion` (SQL Server `rowversion`) makes two simultaneous changes to one account
  detectable: the loser gets 409 and nothing is saved, so the balance can never go wrong. Example run: 60 parallel
  withdrawals of 30 at an account holding 1000 gave 24 successes and 36 conflicts. The exact split varies from run to run, but the balance
  always equals 1000 - 30 x successes (and never goes negative). A client should re-read the account and retry on 409.
- **Global exception handler** (`Infrastructure/ApiExceptionHandler.cs`), active in every environment: database errors are mapped by SQL Server error
  *number* (2601 and 2627 duplicate value, 547 foreign key / CHECK violation) to 409 (the CHECK-constraint side of 547 is only a last-line safety net, because the code validates first; in practice only the foreign-key case can be reached); anything else becomes a generic 500 ProblemDetails
  with no exception text, no stack trace, no SQL. The details go to the server log, the client gets a `traceId` to quote.
- **Database safety net.** UNIQUE indexes (account number, email), CHECK constraints (account type, balance >= 0, amount > 0,
  kind), foreign keys with no cascade delete.
- **Dates are UTC** and serialized with a trailing `Z`.

## Migrations: why Development only

Startup calls `Database.MigrateAsync()` **only in Development**. In production you do not do that: the app's database
login should not be allowed to change the schema; several copies starting at once would race to migrate the same database;
and a failed or slow migration would stop the API from starting, with nobody having reviewed the SQL. Production deployments
generate a script, review it, and run it as a deployment step. In any non-Development environment the database must therefore
already exist and be migrated.

```bash
# from FullStackBank/Api, BTAL_SQLSERVER set; needs the dotnet-ef tool (dotnet tool install -g dotnet-ef)
dotnet ef migrations add <Name> -o Migrations            # after changing entities
dotnet ef migrations script --idempotent -o bank.sql     # what you would run in production
```

Running notes:

- `dotnet run` uses `launchSettings.json`, which forces `ASPNETCORE_ENVIRONMENT=Development`, so setting that variable yourself has no effect.
  To try another environment use `dotnet run --no-launch-profile` (then the default is Production: no automatic migration, the database must exist).
- If SQL Server cannot be reached (or the variable is not a valid connection string) at startup, the API prints a one-line message and exits with code 1.
- Expected 409 conflicts are logged as one warning line. EF Core's own "Microsoft.EntityFrameworkCore.Update" log category is silenced in
  `appsettings.json` (otherwise every duplicate account number would print an error with a stack trace first); genuine failures (a 500) are still
  logged in full by the exception handler.

## Possible extensions (not built here)

- **JWT authentication** (topic 20 teaches it): protect the endpoints, tie accounts to the logged-in customer.
- Paging for the transaction list, a transfer between two accounts (two transactions in one SaveChanges), endpoints for customers.
- Production hardening (auth, HTTPS, CORS, a real migrations pipeline). A learning-level Docker setup is in [`deploy/README.md`](deploy/README.md) (topic 28).
