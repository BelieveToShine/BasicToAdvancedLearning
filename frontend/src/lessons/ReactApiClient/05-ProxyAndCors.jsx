/*
 * LESSON 5 — Why the dev proxy? The same-origin rule and CORS
 *
 * The problem
 *   The React page is served by the Vite dev server (for example http://localhost:5173). The bank API is a
 *   different program on a different port (for example http://localhost:5080). Port is part of an ORIGIN:
 *   origin = scheme + host + port. Two URLs with a different scheme, host or port are different origins.
 *
 * What the browser does about it (CORS)
 *   - By default the browser stops JavaScript from READING a response that comes from a different origin.
 *     The page below calls the API directly and you will see the call fail.
 *   - The server can say "this origin may read my answers" by adding response headers, for example
 *     Access-Control-Allow-Origin. This system of headers is called CORS (Cross-Origin Resource Sharing).
 *   - WHO ENFORCES IT: the BROWSER. The server only sends (or does not send) the headers. In the real run behind
 *     this lesson, a direct GET reached the API and the API answered 200 with the account list; the browser then
 *     refused to hand that answer to the page, and fetch rejected with a TypeError. (The page cannot show this,
 *     because the rejection hides the details; the browser's network log shows it. Check yours with the DevTools
 *     Network tab.)
 *   - "Simple" requests (a GET, or a POST with a form-style Content-Type) are sent first and judged afterwards.
 *     A request such as POST with 'Content-Type: application/json' is NOT simple: the browser first sends an
 *     OPTIONS request, called a PREFLIGHT, asking "may I?". If the answer does not allow it, the real POST is
 *     never sent. (Checked in the real run: only the OPTIONS request left the browser.)
 *   - From JavaScript you cannot tell a CORS block from "server down": both are a rejected fetch with a
 *     TypeError. The browser console explains which one it was.
 *   - CORS does NOT protect the API from other callers. curl, Postman or another server ignore it completely.
 *     It limits what a web page you visit can read with your browser. The API still needs its own checks and
 *     (later) authentication.
 *
 * Two ways out, for a front end and an API on different origins
 *   1. Make the browser see ONE origin: the dev proxy. The page calls a relative URL (/api/accounts), so the
 *      request goes to the Vite server (same origin as the page). Vite's server then makes the request to the
 *      API itself (a server-to-server call: no browser, no CORS) and passes the answer back. See vite.config.js:
 *      server.proxy { '/api': 'http://localhost:5080' }. No CORS setup is needed while learning.
 *      (The string form also sets changeOrigin: true in the pinned Vite, so the API sees ITS OWN address as Host; see the Location note below.)
 *   2. Teach the API to send CORS headers that allow the page's origin (a setting inside the API, outside
 *      this front-end lesson).
 *
 * A proxy in vite.config.js belongs to the dev server (and `vite preview`). A built site that is deployed
 * has neither: there, a web server that forwards /api to the API plays the proxy's role, or the API sends
 * CORS headers. (Deployment is a later topic.)
 *
 * A trap found while building this lesson: the Location header
 *   Through the proxy the 201's Location header is an absolute URL with the API's OWN address
 *   (http://localhost:5080/api/accounts/7), not the address the page used. Calling fetch(location) from the page
 *   would be a cross-origin request. Keep only the path (api.js `pathOf`) and request that through the proxy.
 *   The "Location" button below shows the two origins side by side.
 *   Why the API's own address? vite.config.js writes the proxy as a string, `'/api': 'http://localhost:5080'`. In the
 *   pinned Vite the string form sets changeOrigin: true, so the API sees its own address as the Host header and builds
 *   Location from it (checked in a real run with a throw-away upstream). A proxy WITHOUT changeOrigin keeps the page's
 *   Host, and Location would then name the page's address; that second half is http-proxy semantics, not run here.
 */
import { useState } from 'react';
import { pathOf } from './api.js';

export const title = 'API client 5 — Proxy and CORS';

// Same origin = same scheme, host AND port. URL.origin is exactly that triple.
export function sameOrigin(a, b) {
  return new URL(a).origin === new URL(b).origin;
}

const PAGE_EXAMPLE = 'http://localhost:5173/index.html';
const EXAMPLES = [
  ['http://localhost:5173/api/accounts', 'same scheme, host and port'],
  ['http://localhost:5080/api/accounts', 'different PORT'],
  ['https://localhost:5173/api/accounts', 'different SCHEME'],
  ['http://127.0.0.1:5173/api/accounts', 'different HOST (even though it is the same machine)'],
];

export function OriginTable() {
  return (
    <section>
      <h3>Same origin or not?</h3>
      <p>
        This page (example): <code>{PAGE_EXAMPLE}</code> — and the page you are on now is <code>{window.location.origin}</code>
      </p>
      <table cellPadding={5} border={1} style={{ borderCollapse: 'collapse' }}>
        <tbody>
          {EXAMPLES.map(([url, why]) => (
            <tr key={url}>
              <td>
                <code>{url}</code>
              </td>
              <td>{sameOrigin(PAGE_EXAMPLE, url) ? 'same origin' : 'different origin'}</td>
              <td>{why}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

export function ThroughTheProxy() {
  const [result, setResult] = useState(null);
  async function go() {
    setResult(null);
    try {
      const response = await fetch('/api/accounts');
      // response.url is the URL the browser actually requested.
      if (!response.ok) {
        // 502-504 with a non-JSON body = the dev proxy could not reach the API; any other non-ok status is just reported.
        const type = response.headers.get('Content-Type') ?? '';
        const down = [502, 503, 504].includes(response.status) && !type.includes('json');
        setResult({ ok: false, text: down ? `Status ${response.status}: API not reachable — start it (see FullStackBank/README.md).` : `Status ${response.status}. The browser requested: ${response.url}` });
        return;
      }
      const list = await response.json();
      setResult({ ok: true, text: `Status ${response.status}, ${list.length} accounts. The browser requested: ${response.url}` });
    } catch {
      setResult({ ok: false, text: 'No answer. Is the API running? (see FullStackBank/README.md)' });
    }
  }
  return (
    <section>
      <h3>1. Relative URL, through the proxy</h3>
      <p>
        <code>fetch('/api/accounts')</code> resolves to <code>{new URL('/api/accounts', window.location.href).href}</code>: this page's own origin.
      </p>
      <button onClick={go}>GET /api/accounts</button>
      {result && <p role="status">{result.text}</p>}
    </section>
  );
}

export function DirectToTheApi() {
  const [base, setBase] = useState('http://localhost:5080');
  const [result, setResult] = useState(null);

  async function attempt(label, init) {
    setResult(null);
    try {
      const response = await fetch(`${base}/api/accounts`, init);
      setResult({ label, text: `The page was allowed to read the answer: status ${response.status}.` });
    } catch (err) {
      setResult({
        label,
        text: `fetch rejected (${err.name}). Either the browser blocked the answer (CORS: this API sends no Access-Control-Allow-Origin) or nothing is listening at that address. The Network tab and console say which.`,
      });
    }
  }

  return (
    <section>
      <h3>2. Directly to the API's own address (a different origin)</h3>
      <label>
        API address <input value={base} onChange={(e) => setBase(e.target.value)} size={26} />
      </label>
      <p>Same machine, different port: the browser treats it as another origin. Do not edit the address unless your API runs elsewhere.</p>
      <button onClick={() => attempt('GET', undefined)}>Direct GET</button>{' '}
      <button
        onClick={() =>
          attempt('POST', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            // Deliberately invalid body (empty account number): even if it WERE sent, nothing would be created.
            body: JSON.stringify({ customerId: 1, accountNumber: '', accountType: 'Current' }),
          })
        }
      >
        Direct POST (JSON)
      </button>
      {result && (
        <p role="status">
          {result.label}: {result.text}
        </p>
      )}
    </section>
  );
}

export function LocationHeader() {
  const [result, setResult] = useState(null);

  async function go() {
    setResult(null);
    try {
      const number = 'LOC-' + Math.random().toString(36).slice(2, 8).toUpperCase();
      const response = await fetch('/api/accounts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ customerId: 1, accountNumber: number, accountType: 'Current' }),
      });
      if (response.status !== 201) throw new Error(`status ${response.status}`);
      const location = response.headers.get('Location');
      const created = await response.json();
      // Tidy up through the proxy, using only the PATH of the Location.
      await fetch(pathOf(location), { method: 'DELETE' });
      setResult({ location, created: created.accountNumber, locationOrigin: new URL(location).origin, pageOrigin: window.location.origin, path: pathOf(location) });
    } catch (err) {
      setResult({ error: `Could not run the demo: ${err.message}. Is the API running? (see FullStackBank/README.md)` });
    }
  }

  return (
    <section>
      <h3>3. The Location header and the two origins</h3>
      <button onClick={go}>Open an account and inspect Location</button>
      {result?.error && <p role="alert">{result.error}</p>}
      {result?.location && (
        <div role="status">
          <p>
            Location header: <code>{result.location}</code>
          </p>
          <p>
            Its origin: <code>{result.locationOrigin}</code> — page origin: <code>{result.pageOrigin}</code> —{' '}
            <strong>{result.locationOrigin === result.pageOrigin ? 'same origin' : 'different origin'}</strong>
          </p>
          <p>
            Path to use with the proxy: <code>{result.path}</code> (the demo opened {result.created} and deleted it again).
          </p>
        </div>
      )}
    </section>
  );
}

export default function Lesson() {
  return (
    <div>
      <h2>API client 5 — Proxy and CORS</h2>
      <OriginTable />
      <ThroughTheProxy />
      <DirectToTheApi />
      <LocationHeader />
    </div>
  );
}
