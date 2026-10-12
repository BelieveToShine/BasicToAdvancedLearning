/*
 * LESSON 4 — One API module (components stop knowing about fetch)
 *
 * What this shows
 *   Lessons 1-3 put fetch, headers, JSON and status checks inside the components. That works, but every
 *   component repeats it, and every copy can be slightly wrong. The fix is old and simple: move the HTTP
 *   details into ONE module, `api.js` (next to this file), and let components call plain functions:
 *
 *      const accounts = await api.listAccounts();
 *      await api.createAccount({ customerId: 1, accountNumber: 'ACC-2001', accountType: 'Current' });
 *
 *   The module does four jobs (the page shows its real source below):
 *     1. one BASE path ('/api'), so a URL is written once;
 *     2. the headers (Accept always; Content-Type: application/json only when there is a body);
 *     3. JSON in and out, including "no body" (204) which becomes null;
 *     4. ERROR NORMALISATION: whatever goes wrong becomes an ApiError with the same few properties
 *        (kind, status, title, detail, fieldErrors, traceId). A network failure, a 400, a 404, a 409,
 *        a 422 and a 5xx all arrive in the component's catch block looking alike.
 *
 * Why this is worth it
 *   - A component only decides what to SHOW for each `kind`; it does not know what a Response is.
 *   - A change (a new header, a different base path) is made in one place.
 *   - Tests of components can replace the api module, or fetch, without touching component code. This
 *     lesson's tests replace fetch once and the same components work.
 *
 * Why a class for the error?
 *   `class ApiError extends Error` lets the catch block use `err instanceof ApiError` to tell "the API said no"
 *   from "my code has a bug" (a TypeError from a typo, for instance, is NOT an ApiError and should not be
 *   presented as an API message). This is a plain JavaScript class used as data; React components
 *   in this course stay function components.
 */
import { useEffect, useState } from 'react';
import { api, ApiError } from './api.js';
import apiSource from './api.js?raw'; // Vite's ?raw import: the file's text, so the page can show it

export const title = 'API client 4 — One API module';

// What to show for an ApiError. This is ALL a component needs to know about failures.
export function ErrorView({ error }) {
  // Not an ApiError = not an answer from the API but a bug in OUR code: label it as that, do not dress it up as an API message.
  if (!(error instanceof ApiError)) return <p role="alert">Unexpected error in the front end: {String(error.message ?? error)}</p>;
  if (error.kind === 'network') return <p role="alert">API not reachable — start it (see FullStackBank/README.md), then try again.</p>;
  if (error.kind === 'validation') {
    return (
      <div role="alert">
        <p>Please fix:</p>
        <ul>
          {Object.entries(error.fieldErrors).map(([field, messages]) => (
            <li key={field}>
              {field}: {messages.join(' ')}
            </li>
          ))}
        </ul>
      </div>
    );
  }
  return (
    <p role="alert">
      {error.title}
      {error.detail ? ` — ${error.detail}` : ''} <small>(status {error.status})</small>
    </p>
  );
}

// NOTE: no `fetch`, no `Response`, no header and no URL in the two components below.
export function AccountsPanel() {
  const [accounts, setAccounts] = useState(null);
  const [error, setError] = useState(null);
  const [version, setVersion] = useState(0); // bump to reload

  useEffect(() => {
    let ignore = false;
    api
      .listAccounts()
      .then((list) => {
        if (!ignore) {
          setAccounts(list);
          setError(null);
        }
      })
      .catch((err) => {
        if (!ignore) setError(err);
      });
    return () => {
      ignore = true;
    };
  }, [version]);

  async function remove(account) {
    try {
      await api.deleteAccount(account.accountId); // resolves with nothing: 204 has no body
      setVersion((v) => v + 1);
    } catch (err) {
      setError(err); // for example 409: the account has transactions
    }
  }

  return (
    <section>
      <h3>Accounts</h3>
      <button onClick={() => setVersion((v) => v + 1)}>Reload</button>
      {error && <ErrorView error={error} />}
      {accounts === null && !error && <p>Loading accounts…</p>}
      {accounts && (
        <table cellPadding={5}>
          <tbody>
            {accounts.map((a) => (
              <tr key={a.accountId}>
                <td>{a.accountNumber}</td>
                <td>{a.customerName}</td>
                <td style={{ textAlign: 'right' }}>{a.balance.toFixed(2)}</td>
                <td>
                  <button onClick={() => remove(a)} aria-label={`Delete ${a.accountNumber}`}>
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

export function OpenAccountPanel({ onCreated }) {
  const [form, setForm] = useState({ customerId: '1', accountNumber: '', accountType: 'Current' });
  const [error, setError] = useState(null);
  const [created, setCreated] = useState(null);

  async function submit(e) {
    e.preventDefault();
    setError(null);
    setCreated(null);
    try {
      const { account } = await api.createAccount({ ...form, customerId: Number(form.customerId) });
      setCreated(account);
      onCreated?.(account);
    } catch (err) {
      setError(err);
    }
  }

  return (
    <form onSubmit={submit}>
      <h3>Open an account</h3>
      <label>
        Account number <input value={form.accountNumber} onChange={(e) => setForm({ ...form, accountNumber: e.target.value })} />
      </label>{' '}
      <label>
        Type{' '}
        <select value={form.accountType} onChange={(e) => setForm({ ...form, accountType: e.target.value })}>
          <option>Current</option>
          <option>Savings</option>
        </select>
      </label>{' '}
      <button type="submit">Open account</button>
      {error && <ErrorView error={error} />}
      {created && <p role="status">Created {created.accountNumber} (id {created.accountId}).</p>}
    </form>
  );
}

export default function Lesson() {
  const [reloadKey, setReloadKey] = useState(0);
  return (
    <div>
      <h2>API client 4 — One API module</h2>
      <p>
        Both panels below call <code>api.*</code> only. Try: an empty account number (400), a duplicate (409), Delete on ACC-1001 (409: it has
        transactions), or stop the API (network).
      </p>
      <OpenAccountPanel onCreated={() => setReloadKey((k) => k + 1)} />
      <AccountsPanel key={reloadKey} />
      <details>
        <summary>The real source of api.js</summary>
        <pre style={{ fontSize: 12, overflow: 'auto', maxHeight: 400, background: '#f6f6f6', padding: 8 }}>{apiSource}</pre>
      </details>
    </div>
  );
}
