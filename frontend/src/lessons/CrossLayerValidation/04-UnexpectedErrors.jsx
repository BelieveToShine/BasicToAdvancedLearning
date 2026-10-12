/*
 * 04 - Unexpected errors: the server breaks, the network is down, or our own render code throws.
 *
 * Validation errors (400) and business-rule errors (422/409) are EXPECTED: the user can fix them. These are not:
 *
 *  1. 500 from the API. The API's exception handler turns any unexpected exception into a ProblemDetails with
 *     a generic title and detail and a traceId, and no exception text, SQL or stack trace (FullStackBank/README.md).
 *     The screen should say "our side, try again" and may show the traceId as a reference. Nothing in the body
 *     helps the user, and nothing in it should.
 *  2. Something that is not the API answers (a proxy or gateway): a 5xx whose body is NOT a ProblemDetails.
 *     With the Vite dev server in front of a stopped API this is 502 with a plain-text body.
 *  3. The network is down: fetch rejects (a TypeError) and there is NO status code at all.
 *     callApi() turns all three into a result object, so screens never need try/catch around every call.
 *
 *  4. An ERROR BOUNDARY catches errors thrown while React is RENDERING its children (and in their lifecycle
 *     methods), shows a fallback instead of a blank page, and lets the rest of the page keep working.
 *     It does NOT catch (React docs): errors in event handlers, in asynchronous code (setTimeout, promises),
 *     or errors thrown in the boundary itself. Those you handle with try/catch or .catch() yourself, or you
 *     hand them to the boundary by throwing from a state updater (last button below).
 *     React 19 still needs a CLASS component for a boundary (there is no hook for it yet): this file contains
 *     the only class in the lesson folder.
 */
import { Component, useEffect, useState } from 'react';
import { bad, box, callApi, classify, friendlyMessage } from './bankApi.jsx';

export const title = '4. Unexpected errors and error boundaries';

// A real answer, recorded from this API running with a database it could not open (see the handoff note).
// Look at what is NOT in it: no exception text, no SQL, no stack trace.
export const RECORDED_500 = {
  type: 'https://tools.ietf.org/html/rfc9110#section-15.6.1',
  title: 'An unexpected error occurred',
  status: 500,
  detail: 'Something went wrong on the server. Please try again later.',
  traceId: '00-ca44d67f524ffa5ae04122b1ad319f99-651475ace418fc97-00',
};

// Stand-ins for the network: functions with the same shape as fetch that return a prepared answer.
const answer500 = () =>
  Promise.resolve(new Response(JSON.stringify(RECORDED_500), { status: 500, headers: { 'Content-Type': 'application/problem+json' } }));
const answer502 = () => Promise.resolve(new Response('', { status: 502, headers: { 'Content-Type': 'text/plain' } }));

function FailureDemo() {
  const [shown, setShown] = useState(null);

  async function run(label, url, fetchFn) {
    const result = await callApi('GET', url, undefined, fetchFn);
    setShown({ label, result });
  }

  return (
    <div style={box}>
      <p>
        <button type="button" onClick={() => run('500 from the API (recorded)', '/api/accounts', answer500)}>
          API answers 500 (recorded answer)
        </button>{' '}
        <button type="button" onClick={() => run('502 from a proxy (recorded)', '/api/accounts', answer502)}>
          Proxy answers 502 (recorded answer)
        </button>{' '}
        <button type="button" onClick={() => run('Network down (real fetch)', 'http://localhost:59999/api/accounts')}>
          Network down (real fetch to an address nothing answers on)
        </button>
      </p>
      {shown && (
        <div data-testid="failure-shown">
          <p>
            <strong>{shown.label}</strong> - classified as <code data-testid="failure-kind">{classify(shown.result)}</code>
          </p>
          <p role="alert" style={bad} data-testid="failure-message">
            {friendlyMessage(shown.result)}
          </p>
          <p>
            Raw result: status <code>{shown.result.status}</code> (0 means no HTTP answer at all), body{' '}
            {shown.result.data ? <code>{JSON.stringify(shown.result.data)}</code> : <em>none that could be read</em>}
          </p>
        </div>
      )}
    </div>
  );
}

// ---- The error boundary. A boundary must be a class: getDerivedStateFromError / componentDidCatch have no hook version.
export class ErrorBoundary extends Component {
  state = { error: null };

  // Called while React is rendering, after a child threw: the returned object becomes the new state.
  static getDerivedStateFromError(error) {
    return { error };
  }

  // Called after the fallback is committed: the place to REPORT the error (log service, console...).
  componentDidCatch(error) {
    if (this.props.onCatch) this.props.onCatch(error);
  }

  reset = () => this.setState({ error: null });

  render() {
    if (this.state.error) return this.props.fallback(this.state.error, this.reset);
    return this.props.children;
  }
}

// `before` lets a screen put its data back before the boundary tries to render the children again.
const friendlyFallback = (before) => (error, reset) => {
  return (
    <div role="alert" style={{ ...bad, border: '1px solid #cc0000', padding: 10 }} data-testid="fallback">
      This part of the page could not be shown. The rest of the page still works.{' '}
      <button
        type="button"
        onClick={() => {
          if (before) before();
          reset();
        }}
      >
        Try again
      </button>
      <br />
      <small>For developers: {error.name} - {error.message}</small>
    </div>
  );
};

const SAMPLE = [{ accountNumber: 'ACC-1001' }, { accountNumber: 'ACC-1002' }];

// Written the way a hurried developer writes it: it ASSUMES `accounts` is an array.
function AccountsView({ accounts }) {
  return (
    <ul data-testid="accounts-view">
      {accounts.map((a) => (
        <li key={a.accountNumber}>{a.accountNumber}</li>
      ))}
    </ul>
  );
}

function RenderCrashDemo() {
  const [accounts, setAccounts] = useState(SAMPLE);
  const [caught, setCaught] = useState('nothing yet');
  return (
    <div style={box}>
      <p>
        <button type="button" onClick={() => setAccounts(RECORDED_500)}>
          Give the list the error object instead of an array
        </button>{' '}
        (what happens when code does <code>setAccounts(await response.json())</code> without checking the status first)
      </p>
      <ErrorBoundary
        fallback={friendlyFallback(() => {
          setAccounts(SAMPLE);
          setCaught('nothing yet');
        })}
        onCatch={(error) => setCaught(error.name)}
      >
        <AccountsView accounts={accounts} />
      </ErrorBoundary>
      <p data-testid="caught-in-render">Boundary caught: {caught}</p>
    </div>
  );
}

// ---- What a boundary does NOT catch
function Thrower({ report }) {
  const [, setState] = useState(0);
  return (
    <p>
      <button
        type="button"
        onClick={() => {
          throw new Error('thrown in an event handler');
        }}
      >
        Throw in an event handler
      </button>{' '}
      <button
        type="button"
        onClick={() => {
          window.setTimeout(() => {
            throw new Error('thrown in a setTimeout callback');
          }, 0);
        }}
      >
        Throw in a setTimeout callback
      </button>{' '}
      <button
        type="button"
        onClick={() => {
          Promise.reject(new Error('rejected promise nobody handled'));
        }}
      >
        Reject a promise nobody handles
      </button>{' '}
      <button
        type="button"
        onClick={() => {
          // Async code can hand its error to the nearest boundary: React runs the updater function
          // while rendering, so a throw inside it is a RENDER error.
          window.setTimeout(() => {
            setState(() => {
              throw new Error('thrown from a state updater');
            });
          }, 0);
          report('state updater scheduled');
        }}
      >
        Throw from a state updater (async)
      </button>
    </p>
  );
}

function NotCaughtDemo() {
  const [events, setEvents] = useState([]);
  const [caught, setCaught] = useState('nothing yet');

  // The browser reports errors nobody caught as an 'error' event on window (and rejected promises as
  // 'unhandledrejection'). We listen only to SHOW them; preventDefault keeps our deliberate demo errors out of the console.
  useEffect(() => {
    const onError = (event) => {
      event.preventDefault();
      setEvents((all) => [...all, `window saw an uncaught error: ${event.error ? event.error.message : event.message}`]);
    };
    const onRejection = (event) => {
      event.preventDefault();
      setEvents((all) => [...all, `window saw an unhandled rejection: ${event.reason && event.reason.message}`]);
    };
    window.addEventListener('error', onError);
    window.addEventListener('unhandledrejection', onRejection);
    return () => {
      window.removeEventListener('error', onError);
      window.removeEventListener('unhandledrejection', onRejection);
    };
  }, []);

  return (
    <div style={box}>
      <ErrorBoundary
        fallback={friendlyFallback()}
        onCatch={(error) => setCaught(error.message)}
      >
        <Thrower report={(text) => setEvents((all) => [...all, text])} />
      </ErrorBoundary>
      <p data-testid="caught-by-boundary">Boundary caught: {caught}</p>
      <p>What the page saw:</p>
      <ul data-testid="window-events">
        {events.map((e, i) => (
          <li key={i}>{e}</li>
        ))}
      </ul>
    </div>
  );
}

export default function Lesson() {
  return (
    <section>
      <h1>{title}</h1>
      <h2>1. Failures that are not the user's fault</h2>
      <FailureDemo />
      <h2>2. A render error and the error boundary</h2>
      <RenderCrashDemo />
      <h2>3. Errors a boundary does not catch</h2>
      <p>
        The first three buttons throw somewhere a boundary cannot see: the boundary line stays at "nothing yet" and only
        the browser's window notices. The last one throws from a state updater, which is part of rendering, so the
        boundary catches it and shows its fallback (press "Try again" to bring the buttons back).
      </p>
      <NotCaughtDemo />
    </section>
  );
}
