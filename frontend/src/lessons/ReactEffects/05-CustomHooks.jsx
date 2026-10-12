/*
 * LESSON 5 — Custom hooks: useFetch and useAccounts
 *
 * What this shows
 *   Lesson 4 put the whole loading/error/data pattern inside one component. The next component that needs
 *   data would copy-paste all of it. A CUSTOM HOOK is just a function whose name starts with `use`
 *   and that calls other hooks. Moving the pattern into useFetch(url) means a component only says
 *   WHAT it wants:
 *
 *     const { status, data, error, reload } = useAccounts();
 *
 * Important
 *   A custom hook shares LOGIC, not STATE. Every component that calls useAccounts() gets its own
 *   private loading/data/error state and makes its own request. (Proved in the tests: two components,
 *   two fetches, and reloading one does not touch the other.) To share one copy of the data between
 *   components you would keep it in a common parent and pass it down as props.
 *
 * In development StrictMode runs each effect twice, so lesson 5's page makes 4 requests; the first answer of
 * each hook is ignored by the ignore flag. Ignoring only discards the answer; the request still completes.
 *
 * Rules of hooks still apply inside a custom hook: call hooks at the top level, never inside
 * if/loops and never after an early return, so React sees the same hooks in the same order every render.
 */
import { useEffect, useState } from 'react';

export const title = 'Effects 5 — Custom hooks';

export function useFetch(url) {
  const [state, setState] = useState({ status: 'loading', data: null, error: null });
  const [reloadCount, setReloadCount] = useState(0);

  useEffect(() => {
    let ignore = false;

    async function load() {
      setState({ status: 'loading', data: null, error: null });
      try {
        const response = await fetch(url);
        if (!response.ok) throw new Error(`The server answered ${response.status}`);
        const data = await response.json();
        if (!ignore) setState({ status: 'success', data, error: null });
      } catch (err) {
        if (!ignore) setState({ status: 'error', data: null, error: err.message });
      }
    }

    load();
    return () => {
      ignore = true;
    };
  }, [url, reloadCount]); // a different url or a reload starts a fresh request

  return { ...state, reload: () => setReloadCount((n) => n + 1) };
}

// A hook built on a hook: the one place that knows the accounts endpoint.
export function useAccounts() {
  return useFetch('/api/accounts');
}

export function AccountsTable() {
  const { status, data, error, reload } = useAccounts();
  return (
    <section>
      <h3>Accounts table</h3>
      <button onClick={reload}>Reload the table</button>
      {status === 'loading' && <p>Loading the table…</p>}
      {status === 'error' && <p role="alert">Table error: {error}</p>}
      {status === 'success' && (
        <ul>
          {data.map((a) => (
            <li key={a.accountId}>
              {a.accountNumber} ({a.accountType}): {a.balance.toFixed(2)}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

export function TotalBalance() {
  const { status, data, error } = useAccounts();
  // The total is calculated while rendering (lesson 1): no effect, no extra state.
  const total = status === 'success' ? data.reduce((sum, a) => sum + a.balance, 0) : null;
  return (
    <section>
      <h3>Total balance</h3>
      {status === 'loading' && <p>Adding up…</p>}
      {status === 'error' && <p role="alert">Total error: {error}</p>}
      {status === 'success' && <p>Total across {data.length} accounts: {total.toFixed(2)}</p>}
    </section>
  );
}

export default function Lesson() {
  return (
    <div>
      <h2>Effects 5 — Custom hooks</h2>
      <p>Both sections below call useAccounts(). Each one makes its own request and owns its own state.</p>
      <AccountsTable />
      <TotalBalance />
    </div>
  );
}
