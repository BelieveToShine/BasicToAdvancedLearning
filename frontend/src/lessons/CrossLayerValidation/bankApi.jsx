/*
 * Small helpers shared by the five CrossLayerValidation lessons (this file is not a lesson itself:
 * its name does not start with a digit, so the lesson picker ignores it).
 *
 * The API contract it relies on is in FullStackBank/README.md: every error answer is
 * application/problem+json with { title, status, detail?, traceId } and, for a 400, an `errors`
 * object keyed by the camelCase field name.
 */
import { useCallback, useEffect, useState } from 'react';

export const API_DOWN_TEXT = 'API not reachable — start it (see FullStackBank/README.md).';

// Sends one request and ALWAYS returns a plain object (it never throws), so the screens can use
// ordinary if/else on the result instead of wrapping every call in try/catch.
//   network: true  -> the browser got no HTTP answer at all (server down, wrong address, no connection):
//                     fetch rejects with a TypeError and there is no status code.
//   status, ok     -> the HTTP status code (404, 422...) and whether it was 2xx.
//   data           -> the parsed JSON body, or null (204, or a body that is not JSON).
//   problem        -> data, but only when the request failed AND the body is a ProblemDetails object.
// `fetchFn` is a parameter only so a lesson (or a test) can replace the real network.
export async function callApi(method, url, body, fetchFn = (...args) => fetch(...args)) {
  let response;
  try {
    response = await fetchFn(url, {
      method,
      headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    return { network: true, ok: false, status: 0, data: null, problem: null };
  }
  let data = null;
  const type = response.headers.get('Content-Type') ?? '';
  if (response.status !== 204 && type.includes('json')) {
    try {
      data = await response.json();
    } catch {
      data = null; // said JSON but was not
    }
  }
  const looksLikeProblem = data !== null && typeof data === 'object' && typeof data.title === 'string' && typeof data.status === 'number';
  return { network: false, ok: response.ok, status: response.status, data, problem: !response.ok && looksLikeProblem ? data : null };
}

// Puts a failed (or successful) result into one of a few boxes. The screen decides WHERE to show
// the message by the box, not by guessing from the text.
//   field     400 with an `errors` object  -> show each message next to its input      (lesson 02)
//   business  422                          -> a rule refused a valid request: form-level (lesson 03)
//   conflict  409                          -> clashes with data that already exists: form-level (lesson 03)
//   notFound  404
//   server    5xx WITH a ProblemDetails body -> the API itself said "unexpected error"  (lesson 04)
//   gateway   5xx WITHOUT one                -> something in front of the API answered (a proxy / gateway)
//   network   no HTTP answer at all
export function classify(result) {
  if (result.network) return 'network';
  if (result.ok) return 'ok';
  const { status, problem } = result;
  if (status === 400 && problem && problem.errors && typeof problem.errors === 'object') return 'field';
  if (status === 404) return 'notFound';
  if (status === 409) return 'conflict';
  if (status === 422) return 'business';
  if (status >= 500) return problem ? 'server' : 'gateway';
  return 'other';
}

// Sentence for the person at the screen. For 5xx and network failures it is deliberately generic:
// nothing the API said about its insides is passed on, because the 500 body has none (and a user could not act on it).
export function friendlyMessage(result) {
  const kind = classify(result);
  if (kind === 'network') return 'Could not reach the server. Check your connection, then try again.';
  if (kind === 'gateway') return `The API is not answering (HTTP ${result.status} from something in front of it). ${API_DOWN_TEXT}`;
  if (kind === 'server') {
    const ref = result.problem.traceId ? ` If it keeps happening, quote this reference: ${result.problem.traceId}` : '';
    return `Something went wrong on our side, not in what you typed. Please try again in a moment.${ref}`;
  }
  const p = result.problem;
  if (p) return p.detail ? `${p.title}: ${p.detail}` : p.title;
  return `The request failed (HTTP ${result.status}).`;
}

export function isApiDown(result) {
  const kind = classify(result);
  return kind === 'network' || kind === 'gateway';
}

// Splits a 400 `errors` object into messages for known input fields and everything else.
// Keys are camelCase field names ("amount"). Other keys are not an input of the form: "$.amount" is a JSON
// path (the value had the wrong type or format), "$" means the body was not valid JSON, "body" means it was
// empty. Their messages are plain sentences, so they go to the "general" list unchanged.
export function splitProblemErrors(problem, knownFields) {
  const byField = {};
  const general = [];
  const errors = (problem && problem.errors) || {};
  for (const [key, messages] of Object.entries(errors)) {
    const list = Array.isArray(messages) ? messages : [String(messages)];
    if (knownFields.includes(key)) byField[key] = list;
    else general.push(...list);
  }
  return { byField, general };
}

export function money(value) {
  return Number(value).toFixed(2);
}

// Loads the account list once (and again when reload() is called).
// status: 'loading' | 'ready' | 'down' (no usable API) | 'error' (the API answered, but with a failure)
export function useAccounts() {
  const [state, setState] = useState({ status: 'loading', accounts: [], result: null });
  const [tick, setTick] = useState(0);
  useEffect(() => {
    let cancelled = false;
    callApi('GET', '/api/accounts').then((result) => {
      if (cancelled) return; // the lesson was closed before the answer arrived
      if (result.ok && Array.isArray(result.data)) setState({ status: 'ready', accounts: result.data, result });
      else setState({ status: isApiDown(result) ? 'down' : 'error', accounts: [], result });
    });
    return () => {
      cancelled = true;
    };
  }, [tick]);
  const reload = useCallback(() => setTick((t) => t + 1), []);
  return { ...state, reload };
}

// Shown by every lesson that needs the API, instead of a blank page.
export function ApiDown({ onRetry }) {
  return (
    <p role="alert" style={{ background: '#fdecea', border: '1px solid #cc0000', padding: 10 }}>
      {API_DOWN_TEXT}{' '}
      {onRetry && <button type="button" onClick={onRetry}>Check again</button>}
    </p>
  );
}

export const box = { border: '1px solid #bbb', borderRadius: 6, padding: 12, margin: '12px 0' };
export const bad = { color: '#a00000' };
