// The ONE place where this mini project talks to the Web API. (Not a lesson: the file name does not
// start with a digit, so the lesson picker ignores it.)
//
// Why one file? Screens should not know URLs or JSON details. If the API changes, you change api.js.
//
// Every function here:
//   - calls fetch() with a RELATIVE url ("/api/accounts"). In development the Vite dev server forwards
//     "/api/..." to the real Web API (the "proxy"), so the browser sees one origin and needs no CORS setup.
//   - returns the parsed JSON on success,
//   - throws an ApiError on failure, so a screen has ONE way to handle "it did not work":
//       ApiError.unreachable === true  -> no answer from the API at all (it is not running)
//       ApiError.status / .problem     -> the API answered with an error (400, 404, 409, 422, 500 ...)
//     where .problem is the API's application/problem+json body (title, detail, errors ...).
//
// It also keeps a short log of the calls made (see the "journey" panel in parts.jsx).
// The log is a tiny store that React components read with useSyncExternalStore.

export class ApiError extends Error {
  constructor({ unreachable = false, status = 0, problem = null }) {
    super(unreachable ? 'API not reachable' : problem?.title ?? `HTTP ${status}`);
    this.unreachable = unreachable;
    this.status = status;
    this.problem = problem;
  }
}

// ---- the call log -------------------------------------------------------------------------------
let calls = []; // newest last; at most 6 kept
const listeners = new Set();
function publish(next) {
  calls = next.slice(-6); // a NEW array each time: useSyncExternalStore compares snapshots by identity
  listeners.forEach((l) => l());
}
export const callLog = {
  subscribe(listener) {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
  getSnapshot: () => calls,
  clear: () => publish([]),
};

let nextCallId = 1;

// What the database does for each endpoint WHEN IT SUCCEEDS (taken from the API's controller code).
// sqlThatRan() below picks the honest text for the status that really came back.
const SQL = {
  list: 'SELECT ... FROM Accounts (+ customer name), optional WHERE AccountType = @type',
  one: 'SELECT ... FROM Accounts WHERE AccountId = @id',
  transactions: 'SELECT ... FROM Transactions WHERE AccountId = @id ORDER BY TransactionId DESC',
  deposit: 'UPDATE Accounts (Balance) + INSERT INTO Transactions - one database transaction',
  open: 'INSERT INTO Accounts (the UNIQUE index on AccountNumber is the duplicate guard)',
};

// The SQL that actually ran depends on how the request ended. Checked against the API's controller code.
function sqlThatRan(sqlKey, status, problem) {
  if (status === 'no answer' || problem === 'unreachable') return 'the request never reached the API, so no SQL ran';
  if (typeof status === 'number' && status >= 200 && status < 300) return SQL[sqlKey];
  if (status === 400) {
    // POST /api/accounts with an unknown customerId: the action ran and looked the customer up first.
    if (sqlKey === 'open' && problem?.errors?.customerId) return 'SELECT ... FROM Customers WHERE CustomerId = @id (no such customer); nothing inserted';
    return 'no SQL ran: validation failed before the controller action';
  }
  if (status === 404) return 'SELECT ... FROM Accounts found no row; nothing else ran';
  if (status === 409 && sqlKey === 'open') return 'INSERT was attempted and refused by the UNIQUE index; nothing stored';
  if (status === 422) return 'SELECT ... FROM Accounts WHERE AccountId = @id only (the rule refused it; nothing written)';
  return 'the API answered with an error; its log has the details';
}

async function call(sqlKey, method, url, body) {
  const entry = { id: nextCallId++, method, url, body, status: null, sql: 'waiting for the answer...' };
  publish([...calls, entry]);
  const finish = (status, problem) =>
    publish(calls.map((c) => (c.id === entry.id ? { ...c, status, sql: sqlThatRan(sqlKey, status, problem) } : c)));

  let res;
  try {
    res = await fetch(url, {
      method,
      headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    // fetch only REJECTS when no HTTP answer arrived at all (server down, network cut).
    // An HTTP error such as 404 or 422 is NOT a rejection: fetch resolves and we look at res.ok.
    finish('no answer');
    throw new ApiError({ unreachable: true });
  }
  const type = res.headers.get('content-type') ?? '';
  const isJson = type.includes('json');
  if (res.ok) {
    finish(res.status);
    return isJson ? res.json() : null;
  }

  // Not ok. The real API ALWAYS answers errors as JSON (problem+json). If something else answered
  // (for example the Vite proxy's own error page when the API is down), treat it as "not reachable".
  if (!isJson) {
    finish(res.status, 'unreachable');
    throw new ApiError({ unreachable: true, status: res.status });
  }
  const problem = await res.json();
  finish(res.status, problem);
  throw new ApiError({ status: res.status, problem });
}

// ---- the endpoints this app uses ------------------------------------------------------------------
export const api = {
  // GET /api/accounts  or  GET /api/accounts?type=Savings
  listAccounts(type = '') {
    const query = type ? `?type=${encodeURIComponent(type)}` : '';
    return call('list', 'GET', `/api/accounts${query}`);
  },
  // GET /api/accounts/{id}
  getAccount: (id) => call('one', 'GET', `/api/accounts/${id}`),
  // GET /api/accounts/{id}/transactions   (newest first - the API sorts, we do not)
  listTransactions: (id) => call('transactions', 'GET', `/api/accounts/${id}/transactions`),
  // POST /api/accounts/{id}/transactions   body { kind: "Deposit" | "Withdrawal", amount }
  postTransaction: (id, kind, amount) => call('deposit', 'POST', `/api/accounts/${id}/transactions`, { kind, amount }),
  // POST /api/accounts   body { customerId, accountNumber, accountType }   -> 201 + the new account
  openAccount: (customerId, accountNumber, accountType) =>
    call('open', 'POST', '/api/accounts', { customerId, accountNumber, accountType }),
};

// Short text for money and dates (the API sends numbers and UTC ISO strings).
export const money = (n) => Number(n).toFixed(2);
export const utc = (iso) => `${iso.replace('T', ' ').slice(0, 19)} UTC`;
