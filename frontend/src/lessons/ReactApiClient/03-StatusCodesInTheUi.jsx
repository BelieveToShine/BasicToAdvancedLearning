/*
 * LESSON 3 — What each status code means for the screen
 *
 * What this shows
 *   The server's answer is not just "data or error". The STATUS CODE says what happened, and each one
 *   asks the UI to do something different. This page has a table of the codes the bank API uses and a lab
 *   that makes REAL requests so you can see each answer (status, ok, Content-Type, body).
 *
 * response.ok vs response.status
 *   response.ok is true exactly when the status is 200-299 (fetch documentation). So `if (!response.ok)` is the
 *   right first check for "something went wrong", but it throws away WHICH thing. Read response.status when the
 *   UI must react differently: a 404 is "no such account", a 409 is "that clashes with existing data".
 *   fetch itself never throws for a 404 or 500: those are answers. It only rejects when there is no usable answer.
 *
 * Success is not always JSON
 *   200 and 201 come with a JSON body. 204 No Content has NO body at all, so response.json() would throw
 *   (the tests prove it). Read the body only when the status says there is one.
 *
 * Error bodies are application/problem+json
 *   The API describes every error in one standard shape (RFC 9457 "problem details"): type, title, status, detail,
 *   traceId. response.json() parses it fine: json() does not look at the Content-Type, it just parses the text.
 *   Checking the Content-Type first is still wise: a proxy or a crashed server can answer with plain text.
 *   400 adds an `errors` object: { fieldName: [message, ...] } with the field names the API uses (camelCase).
 *   Those names match the form's state names here, so each message can be shown next to its own input.
 *
 * The statuses of this API (FullStackBank/README.md)
 *   400 invalid input - 404 no such account - 409 conflict (duplicate number, delete of an account with
 *   transactions, simultaneous change) - 422 valid request but a business rule says no (insufficient funds)
 *   - 500 unexpected. Other APIs make other choices (some use 409 for insufficient funds); your client must follow
 *   THIS API's contract.
 *
 * No answer at all
 *   A 502/503/504 with a plain-text body (the Vite dev proxy when the API is stopped) is not an answer from the API either:
 *   no problem body, no traceId. The lab treats it as "no answer" too (isGatewayDown).
 *   If fetch rejects (server down, no network, blocked by the browser), there is no status. This is a different
 *   case from "the server said no", and the UI says so: "cannot reach the API", with a Retry button.
 */
import { useState } from 'react';

export const title = 'API client 3 — Status codes in the UI';

// What each answer asks the UI to do. (A table in the page AND data the code uses.)
export const STATUS_GUIDE = [
  { code: '200', meaning: 'OK', ui: 'Show the data from the JSON body.' },
  { code: '201', meaning: 'Created', ui: 'Show success, use the new thing in the body; the Location header says where it lives.' },
  { code: '204', meaning: 'No Content', ui: 'Success with NO body: update the screen (e.g. remove the row). Do not call response.json().' },
  { code: '400', meaning: 'Bad Request', ui: 'Show each message from `errors` next to its own input.' },
  { code: '404', meaning: 'Not Found', ui: 'Say it does not exist (anymore). The code is fine; the id is not.' },
  { code: '409', meaning: 'Conflict', ui: 'Say what clashes (use `detail`). The user can change the input or reload.' },
  { code: '422', meaning: 'Unprocessable Content (older name: Unprocessable Entity)', ui: 'The request was valid, a business rule refused it: show `detail` (e.g. insufficient funds).' },
  { code: '500', meaning: 'Server error', ui: 'Say "something went wrong, try again". Do not guess; the body has no exception text, only a traceId to quote.' },
  { code: 'none', meaning: 'fetch rejected', ui: 'No answer: "cannot reach the API", with a Retry button.' },
];

// A 502/503/504 whose body is NOT a problem+json is not an answer from the API: in development it is the Vite proxy saying
// "I could not reach the API". It is treated like "no answer at all" (same idea as isGatewayDown in lessons 1-2).
export const isGatewayDown = (status, contentType) => [502, 503, 504].includes(status) && !(contentType ?? '').includes('json');

export function guideFor(status, contentType = 'application/problem+json') {
  if (status === null || isGatewayDown(status, contentType)) return STATUS_GUIDE.find((g) => g.code === 'none');
  const exact = STATUS_GUIDE.find((g) => g.code === String(status));
  if (exact) return exact;
  return status >= 500 ? STATUS_GUIDE.find((g) => g.code === '500') : { code: String(status), meaning: '', ui: 'Not used by this API: handle it as a general failure.' };
}

// Read the body only when there is one, and parse it only when the server says it is JSON.
export async function readBody(response) {
  const text = await response.text();
  if (text === '') return null;
  const type = response.headers.get('Content-Type') ?? '';
  if (!type.includes('json')) return text;
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

// One real call, summarised for display. Never throws: "no answer" is part of the result.
export async function call(method, url, body) {
  const label = `${method} ${url}`;
  let response;
  try {
    response = await fetch(url, {
      method,
      headers: body === undefined ? {} : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch (err) {
    return { label, status: null, ok: null, contentType: null, body: null, rejected: err.name, unreachable: true };
  }
  const contentType = response.headers.get('Content-Type');
  if (isGatewayDown(response.status, contentType)) return { label, status: null, ok: null, contentType: null, body: null, rejected: 'API not reachable (proxy answered ' + response.status + ')', unreachable: true };
  return { label, status: response.status, ok: response.ok, contentType, body: await readBody(response) };
}

// A short text for the body column. Shown to teach, so it names the interesting parts.
function summarise(entry) {
  if (entry.rejected) return entry.rejected.startsWith('API not reachable') ? `${entry.rejected} — treated as no answer, there is no status` : `fetch rejected with ${entry.rejected} — there is no status`;
  const b = entry.body;
  if (b === null) return '(no body)';
  if (Array.isArray(b)) return `array of ${b.length} accounts`;
  if (typeof b === 'object' && b.errors) return `${b.title} — errors: ${JSON.stringify(b.errors)}`;
  if (typeof b === 'object' && b.title) return `${b.title}${b.detail ? ' — ' + b.detail : ''}`;
  if (typeof b === 'object' && b.accountNumber) return `account ${b.accountNumber} (id ${b.accountId})`;
  return String(b).slice(0, 80);
}

const uniqueNumber = () => 'LAB-' + Math.random().toString(36).slice(2, 8).toUpperCase();
const newAccount = (accountNumber) => ({ customerId: 1, accountNumber, accountType: 'Current' });

// Each scenario returns { entries, shown } : all calls it made, and which entry is "the answer being taught".
// Setup and tidy-up calls are listed too (nothing hidden), and the lab removes what it created.
const SCENARIOS = {
  200: async () => {
    const e = await call('GET', '/api/accounts');
    return { entries: [e], shown: 0 };
  },
  201: async () => {
    const created = await call('POST', '/api/accounts', newAccount(uniqueNumber()));
    const tidy = created.status === 201 ? [await call('DELETE', `/api/accounts/${created.body.accountId}`)] : [];
    return { entries: [created, ...tidy], shown: 0 };
  },
  204: async () => {
    const created = await call('POST', '/api/accounts', newAccount(uniqueNumber()));
    if (created.status !== 201) return { entries: [created], shown: 0 };
    const del = await call('DELETE', `/api/accounts/${created.body.accountId}`);
    return { entries: [created, del], shown: 1 };
  },
  400: async () => {
    const e = await call('POST', '/api/accounts', { customerId: 1, accountNumber: '', accountType: 'Gold' });
    return { entries: [e], shown: 0 };
  },
  404: async () => {
    const e = await call('GET', '/api/accounts/999999');
    return { entries: [e], shown: 0 };
  },
  409: async () => {
    const number = uniqueNumber();
    const first = await call('POST', '/api/accounts', newAccount(number));
    if (first.status !== 201) return { entries: [first], shown: 0 };
    const again = await call('POST', '/api/accounts', newAccount(number)); // same number twice
    const tidy = await call('DELETE', `/api/accounts/${first.body.accountId}`);
    return { entries: [first, again, tidy], shown: 1 };
  },
  422: async () => {
    const first = await call('POST', '/api/accounts', newAccount(uniqueNumber())); // a new account has balance 0
    if (first.status !== 201) return { entries: [first], shown: 0 };
    const id = first.body.accountId;
    const withdraw = await call('POST', `/api/accounts/${id}/transactions`, { kind: 'Withdrawal', amount: 10 });
    const tidy = await call('DELETE', `/api/accounts/${id}`);
    return { entries: [first, withdraw, tidy], shown: 1 };
  },
  none: async () => {
    // Port 5199 on this machine: nothing is expected to listen there, so the browser cannot connect.
    const e = await call('GET', 'http://127.0.0.1:5199/api/accounts');
    return { entries: [e], shown: 0 };
  },
};

export function StatusLab() {
  const [running, setRunning] = useState(null);
  const [result, setResult] = useState(null);

  async function run(key) {
    setRunning(key);
    setResult(null);
    const r = await SCENARIOS[key]();
    setResult({ key, ...r });
    setRunning(null);
  }

  return (
    <section>
      <h3>The lab: make a real request for each case</h3>
      <p>
        {Object.keys(SCENARIOS).map((key) => (
          <button key={key} disabled={running !== null} onClick={() => run(key)} style={{ marginRight: 6 }}>
            {key === 'none' ? 'No answer' : key}
          </button>
        ))}
      </p>
      {running && <p>Running {running}…</p>}
      {result && (
        <div>
          <table cellPadding={5} border={1} style={{ borderCollapse: 'collapse' }}>
            <thead>
              <tr>
                <th>Call</th>
                <th>status</th>
                <th>response.ok</th>
                <th>Content-Type</th>
                <th>Body</th>
              </tr>
            </thead>
            <tbody>
              {result.entries.map((e, i) => (
                <tr key={i} style={i === result.shown ? { background: '#fff3e6' } : undefined}>
                  <td>
                    <code>{e.label}</code>
                  </td>
                  <td>{e.status ?? '—'}</td>
                  <td>{e.ok === null ? '—' : String(e.ok)}</td>
                  <td>{e.contentType ?? '—'}</td>
                  <td>{summarise(e)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <p role="status">
            The highlighted row is the answer being taught. What the UI should do: <strong>{guideFor(result.entries[result.shown].status).ui}</strong>
          </p>
        </div>
      )}
    </section>
  );
}

// A form that does what the 400 row says: put each message next to its own input.
const FIELDS = ['customerId', 'accountNumber', 'accountType'];

export function FieldErrorForm() {
  const [values, setValues] = useState({ customerId: '1', accountNumber: '', accountType: '' });
  const [fieldErrors, setFieldErrors] = useState({});
  const [banner, setBanner] = useState(null);

  async function submit(e) {
    e.preventDefault();
    setFieldErrors({});
    setBanner(null);
    const entry = await call('POST', '/api/accounts', { ...values, customerId: Number(values.customerId) });
    if (entry.status === null) return setBanner('API not reachable — start it (see FullStackBank/README.md).');
    if (entry.status === 201) return setBanner(`Created ${entry.body.accountNumber}. (It stays in the demo database.)`);
    if (entry.status === 400) return setFieldErrors(entry.body.errors ?? {});
    // 409 and anything else: one message for the whole form, taken from the problem body.
    setBanner(`${entry.status}: ${entry.body?.title ?? 'Request failed'}${entry.body?.detail ? ' — ' + entry.body.detail : ''}`);
  }

  // Keys that do not belong to an input (for example "$" for unparseable JSON) must not vanish silently.
  const otherKeys = Object.keys(fieldErrors).filter((k) => !FIELDS.includes(k));

  return (
    <form onSubmit={submit}>
      <h3>A form that shows 400 errors next to the fields</h3>
      {FIELDS.map((name) => (
        <p key={name}>
          <label>
            {name}{' '}
            <input value={values[name]} onChange={(e) => setValues((v) => ({ ...v, [name]: e.target.value }))} aria-invalid={Boolean(fieldErrors[name])} />
          </label>
          {(fieldErrors[name] ?? []).map((m) => (
            <span key={m} role="alert" style={{ color: '#b00020', marginLeft: 8 }}>
              {m}
            </span>
          ))}
        </p>
      ))}
      {otherKeys.map((k) => (
        <p key={k} role="alert" style={{ color: '#b00020' }}>
          {k}: {fieldErrors[k].join(' ')}
        </p>
      ))}
      <button type="submit">Open account</button>
      {banner && <p role="status">{banner}</p>}
    </form>
  );
}

export default function Lesson() {
  return (
    <div>
      <h2>API client 3 — Status codes in the UI</h2>
      <table cellPadding={5} border={1} style={{ borderCollapse: 'collapse' }}>
        <thead>
          <tr>
            <th>Status</th>
            <th>Meaning</th>
            <th>What the UI should do</th>
          </tr>
        </thead>
        <tbody>
          {STATUS_GUIDE.map((g) => (
            <tr key={g.code}>
              <td>{g.code}</td>
              <td>{g.meaning}</td>
              <td>{g.ui}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <StatusLab />
      <FieldErrorForm />
    </div>
  );
}
