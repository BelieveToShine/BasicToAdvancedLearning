/*
 * LESSON 4 — Fetching data in an effect
 *
 * What this shows
 *   Loading the bank accounts from the API when the page appears. The pattern has four parts:
 *
 *   1. THREE pieces of state describe the request: still loading, failed, or succeeded with data.
 *      We keep them in ONE state object { status, data, error } so they can never disagree
 *      (for example "loading" and "error" at the same time).
 *   2. The fetch happens in useEffect, because it is outside work (the network) that must start AFTER
 *      the first render.
 *   3. fetch rejects on network-level failures (e.g. no connection, blocked, aborted). When the
 *      server answers with an error status such as 500, fetch still succeeds, so we check `response.ok`
 *      ourselves. (This is documented fetch behaviour; the tests use a fake response with ok: false.)
 *   4. A request that is out of date must not overwrite a newer one. Click "Reload" twice quickly:
 *      the answers may come back in a different order. Each effect run owns a flag `ignore`; its
 *      cleanup sets the flag, and the answer is thrown away if the flag is set.
 *
 * async functions inside effects
 *   An effect function must return nothing, or a cleanup FUNCTION. An `async` function always returns a
 *   Promise. React 19 warns about that in development. When React later tries to run the effect's cleanup
 *   it finds a Promise instead of a function and throws TypeError "destroy is not a function". This
 *   happens on unmount or when the dependencies change, and in development under StrictMode right at
 *   mount. Production builds throw too (with a minified message). With no error boundary React removes
 *   the whole component tree. The cleanup you meant to return never runs (BrokenAsyncEffect below,
 *   proved in the tests). The fix is to keep the effect itself normal and define-and-call an async
 *   function INSIDE it, as AccountsList does.
 *
 * StrictMode and requests
 *   In development StrictMode runs the effect twice, so you will see two requests (4 in lesson 5). The
 *   first answer is ignored by the ignore flag: this is exactly why the flag matters. Ignoring only
 *   discards the answer; the request still completes. AbortController can cancel the request itself
 *   (not covered here).
 *
 * API shape (GET /api/accounts), camelCase JSON: an array of
 * { accountId, customerName, accountNumber, accountType, balance }. This matches the Phase 6 mini-project API
 * (the WebApiCrud topic's AccountDto). accountId is the unique key for each table row.
 * The vite dev server forwards /api to the Web API (see vite.config.js). If the API is not running,
 * this page shows the error state, which is itself a good thing to look at.
 *
 * Tests never touch the network: they replace fetch with a fake (vi.fn).
 */
import { useEffect, useState } from 'react';

export const title = 'Effects 4 — Fetching data';

export function AccountsList() {
  const [state, setState] = useState({ status: 'loading', data: null, error: null });
  const [reloadCount, setReloadCount] = useState(0);

  useEffect(() => {
    let ignore = false; // belongs to THIS run of the effect

    async function load() {
      setState({ status: 'loading', data: null, error: null });
      try {
        const response = await fetch('/api/accounts');
        if (!response.ok) throw new Error(`The server answered ${response.status}`);
        const accounts = await response.json();
        if (!ignore) setState({ status: 'success', data: accounts, error: null });
      } catch (err) {
        if (!ignore) setState({ status: 'error', data: null, error: err.message });
      }
    }

    load(); // the effect itself is NOT async; it just starts the async function
    return () => {
      ignore = true; // this run is out of date from now on (re-run or unmount)
    };
  }, [reloadCount]);

  return (
    <section>
      <h3>Accounts</h3>
      <button onClick={() => setReloadCount((n) => n + 1)}>Reload</button>
      {state.status === 'loading' && <p>Loading accounts…</p>}
      {state.status === 'error' && <p role="alert">Could not load accounts: {state.error}</p>}
      {state.status === 'success' && (
        <table cellPadding={6}>
          <thead>
            <tr>
              <th>Customer</th>
              <th>Number</th>
              <th>Type</th>
              <th>Balance</th>
            </tr>
          </thead>
          <tbody>
            {state.data.map((a) => (
              <tr key={a.accountId}>
                <td>{a.customerName}</td>
                <td>{a.accountNumber}</td>
                <td>{a.accountType}</td>
                <td>{a.balance.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

// WRONG on purpose, kept only so the tests can prove what goes wrong: an async effect function.
// `onCleanup` is a stand-in for real cleanup work.
export function BrokenAsyncEffect({ onCleanup }) {
  useEffect(async () => {
    return () => onCleanup(); // returned inside a Promise: React never receives it
  }, []);
  return <p>Broken async effect</p>;
}

export default function Lesson() {
  return (
    <div>
      <h2>Effects 4 — Fetching data</h2>
      <p>Status moves loading → success or error. Press Reload to start a new request.</p>
      <AccountsList />
    </div>
  );
}
