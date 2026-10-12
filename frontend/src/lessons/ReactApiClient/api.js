/*
 * api.js — the ONE place in the front end that knows how to talk to the bank Web API.
 *
 * Lesson 4 explains why. In short: components should say WHAT they want ("list the accounts",
 * "open an account") and never know HOW (URL, headers, JSON, status codes). If the base path or
 * an error shape changes, you edit this file, not every component.
 *
 * What this module does for every call
 *   1. Builds the URL from one BASE path ('/api'). It is a RELATIVE URL, so the browser sends it to
 *      the page's own origin: in development that is the Vite server, which forwards /api to the API.
 *   2. Sets the headers: Accept always, and 'Content-Type: application/json' only when there is a body.
 *      (Without that Content-Type the API answers 415 — see lesson 2.)
 *   3. Turns a JS value into a JSON string for the body, and the response body back into a value.
 *      An empty body (204) becomes null instead of crashing response.json().
 *   4. Makes every failure look the same: it THROWS an ApiError. Callers write one try/catch and
 *      read the same few properties, whatever went wrong.
 */

export const BASE = '/api';

// One small error class. `kind` is the part the UI usually needs; the rest is detail.
//   network     the browser got no usable answer (fetch rejected) or the dev proxy could not reach the API
//   validation  400: look at fieldErrors, e.g. { accountNumber: ['accountNumber is required.'] }
//   notFound    404
//   conflict    409
//   rule        422: the request was valid but a business rule said no (e.g. insufficient funds)
//   server      5xx from the API itself
//   other       any other non-2xx status
export class ApiError extends Error {
  constructor({ kind, status = 0, title, detail = '', fieldErrors = {}, traceId = null }) {
    super(title);
    this.name = 'ApiError';
    this.kind = kind;
    this.status = status; // 0 means "no HTTP answer at all"
    this.title = title;
    this.detail = detail;
    this.fieldErrors = fieldErrors;
    this.traceId = traceId;
  }
}

// Read the body once, as text, and parse it only when the server says it is JSON.
// ('application/problem+json' contains the word json, so error bodies are parsed too.)
export async function readBody(response) {
  const text = await response.text();
  if (text === '') return null; // 204 No Content, or any empty body
  const type = response.headers.get('Content-Type') ?? '';
  if (!type.includes('json')) return text;
  try {
    return JSON.parse(text);
  } catch {
    return text; // the server SAID json but sent something else: keep the raw text
  }
}

// Map a non-2xx answer to an ApiError. `data` is whatever readBody returned.
export function errorFromResponse(response, data) {
  const status = response.status;
  const isProblem = data !== null && typeof data === 'object' && typeof data.title === 'string';
  // The API always answers errors with a problem+json body. A plain-text 502/503/504 therefore did not
  // come from the API: in development it is the Vite proxy saying "I could not reach the API".
  if (!isProblem && [502, 503, 504].includes(status)) {
    return new ApiError({ kind: 'network', status, title: 'API not reachable', detail: `Answered ${status} without a problem body: the API is probably not running.` });
  }
  const kind =
    status === 400 ? 'validation' : status === 404 ? 'notFound' : status === 409 ? 'conflict' : status === 422 ? 'rule' : status >= 500 ? 'server' : 'other';
  return new ApiError({
    kind,
    status,
    title: isProblem ? data.title : `The server answered ${status}`,
    detail: isProblem ? (data.detail ?? '') : '',
    fieldErrors: isProblem && data.errors ? data.errors : {},
    traceId: isProblem ? (data.traceId ?? null) : null,
  });
}

// The one function that calls fetch. Returns { status, data, location } or throws ApiError.
export async function request(method, path, body) {
  const headers = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  let response;
  try {
    response = await fetch(BASE + path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
  } catch {
    // fetch only REJECTS when no answer could be used (cannot connect, blocked by CORS, ...).
    throw new ApiError({ kind: 'network', title: 'API not reachable', detail: 'The request got no answer. Is the API running? (see FullStackBank/README.md)' });
  }
  let data;
  try {
    data = await readBody(response);
  } catch {
    // the connection broke while the body was still arriving: the same "no usable answer" case
    throw new ApiError({ kind: 'network', title: 'API not reachable', detail: 'The answer was cut off while it was being read.' });
  }
  if (!response.ok) throw errorFromResponse(response, data);
  return { status: response.status, data, location: response.headers.get('Location') };
}

// The Location header of a 201 is an absolute URL. Keep only its path ('/api/accounts/7') so it can be
// requested through the same origin as everything else (lesson 5 shows why).
export function pathOf(location) {
  return new URL(location, window.location.href).pathname;
}

// The bank, in the words the components use.
export const api = {
  listAccounts: async () => (await request('GET', '/accounts')).data,
  getAccount: async (id) => (await request('GET', `/accounts/${id}`)).data,
  createAccount: async (body) => {
    const { data, location } = await request('POST', '/accounts', body);
    return { account: data, location };
  },
  deleteAccount: async (id) => {
    await request('DELETE', `/accounts/${id}`);
  },
  listTransactions: async (id) => (await request('GET', `/accounts/${id}/transactions`)).data,
  addTransaction: async (id, body) => (await request('POST', `/accounts/${id}/transactions`, body)).data,
};
