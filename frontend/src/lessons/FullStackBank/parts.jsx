// Small shared pieces for the FullStackBank lessons. (Not a lesson: the name does not start with a digit.)
import { useEffect, useLayoutEffect, useState, useSyncExternalStore } from 'react';
import { callLog } from './api.js';

// useLoad: "run this API call when the screen appears (and again when deps change) and give me its state".
//   state.status is 'loading' | 'ok' | 'error';  state.data (ok) or state.error (an ApiError).
// The `ignore` flag matters: if the user switches to another account before the first answer arrives,
// the late answer for the OLD account must not overwrite the NEW one. The effect's cleanup sets the flag.
// Returns [state, reload]; reload() runs the call again.
export function useLoad(load, deps) {
  const [state, setState] = useState({ status: 'loading' });
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    let ignore = false;
    setState({ status: 'loading' });
    load().then(
      (data) => !ignore && setState({ status: 'ok', data }),
      (error) => !ignore && setState({ status: 'error', error }),
    );
    return () => {
      ignore = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- the caller lists exactly what should re-run the call
  }, [...deps, attempt]);
  return [state, () => setAttempt((a) => a + 1)];
}

// useFreshCallLog: a lesson page calls this once so its Journey panel starts empty instead of showing
// requests left over from the previously opened lesson. A layout effect runs before any useEffect, so it
// clears the log BEFORE the screen's first fetch is recorded.
export function useFreshCallLog() {
  useLayoutEffect(() => {
    callLog.clear();
  }, []);
}

// One component for every failure, so each screen only says WHAT failed, not how to print it.
//  - unreachable: the friendly "start the API" message (the page must never be blank)
//  - otherwise: the API's problem+json (title, detail, and per-field errors for a 400)
export function ErrorView({ error, onRetry }) {
  if (error.unreachable) {
    return (
      <div role="alert" style={box('#fdecea', '#d93025')}>
        <strong>API not reachable</strong> - start it (see FullStackBank/README.md), then try again.
        {onRetry && (
          <>
            {' '}
            <button onClick={onRetry}>Try again</button>
          </>
        )}
      </div>
    );
  }
  const { status, problem } = error;
  const fields = problem?.errors ? Object.entries(problem.errors) : [];
  return (
    <div role="alert" style={box('#fff4e5', '#e69138')}>
      <strong>
        {status} {problem?.title ?? 'Error'}
      </strong>
      {problem?.detail && <div>{problem.detail}</div>}
      {fields.length > 0 && (
        <ul style={{ margin: '4px 0 0' }}>
          {fields.map(([field, messages]) => (
            <li key={field}>
              <code>{field}</code>: {messages.join(' ')}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

const box = (bg, border) => ({ background: bg, border: `1px solid ${border}`, borderRadius: 6, padding: 8, margin: '8px 0' });

// The "journey of one request" panel. `shows` says where each piece of data on the screen comes from:
// [label, where] pairs. The rest is read from the call log in api.js (the newest call).
const STEPS = [
  ['1 React', 'a handler or an effect calls api.js'],
  ['2 fetch', 'sends the HTTP request'],
  ['3 Vite proxy', 'forwards /api/... to the API (development)'],
  ['4 Web API', 'controller -> EF Core'],
  ['5 SQL Server', 'runs the SQL, returns rows'],
];

export function Journey({ shows = [] }) {
  const calls = useSyncExternalStore(callLog.subscribe, callLog.getSnapshot);
  const last = calls[calls.length - 1];
  return (
    <section aria-label="journey of one request" style={{ border: '1px dashed #999', borderRadius: 6, padding: 8, marginTop: 16, background: '#fafafa', fontSize: 13 }}>
      <strong>Journey of one request: React -&gt; fetch -&gt; Vite proxy -&gt; API -&gt; SQL Server</strong>
      <div style={{ display: 'flex', gap: 4, margin: '6px 0', flexWrap: 'wrap' }}>
        {STEPS.map(([name, what]) => (
          <div key={name} style={{ border: '1px solid #bbb', borderRadius: 4, padding: '2px 6px', background: '#fff' }}>
            <strong>{name}</strong>
            <div style={{ color: '#555' }}>{what}</div>
          </div>
        ))}
      </div>
      {last ? (
        <div data-testid="last-call">
          Latest request: <code>{last.method} {last.url}</code>
          {last.body && <> with body <code>{JSON.stringify(last.body)}</code></>} -&gt; answer{' '}
          <strong data-testid="last-status">{last.status ?? '...waiting'}</strong>
          <div>SQL that ran: <code>{last.sql}</code></div>
        </div>
      ) : (
        <div>No request yet.</div>
      )}
      {calls.length > 1 && (
        <div>
          Requests so far (oldest first):{' '}
          <span data-testid="call-history">{calls.map((c) => `${c.method} ${c.url} ${c.status ?? '...'}`).join('  |  ')}</span>
        </div>
      )}
      <div style={{ color: '#555', marginTop: 4 }}>
        Note: in development, React StrictMode runs effects twice on purpose, so you may see the same GET twice in the browser's network tab; normally a production build sends it once.
      </div>
      {shows.length > 0 && (
        <table style={{ marginTop: 6, borderCollapse: 'collapse' }}>
          <caption style={{ textAlign: 'left' }}>Where the data on this screen lives:</caption>
          <tbody>
            {shows.map(([label, where]) => (
              <tr key={label}>
                <td style={{ padding: '1px 8px 1px 0' }}>{label}</td>
                <td style={{ padding: '1px 0' }}>{where}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

export const STATE = 'React state in this component';
export const FROM_API = 'API response (copied into React state)';
