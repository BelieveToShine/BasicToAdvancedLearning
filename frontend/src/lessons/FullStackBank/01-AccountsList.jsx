/*
 * Mini project, step 1 - the accounts list (React -> fetch -> Vite proxy -> Web API -> SQL Server).
 *
 * From this topic on the pages are no longer built from data hard-coded in the file. The data lives
 * in SQL Server; React only ASKS for it, over HTTP, and keeps a COPY in state while the screen is open.
 *
 * What happens when this screen appears (the same journey is drawn in Flow/1-AccountsList.svg):
 *   1. React renders the first time with nothing loaded yet: status "loading".
 *   2. An effect (useLoad in parts.jsx) calls api.listAccounts(type) -> fetch('/api/accounts').
 *   3. The Vite dev server's proxy sees the path starts with /api and forwards it to the Web API.
 *   4. The controller asks EF Core, EF Core sends SELECT ... FROM Accounts to SQL Server.
 *   5. The rows come back as JSON; React stores them in state and renders again: status "ok".
 *
 * The filter works the same way: the chosen type ("", "Savings", "Current") is STATE. When it changes,
 * the effect runs again with a new URL: /api/accounts?type=Savings. The filtering is done by the API
 * (a WHERE in SQL), not by JavaScript here - the browser only ever holds the rows it asked for.
 *
 * Development note: React StrictMode (on in this app's main.jsx) runs effects twice on purpose, so in
 * `vite dev` you may see the same GET twice in the browser's network tab; normally a
 * production build sends it once. The `ignore` flag in useLoad makes the double run harmless.
 *
 * Three honest states are always handled, so the page is never blank: loading, error, data.
 * "Error" includes "API not reachable" (the API is not running): a friendly message, not a crash.
 */
import { useState } from 'react';
import { api, money } from './api.js';
import { ErrorView, Journey, useLoad, STATE, FROM_API, useFreshCallLog } from './parts.jsx';

export const title = '1. Accounts list (GET /api/accounts, filter ?type=)';

const TYPES = ['', 'Savings', 'Current']; // '' means "all": no ?type= in the URL

// Exported so the later lessons can reuse it. `onOpen` is optional: later lessons pass it to make rows clickable.
export function AccountsList({ onOpen }) {
  const [type, setType] = useState(''); // STATE: which filter button is selected
  const [state, reload] = useLoad(() => api.listAccounts(type), [type]);

  return (
    <div>
      <p>
        Show:{' '}
        {TYPES.map((t) => (
          <button key={t} aria-pressed={type === t} onClick={() => setType(t)} style={{ fontWeight: type === t ? 'bold' : 'normal', marginRight: 4 }}>
            {t || 'All'}
          </button>
        ))}
      </p>

      {state.status === 'loading' && <p>Loading accounts...</p>}
      {state.status === 'error' && <ErrorView error={state.error} onRetry={reload} />}
      {state.status === 'ok' && (
        <table aria-label="accounts">
          <thead>
            <tr>
              <th>Number</th>
              <th>Owner</th>
              <th>Type</th>
              <th>Balance</th>
              {onOpen && <th />}
            </tr>
          </thead>
          <tbody>
            {state.data.map((a) => (
              <tr key={a.accountId}>
                <td>{a.accountNumber}</td>
                <td>{a.customerName}</td>
                <td>{a.accountType}</td>
                <td style={{ textAlign: 'right' }}>{money(a.balance)}</td>
                {onOpen && (
                  <td>
                    <button onClick={() => onOpen(a.accountId)}>Open {a.accountNumber}</button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {state.status === 'ok' && state.data.length === 0 && <p>No accounts of this type.</p>}
    </div>
  );
}

export default function Lesson() {
  useFreshCallLog(); // the Journey panel starts empty for this lesson
  return (
    <div>
      <h2>Step 1: the accounts list</h2>
      <AccountsList />
      <Journey
        shows={[
          ['filter buttons (which type is selected)', STATE],
          ['the table rows', `${FROM_API} <- SQL Server table Accounts`],
          ['owner name', 'SQL Server table Customers (the API joins it for us)'],
        ]}
      />
    </div>
  );
}
