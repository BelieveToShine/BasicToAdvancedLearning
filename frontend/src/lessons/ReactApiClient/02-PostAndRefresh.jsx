/*
 * LESSON 2 — POST, then make the screen match the server
 *
 * What this shows
 *   An "open account" form that sends POST /api/accounts with a JSON body, and two ways to bring the
 *   list up to date after the server says 201 Created.
 *
 * The three things a JSON POST needs
 *   fetch('/api/accounts', {
 *     method: 'POST',
 *     headers: { 'Content-Type': 'application/json' },   // 1. tell the server what the body is
 *     body: JSON.stringify({ customerId, accountNumber, accountType }),  // 2. the body is TEXT
 *   })
 *   3. The values must have the right JSON types: customerId is a number, so convert the text from the
 *      <input> with Number(...). (A number typed in an input is still a string in JavaScript.)
 *   Without the Content-Type header, fetch labels a string body 'text/plain;charset=UTF-8'. The bank API
 *   only accepts JSON bodies and answers 415 Unsupported Media Type (checked against the real API).
 *   The button "Send without the header" lets you see that.
 *
 * What a successful answer contains
 *   201 Created, a JSON body with the new account (balance 0: money arrives through transactions) and a
 *   Location header: the URL of the new resource. Read a header with response.headers.get('Location').
 *   In this dev setup the Location is an absolute URL pointing at the API's own address, not at the page's
 *   address. Lesson 5 explains why that matters.
 *
 * After the 201, how does the list learn about the new account?
 *   A) Update state with the account the server returned. No extra request; the screen changes at once.
 *      The risk: the list is now "what we had + one", and anything that changed on the server meanwhile
 *      (another user, a different sort order) is not in it.
 *   B) Fetch the list again (GET). One more request, but the list is exactly what the server holds now.
 *   Neither is "right"; pick on purpose. The page counts the GET requests so you can see the difference.
 *
 * Double submits: while the POST is in flight the button is disabled. Two quick clicks would otherwise send
 * two POSTs (the second would get 409 here because the account number must be unique).
 *
 * (The counter starts at 2, not 1, in the dev page: StrictMode runs the first effect twice in development, see Topic 23.)
 *
 * Only the happy path is handled in detail here. For a non-2xx answer the form just shows the title and
 * detail of the problem body; lesson 3 is about what each status should do in the UI.
 */
import { useEffect, useState } from 'react';

export const title = 'API client 2 — POST and refresh';

const NOT_REACHABLE = 'API not reachable — start it (see FullStackBank/README.md).';
// With the API stopped, the Vite proxy itself answers 502 (lesson 1); treat 502-504 like "no answer".
const isGatewayDown = (response) => [502, 503, 504].includes(response.status);

async function fetchAccounts() {
  const response = await fetch('/api/accounts');
  if (isGatewayDown(response)) throw new TypeError('gateway down');
  if (!response.ok) throw new Error(`The server answered ${response.status}`);
  return response.json();
}

// The form. It owns its inputs and the result of the last submit; the PARENT owns the list.
export function OpenAccountForm({ onCreated }) {
  const [customerId, setCustomerId] = useState('1');
  const [accountNumber, setAccountNumber] = useState('ACC-2001');
  const [accountType, setAccountType] = useState('Current');
  const [sending, setSending] = useState(false);
  const [result, setResult] = useState(null); // { status, text, location }

  // Sends the request. `withHeader` is false only for the demo button.
  async function send(withHeader) {
    setSending(true);
    setResult(null);
    try {
      const response = await fetch('/api/accounts', {
        method: 'POST',
        headers: withHeader ? { 'Content-Type': 'application/json' } : {},
        body: JSON.stringify({ customerId: Number(customerId), accountNumber, accountType }),
      });
      if (isGatewayDown(response)) {
        setResult({ status: response.status, text: NOT_REACHABLE, location: null });
      } else if (response.ok) {
        const account = await response.json();
        const location = response.headers.get('Location');
        setResult({ status: response.status, text: `Created account ${account.accountNumber} (id ${account.accountId}), balance ${account.balance.toFixed(2)}.`, location });
        onCreated(account);
      } else {
        const problem = await response.json().catch(() => ({}));
        setResult({ status: response.status, text: `${problem.title ?? 'Request failed'}${problem.detail ? ': ' + problem.detail : ''}`, location: null });
      }
    } catch {
      setResult({ status: null, text: NOT_REACHABLE, location: null });
    } finally {
      setSending(false);
    }
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault(); // stay on the page; we handle the request ourselves
        send(true);
      }}
    >
      <h3>Open an account</h3>
      <p>
        <label>
          Customer id <input type="number" value={customerId} onChange={(e) => setCustomerId(e.target.value)} style={{ width: 60 }} />
        </label>{' '}
        <label>
          Account number <input value={accountNumber} onChange={(e) => setAccountNumber(e.target.value)} />
        </label>{' '}
        <label>
          Type{' '}
          <select value={accountType} onChange={(e) => setAccountType(e.target.value)}>
            <option>Current</option>
            <option>Savings</option>
          </select>
        </label>
      </p>
      <button type="submit" disabled={sending}>
        {sending ? 'Sending…' : 'Open account'}
      </button>{' '}
      <button type="button" disabled={sending} onClick={() => send(false)}>
        Send without the Content-Type header
      </button>
      {result && (
        <div role="status">
          <p>
            Status: <strong>{result.status ?? 'no answer'}</strong> — {result.text}
          </p>
          {result.location && (
            <p>
              Location header: <code>{result.location}</code>
            </p>
          )}
        </div>
      )}
    </form>
  );
}

export function AccountsPage() {
  const [accounts, setAccounts] = useState(null); // null = not loaded yet
  const [error, setError] = useState(null);
  const [strategy, setStrategy] = useState('state'); // 'state' = A, 'refetch' = B
  const [gets, setGets] = useState(0); // how many GET /api/accounts we have sent
  const [refreshing, setRefreshing] = useState(false);

  async function load() {
    setGets((n) => n + 1);
    try {
      setAccounts(await fetchAccounts());
      setError(null);
    } catch (err) {
      setError(err instanceof TypeError ? NOT_REACHABLE : err.message);
    }
  }

  useEffect(() => {
    let ignore = false;
    setGets((n) => n + 1);
    fetchAccounts()
      .then((list) => {
        if (!ignore) setAccounts(list);
      })
      .catch((err) => {
        if (!ignore) setError(err instanceof TypeError ? NOT_REACHABLE : err.message);
      });
    return () => {
      ignore = true;
    };
  }, []);

  async function handleCreated(account) {
    if (strategy === 'state') {
      setAccounts((list) => [...(list ?? []), account]); // A: use the answer we already have
    } else {
      setRefreshing(true);
      await load(); // B: ask the server again
      setRefreshing(false);
    }
  }

  return (
    <div>
      <fieldset>
        <legend>After a 201, update the list by:</legend>
        <label>
          <input type="radio" name="strategy" checked={strategy === 'state'} onChange={() => setStrategy('state')} /> A) adding the returned account to state
        </label>{' '}
        <label>
          <input type="radio" name="strategy" checked={strategy === 'refetch'} onChange={() => setStrategy('refetch')} /> B) fetching the list again
        </label>
      </fieldset>

      <OpenAccountForm onCreated={handleCreated} />

      <h3>Accounts {refreshing && <small>(refreshing…)</small>}</h3>
      <p>
        GET /api/accounts requests sent so far: <strong data-testid="get-count">{gets}</strong>
      </p>
      {error && <p role="alert">{error}</p>}
      {!error && accounts === null && <p>Loading accounts…</p>}
      {accounts && (
        <ul>
          {accounts.map((a) => (
            <li key={a.accountId}>
              {a.accountNumber} — {a.customerName} — {a.accountType} — {a.balance.toFixed(2)}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default function Lesson() {
  return (
    <div>
      <h2>API client 2 — POST and refresh</h2>
      <p>
        Open an account, then watch the list and the GET counter. Each time you click "Open account" use a NEW account number: the API
        refuses a duplicate with 409. (Opened accounts stay in the demo database; that is real.)
      </p>
      <AccountsPage />
    </div>
  );
}
