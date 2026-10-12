/*
 * LESSON 1 — GET from the API
 *
 * What this shows
 *   Reading the list of bank accounts: fetch('/api/accounts') -> check the answer -> read the JSON.
 *   The loading / error / data state and the "ignore" flag in the effect are exactly the pattern from
 *   Topic 23 (ReactEffects, lesson 4): one state object, a fetch inside useEffect, and a flag that
 *   throws away an out-of-date answer. This lesson looks at what is NEW here: what the real answer contains.
 *
 * fetch() is a two-step thing
 *   1. `await fetch(url)` finishes as soon as the status line and headers have arrived. You get a
 *      Response object: response.status (200), response.ok (true for 200-299), response.headers.
 *   2. `await response.json()` is a SECOND promise: it reads the body and parses it. Do it after the check.
 *   The page below prints the status, ok and Content-Type of the real answer so you can see step 1.
 *
 * Two different ways to fail (the page shows them differently)
 *   - fetch REJECTS: the browser got no usable answer at all (cannot connect, blocked, ...).
 *   - fetch RESOLVES with ok === false: the server DID answer, with a status such as 404 or 500.
 *   fetch does not throw for a 404 or 500; checking response.ok is your job (lesson 3 goes deeper).
 *
 * "API not reachable"
 *   The URL '/api/accounts' is relative, so it goes to the page's own server: the Vite dev server,
 *   which forwards it to the API (lesson 5). If the API is not running, the Vite server cannot forward
 *   the request and answers 502 Bad Gateway with a plain-text body (checked in a real run). So "API down"
 *   can show up as a rejected fetch OR as a 502 that is not a problem+json body, and both mean
 *   "start the API (see FullStackBank/README.md)".
 *
 * Response shape (GET /api/accounts, from FullStackBank/README.md):
 *   [ { accountId, customerName, accountNumber, accountType, balance }, ... ]
 *   accountId is unique, so it is the `key` for each table row.
 */
import { useEffect, useState } from 'react';

export const title = 'API client 1 — GET from the API';

// A failed answer that is NOT a problem body from the API is the dev proxy talking (see above).
function looksUnreachable(response) {
  const type = response.headers.get('Content-Type') ?? '';
  return [502, 503, 504].includes(response.status) && !type.includes('json');
}

export function AccountsList() {
  const [state, setState] = useState({ status: 'loading', data: null, error: null, unreachable: false, response: null });
  const [reloadCount, setReloadCount] = useState(0);

  useEffect(() => {
    let ignore = false; // belongs to THIS run of the effect (Topic 23)

    async function load() {
      setState({ status: 'loading', data: null, error: null, unreachable: false, response: null });
      let response;
      try {
        response = await fetch('/api/accounts'); // step 1: status + headers
      } catch {
        if (!ignore) setState({ status: 'error', data: null, error: 'The browser got no answer.', unreachable: true, response: null });
        return;
      }
      // What the first step gave us, kept for display.
      const seen = { status: response.status, ok: response.ok, contentType: response.headers.get('Content-Type') };
      if (!response.ok) {
        if (!ignore) setState({ status: 'error', data: null, error: `The server answered ${response.status}.`, unreachable: looksUnreachable(response), response: seen });
        return;
      }
      try {
        const accounts = await response.json(); // step 2: read + parse the body
        if (!ignore) setState({ status: 'success', data: accounts, error: null, unreachable: false, response: seen });
      } catch {
        if (!ignore) setState({ status: 'error', data: null, error: 'The answer was not valid JSON.', unreachable: false, response: seen });
      }
    }

    load(); // the effect itself is not async; it only starts the async function
    return () => {
      ignore = true;
    };
  }, [reloadCount]);

  return (
    <section>
      <h3>Accounts from GET /api/accounts</h3>
      <button onClick={() => setReloadCount((n) => n + 1)}>Reload</button>

      {state.status === 'loading' && <p>Loading accounts…</p>}

      {state.status === 'error' && state.unreachable && (
        <p role="alert">API not reachable — start it (see FullStackBank/README.md), then press Reload.</p>
      )}
      {state.status === 'error' && !state.unreachable && <p role="alert">Could not load accounts: {state.error}</p>}

      {state.response && (
        <p>
          Step 1 gave: status <strong>{state.response.status}</strong>, ok <strong>{String(state.response.ok)}</strong>, Content-Type{' '}
          <code>{state.response.contentType}</code>
        </p>
      )}

      {state.status === 'success' && (
        <table cellPadding={6}>
          <thead>
            <tr>
              <th>Id</th>
              <th>Customer</th>
              <th>Number</th>
              <th>Type</th>
              <th style={{ textAlign: 'right' }}>Balance</th>
            </tr>
          </thead>
          <tbody>
            {state.data.map((a) => (
              <tr key={a.accountId}>
                <td>{a.accountId}</td>
                <td>{a.customerName}</td>
                <td>{a.accountNumber}</td>
                <td>{a.accountType}</td>
                <td style={{ textAlign: 'right' }}>{a.balance.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

export default function Lesson() {
  return (
    <div>
      <h2>API client 1 — GET from the API</h2>
      <p>
        <code>fetch('/api/accounts')</code> resolves when the headers arrive; <code>response.json()</code> then reads the body.
        Stop the API and press Reload to see the "not reachable" state.
      </p>
      <AccountsList />
    </div>
  );
}
