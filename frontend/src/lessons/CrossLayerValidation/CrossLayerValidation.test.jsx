// Proves every claim the CrossLayerValidation lessons make. fetch is replaced by a fake (no real network),
// returning answers copied from the real API (see the handoff note for the real-API run).
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useRef, useState } from 'react';
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';
import { callApi, classify, friendlyMessage, splitProblemErrors } from './bankApi.jsx';
import Lesson1, { checkAccountInBrowser, checkTransactionInBrowser, databaseVerdict } from './01-ThreeLayers.jsx';
import Lesson2 from './02-FieldErrorsFromProblemDetails.jsx';
import Lesson3 from './03-BusinessRuleErrors.jsx';
import Lesson4, { ErrorBoundary, RECORDED_500 } from './04-UnexpectedErrors.jsx';
import Lesson5, { GuardedDeposit, UnguardedDeposit } from './05-LoadingRetryAndDoubleSubmit.jsx';

const json = (status, body, type = 'application/problem+json') =>
  new Response(body === null ? null : JSON.stringify(body), { status, headers: body === null ? {} : { 'Content-Type': type } });
const ok = (status, body) => json(status, body, 'application/json; charset=utf-8');
const problem = (status, title, extra = {}) => ({ type: 'x', title, status, traceId: '00-test', ...extra });
const validation = (errors) => problem(400, 'One or more validation errors occurred.', { errors });

const ACCOUNTS = [
  { accountId: 1, customerName: 'Asha Rao', accountNumber: 'ACC-1001', accountType: 'Savings', balance: 5000 },
  { accountId: 2, customerName: 'Ben Carter', accountNumber: 'ACC-1002', accountType: 'Current', balance: 1200.5 },
];

// A fake API: `routes` maps "METHOD /path" to a function (body) => Response. Records every call.
let calls;
function fakeApi(routes = {}) {
  calls = [];
  const fn = vi.fn(async (url, init = {}) => {
    const method = init.method ?? 'GET';
    const body = init.body ? JSON.parse(init.body) : undefined;
    calls.push({ method, url, body });
    const handler = routes[`${method} ${url}`];
    if (handler === 'network') throw new TypeError('Failed to fetch');
    if (handler) return handler(body);
    if (method === 'GET' && url === '/api/accounts') return ok(200, ACCOUNTS);
    if (method === 'GET' && /^\/api\/accounts\/\d+$/.test(url)) return ok(200, ACCOUNTS[0]);
    if (method === 'GET' && /transactions$/.test(url)) return ok(200, [{ transactionId: 1 }]);
    return json(500, problem(500, 'no route in the test'));
  });
  vi.stubGlobal('fetch', fn);
  return fn;
}
const count = (method, url) => calls.filter((c) => c.method === method && c.url === url).length;

beforeEach(() => vi.spyOn(console, 'error').mockImplementation(() => {}));
afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe('bankApi helpers', () => {
  test('callApi never throws: a network failure becomes a result with status 0', async () => {
    const r = await callApi('GET', '/x', undefined, async () => {
      throw new TypeError('Failed to fetch');
    });
    expect(r).toMatchObject({ network: true, ok: false, status: 0, problem: null });
    expect(classify(r)).toBe('network');
  });

  test('classify decides by status code and body shape', async () => {
    const mk = (status, body, type) => callApi('POST', '/x', {}, async () => json(status, body, type));
    expect(classify(await mk(400, validation({ amount: ['m'] })))).toBe('field');
    expect(classify(await mk(422, problem(422, 'Insufficient funds')))).toBe('business');
    expect(classify(await mk(409, problem(409, 'Duplicate value')))).toBe('conflict');
    expect(classify(await mk(404, problem(404, 'Account not found')))).toBe('notFound');
    expect(classify(await mk(500, problem(500, 'An unexpected error occurred')))).toBe('server');
    expect(classify(await callApi('GET', '/x', undefined, async () => new Response('', { status: 502, headers: { 'Content-Type': 'text/plain' } })))).toBe('gateway');
    expect(classify(await callApi('GET', '/x', undefined, async () => ok(200, [])))).toBe('ok');
  });

  test('splitProblemErrors: known fields get their message arrays, other keys go to general unchanged', () => {
    const p = validation({ amount: ['a', 'b'], '$.kind': ["The value for 'kind' has the wrong type or format."], body: ['A non-empty request body is required.'] });
    expect(splitProblemErrors(p, ['kind', 'amount'])).toEqual({
      byField: { amount: ['a', 'b'] },
      general: ["The value for 'kind' has the wrong type or format.", 'A non-empty request body is required.'],
    });
  });
});

describe('01 three layers', () => {
  test('browser checks are plain functions that catch what they can see, and cannot see a duplicate', () => {
    expect(checkTransactionInBrowser({ kind: 'Deposit', amount: -5 })).toEqual(['amount must be greater than 0.']);
    expect(checkTransactionInBrowser({ kind: 'Bonus', amount: 5 })).toHaveLength(1);
    expect(checkTransactionInBrowser({ kind: 'Deposit', amount: 5 })).toEqual([]);
    expect(checkAccountInBrowser({ accountNumber: 'ACC-1001', accountType: 'Savings' })).toEqual([]);
  });

  test('asking the browser sends nothing; the raw send skips the browser check and the API decides', async () => {
    fakeApi({ 'POST /api/accounts/1/transactions': () => json(400, validation({ amount: ['amount must be between 0.01 and 1,000,000,000,000.00.'] })) });
    render(<Lesson1 />);
    const card = await screen.findByRole('region', { name: /negative amount/ });
    await userEvent.click(within(card).getByRole('button', { name: /Ask the browser/ }));
    expect(within(card).getByTestId('amount-browser')).toHaveTextContent('amount must be greater than 0.');
    expect(count('POST', '/api/accounts/1/transactions')).toBe(0); // the browser check alone makes no request
    await userEvent.click(within(card).getByRole('button', { name: /raw fetch/ }));
    expect(await screen.findByTestId('amount-api')).toHaveTextContent('HTTP 400');
    expect(count('POST', '/api/accounts/1/transactions')).toBe(1); // sent although the browser said no
    expect(screen.getByTestId('amount-api')).toHaveTextContent('not reached');
    expect(calls.find((c) => c.method === 'POST').body).toEqual({ kind: 'Deposit', amount: -5 });
  });

  test('duplicate account number: passes the browser, the database (via the API) refuses', async () => {
    fakeApi({ 'POST /api/accounts': () => json(409, problem(409, 'Duplicate value')) });
    render(<Lesson1 />);
    const card = await screen.findByRole('region', { name: /duplicate account number/ });
    await userEvent.click(within(card).getByRole('button', { name: /Ask the browser/ }));
    expect(within(card).getByTestId('duplicate-browser')).toHaveTextContent('looks fine');
    await userEvent.click(within(card).getByRole('button', { name: /raw fetch/ }));
    expect(await screen.findByTestId('duplicate-api')).toHaveTextContent('UNIQUE index');
    expect(calls.find((c) => c.method === 'POST').body).toEqual({ customerId: 1, accountNumber: 'ACC-1001', accountType: 'Savings' });
    const row = within(screen.getByTestId('summary')).getByText('C. duplicate account number').closest('tr');
    expect(row).toHaveTextContent('passed it');
    expect(row).toHaveTextContent('HTTP 409 (conflict)');
  });

  test('databaseVerdict only claims what the API answer supports', () => {
    expect(databaseVerdict(null)).toBe('-');
    expect(databaseVerdict({ status: 400 })).toMatch(/not reached/);
    expect(databaseVerdict({ status: 409, problem: { title: 'Duplicate value' } })).toMatch(/2601/);
    expect(databaseVerdict({ status: 409, problem: { title: 'Account has transactions' } })).toMatch(/no verdict/);
  });

  test('without the API the lesson shows the friendly state, not a blank page', async () => {
    fakeApi({ 'GET /api/accounts': 'network' });
    render(<Lesson1 />);
    expect(await screen.findByRole('alert')).toHaveTextContent('API not reachable — start it (see FullStackBank/README.md).');
  });

  test('a 502 from a proxy in front of a stopped API is also "not reachable"', async () => {
    fakeApi({ 'GET /api/accounts': () => new Response('', { status: 502, headers: { 'Content-Type': 'text/plain' } }) });
    render(<Lesson1 />);
    expect(await screen.findByRole('alert')).toHaveTextContent('API not reachable');
  });
});

describe('02 field errors', () => {
  test('each message appears next to its own input and is linked with aria-describedby', async () => {
    fakeApi({
      'POST /api/accounts': () =>
        json(400, validation({ accountType: ["accountType must be 'Savings' or 'Current'."], accountNumber: ["accountNumber must be 1-20 characters: letters, digits or '-'."] })),
    });
    render(<Lesson2 />);
    await screen.findByLabelText('Account number');
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    const number = await screen.findByLabelText('Account number');
    await waitFor(() => expect(number).toHaveAttribute('aria-invalid', 'true'));
    expect(number).toHaveAccessibleDescription(/accountNumber must be 1-20/);
    expect(screen.getByLabelText('Account type')).toHaveAccessibleDescription(/must be 'Savings' or 'Current'/);
    expect(screen.getByLabelText('Customer id')).not.toHaveAttribute('aria-invalid'); // no error for it
    expect(screen.getByTestId('raw-problem')).toHaveTextContent('"errors"');
  });

  test('keys that are not form fields go to the general box; blank customerId is left out of the JSON', async () => {
    fakeApi({ 'POST /api/accounts': () => json(400, validation({ '$.customerId': ["The value for 'customerId' has the wrong type or format."] })) });
    render(<Lesson2 />);
    await screen.findByLabelText('Customer id');
    await userEvent.clear(screen.getByLabelText('Customer id'));
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    expect(await screen.findByTestId('general-errors')).toHaveTextContent("The value for 'customerId' has the wrong type or format.");
    expect(calls.find((c) => c.method === 'POST').body).not.toHaveProperty('customerId');
    expect(screen.getByLabelText('Customer id')).not.toHaveAttribute('aria-invalid');
  });

  test('a valid request shows the created account, which can be deleted again', async () => {
    fakeApi({
      'POST /api/accounts': () => ok(201, { accountId: 9, customerName: 'Asha Rao', accountNumber: 'PRACTICE-1', accountType: 'Current', balance: 0 }),
      'DELETE /api/accounts/9': () => new Response(null, { status: 204 }),
    });
    render(<Lesson2 />);
    await screen.findByLabelText('Customer id');
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    expect(await screen.findByTestId('created')).toHaveTextContent('PRACTICE-1');
    await userEvent.click(screen.getByRole('button', { name: /Delete it again/ }));
    expect(await screen.findByText('Practice account deleted.')).toBeInTheDocument();
    expect(count('DELETE', '/api/accounts/9')).toBe(1);
  });
});

describe('03 business-rule errors', () => {
  test('422 is a form-level message: no input is marked invalid', async () => {
    fakeApi({
      'POST /api/accounts/1/transactions': () =>
        json(422, problem(422, 'Insufficient funds', { detail: 'Account 1 has a balance of 5000.00; a withdrawal of 5001.00 is not allowed.' })),
    });
    render(<Lesson3 />);
    await userEvent.click(await screen.findByRole('button', { name: /withdraw 5001.00/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Send' }));
    const message = await screen.findByTestId('form-message');
    expect(message).toHaveTextContent('Business rule (422)');
    expect(message).toHaveTextContent('Insufficient funds');
    expect(screen.getByLabelText('Amount')).not.toHaveAttribute('aria-invalid');
    expect(calls.find((c) => c.method === 'POST').body).toEqual({ kind: 'Withdrawal', amount: 5001 });
  });

  test('400 on the same form IS shown next to the input, and there is no form-level box', async () => {
    fakeApi({ 'POST /api/accounts/1/transactions': () => json(400, validation({ amount: ['amount must be between 0.01 and 1,000,000,000,000.00.'] })) });
    render(<Lesson3 />);
    await userEvent.click(await screen.findByRole('button', { name: 'withdraw -5' }));
    await userEvent.click(screen.getByRole('button', { name: 'Send' }));
    await waitFor(() => expect(screen.getByLabelText('Amount')).toHaveAttribute('aria-invalid', 'true'));
    expect(screen.getByLabelText('Amount')).toHaveAccessibleDescription(/amount must be between/);
    expect(screen.queryByTestId('form-message')).toBeNull();
  });

  test('409 duplicate account number is a form-level conflict message', async () => {
    fakeApi({ 'POST /api/accounts': () => json(409, problem(409, 'Duplicate value', { detail: 'A record with the same unique value already exists.' })) });
    render(<Lesson3 />);
    await userEvent.click(await screen.findByRole('button', { name: /Open an account with the number ACC-1001/ }));
    expect(await screen.findByTestId('form-message')).toHaveTextContent('Conflict (409)');
  });

  test('delete-with-history is sent only when the account has transactions', async () => {
    fakeApi({ 'DELETE /api/accounts/1': () => json(409, problem(409, 'Account has transactions', { detail: 'Account 1 has transaction history and cannot be deleted.' })) });
    render(<Lesson3 />);
    await userEvent.click(await screen.findByRole('button', { name: /Delete account 1/ }));
    expect(await screen.findByTestId('form-message')).toHaveTextContent('Account has transactions');
    // now an account without history: nothing is deleted, nothing is sent
    fakeApi({ 'GET /api/accounts/1/transactions': () => ok(200, []) });
    render(<Lesson3 />);
    const buttons = await screen.findAllByRole('button', { name: /Delete account 1/ });
    await userEvent.click(buttons[buttons.length - 1]);
    expect(await screen.findByText(/has no transactions, so a DELETE would succeed/)).toBeInTheDocument();
    expect(count('DELETE', '/api/accounts/1')).toBe(0);
  });
});

describe('04 unexpected errors', () => {
  test('the 500 answer shows a generic sentence and the traceId; nothing from the body beyond that', () => {
    const result = { network: false, ok: false, status: 500, data: RECORDED_500, problem: RECORDED_500 };
    expect(classify(result)).toBe('server');
    const text = friendlyMessage(result);
    expect(text).toContain(RECORDED_500.traceId);
    expect(text).not.toMatch(/SqlException|stack|SELECT|Exception/i);
    expect(Object.keys(RECORDED_500).sort()).toEqual(['detail', 'status', 'title', 'traceId', 'type']);
  });

  test('the three failure buttons classify server / gateway / network', async () => {
    fakeApi({ 'GET http://localhost:59999/api/accounts': 'network' });
    render(<Lesson4 />);
    await userEvent.click(screen.getByRole('button', { name: /API answers 500/ }));
    await waitFor(() => expect(screen.getByTestId('failure-kind')).toHaveTextContent('server'));
    await userEvent.click(screen.getByRole('button', { name: /Proxy answers 502/ }));
    await waitFor(() => expect(screen.getByTestId('failure-kind')).toHaveTextContent('gateway'));
    await userEvent.click(screen.getByRole('button', { name: /Network down/ }));
    await waitFor(() => expect(screen.getByTestId('failure-kind')).toHaveTextContent('network'));
    expect(screen.getByTestId('failure-shown')).toHaveTextContent('status 0');
  });

  test('an error boundary catches a render error, shows the fallback, and can recover', async () => {
    render(<Lesson4 />);
    expect(screen.getByTestId('accounts-view')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: /Give the list the error object/ }));
    expect(screen.getByTestId('fallback')).toHaveTextContent('could not be shown');
    expect(screen.queryByTestId('accounts-view')).toBeNull();
    expect(screen.getByTestId('caught-in-render')).toHaveTextContent('TypeError');
    expect(screen.getByRole('heading', { name: /Unexpected errors/ })).toBeInTheDocument(); // the rest of the page lives on
    await userEvent.click(within(screen.getByTestId('caught-in-render').parentElement).getByRole('button', { name: 'Try again' }));
    expect(screen.getByTestId('accounts-view')).toBeInTheDocument();
  });

  // In a browser an exception in a timer callback is reported as an 'error' event on window. Node (which runs these
  // tests) reports it differently, so this helper makes timers behave like the browser's (checked for real in Chromium).
  function browserLikeTimers() {
    const original = window.setTimeout;
    vi.spyOn(window, 'setTimeout').mockImplementation((fn, ms) =>
      original(() => {
        try {
          fn();
        } catch (error) {
          window.dispatchEvent(new ErrorEvent('error', { error, message: error.message, cancelable: true }));
        }
      }, ms),
    );
  }

  test('handler error and setTimeout error reach window, the boundary stays quiet', async () => {
    browserLikeTimers();
    render(<Lesson4 />);
    await userEvent.click(screen.getByRole('button', { name: 'Throw in an event handler' }));
    await userEvent.click(screen.getByRole('button', { name: 'Throw in a setTimeout callback' }));
    await waitFor(() => expect(screen.getByTestId('window-events')).toHaveTextContent('thrown in a setTimeout callback'));
    expect(screen.getByTestId('window-events')).toHaveTextContent('thrown in an event handler');
    expect(screen.getByTestId('caught-by-boundary')).toHaveTextContent('nothing yet');
    expect(screen.queryByTestId('fallback')).toBeNull();
  });

  test('throwing from a state updater (async) IS caught by the boundary', async () => {
    browserLikeTimers();
    render(<Lesson4 />);
    await userEvent.click(screen.getByRole('button', { name: /Throw from a state updater/ }));
    await waitFor(() => expect(screen.getByTestId('caught-by-boundary')).toHaveTextContent('thrown from a state updater'));
    expect(screen.getByTestId('fallback')).toBeInTheDocument();
  });

  test('ErrorBoundary alone: children render normally when nothing throws', () => {
    render(<ErrorBoundary fallback={() => <p>fallback</p>}><p>fine</p></ErrorBoundary>);
    expect(screen.getByText('fine')).toBeInTheDocument();
  });
});

describe('05 loading, retry and double submit', () => {
  const deposit201 = () => ok(201, { transactionId: 5, kind: 'Deposit', amount: 1, createdAt: '2026-10-12T00:00:00Z' });
  const slow = () => new Promise((resolve) => setTimeout(() => resolve(deposit201()), 30));
  const POST = 'POST /api/accounts/1/transactions';

  test('WITHOUT a guard, two clicks 100 ms apart on a slow network send two deposits', async () => {
    fakeApi({ [POST]: slow });
    render(<UnguardedDeposit accountId={1} onDone={() => {}} latencyMs={300} />);
    await userEvent.click(screen.getByRole('button', { name: 'Click twice, 100 ms apart' }));
    await waitFor(() => expect(screen.getByTestId('unguarded-answers')).toHaveTextContent('HTTP 201, HTTP 201'), { timeout: 2000 });
    expect(count('POST', '/api/accounts/1/transactions')).toBe(2);
  });

  test('the same two attempts with the guard: the second is ignored while the first is pending', async () => {
    fakeApi({ [POST]: slow });
    render(<GuardedDeposit accountId={1} onDone={() => {}} latencyMs={300} />);
    await userEvent.click(screen.getByRole('button', { name: 'Call the handler twice, 100 ms apart' }));
    await waitFor(() => expect(screen.getByTestId('guarded-answers')).toHaveTextContent('HTTP 201'), { timeout: 2000 });
    await new Promise((r) => setTimeout(r, 200));
    expect(count('POST', '/api/accounts/1/transactions')).toBe(1);
  });

  test('two overlapping deposits: the API answers one of them 409 (what the real API did), the UI shows both answers', async () => {
    let n = 0;
    fakeApi({ [POST]: () => (++n === 1 ? deposit201() : json(409, problem(409, 'The record was changed by someone else'))) });
    render(<UnguardedDeposit accountId={1} onDone={() => {}} />);
    await userEvent.click(screen.getByRole('button', { name: 'Both at the same instant' }));
    await waitFor(() => expect(screen.getByTestId('unguarded-answers')).toHaveTextContent('HTTP 201, HTTP 409'));
  });

  test('WITH the guard, a real double click sends one deposit: the button is disabled and says Sending...', async () => {
    fakeApi({ [POST]: slow });
    render(<GuardedDeposit accountId={1} onDone={() => {}} />);
    const button = screen.getByRole('button', { name: 'Deposit 1.00' });
    await userEvent.dblClick(button);
    expect(screen.getByRole('button', { name: 'Sending...' })).toBeDisabled();
    await waitFor(() => expect(screen.getByTestId('guarded-answers')).toHaveTextContent('HTTP 201'));
    expect(count('POST', '/api/accounts/1/transactions')).toBe(1);
    expect(screen.getByRole('button', { name: 'Deposit 1.00' })).toBeEnabled(); // re-enabled afterwards
  });

  test('calling the guarded handler twice in the same event still sends one (the ref)', async () => {
    fakeApi({ [POST]: slow });
    render(<GuardedDeposit accountId={1} onDone={() => {}} />);
    await userEvent.click(screen.getByRole('button', { name: 'Call the handler twice in a row' }));
    await waitFor(() => expect(screen.getByTestId('guarded-answers')).toHaveTextContent('HTTP 201'));
    expect(count('POST', '/api/accounts/1/transactions')).toBe(1);
  });

  test('a guard made of state alone is NOT enough when the handler is called twice in one event', async () => {
    fakeApi({ [POST]: slow });
    function StateOnly() {
      const [pending, setPending] = useState(false);
      async function deposit() {
        if (pending) return; // reads the state of THIS render: still false for the second call
        setPending(true);
        await callApi('POST', '/api/accounts/1/transactions', { kind: 'Deposit', amount: 1 });
        setPending(false);
      }
      return <button disabled={pending} onClick={() => { deposit(); deposit(); }}>go</button>;
    }
    render(<StateOnly />);
    await userEvent.click(screen.getByRole('button', { name: 'go' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'go' })).toBeEnabled());
    expect(count('POST', '/api/accounts/1/transactions')).toBe(2);
  });

  test('after a failure the button is enabled again, says Try again, and a retry can succeed', async () => {
    fakeApi({ [POST]: deposit201 });
    render(<GuardedDeposit accountId={1} onDone={() => {}} />);
    await userEvent.click(screen.getByLabelText(/make the next attempt fail/));
    // the failing attempt goes to http://localhost:59999: make that fake fail like an unreachable address
    const original = globalThis.fetch;
    vi.stubGlobal('fetch', vi.fn(async (url, init) => {
      if (url.startsWith('http://localhost:59999')) throw new TypeError('Failed to fetch');
      return original(url, init);
    }));
    await userEvent.click(screen.getByRole('button', { name: 'Deposit 1.00' }));
    expect(await screen.findByTestId('guarded-failure')).toHaveTextContent('Could not reach the server');
    expect(screen.getByTestId('guarded-failure')).toHaveTextContent('may or may not');
    const retry = screen.getByRole('button', { name: 'Try again' });
    expect(retry).toBeEnabled();
    await userEvent.click(retry);
    await waitFor(() => expect(screen.getByTestId('guarded-answers')).toHaveTextContent('no answer, HTTP 201'));
    expect(screen.queryByTestId('guarded-failure')).toBeNull();
  });

  test('whole lesson: shows balance and count, refreshes after a deposit', async () => {
    let n = 1;
    fakeApi({ [POST]: () => { n += 1; return deposit201(); }, 'GET /api/accounts/1/transactions': () => ok(200, Array.from({ length: n }, (_, i) => ({ transactionId: i }))) });
    render(<Lesson5 />);
    await waitFor(() => expect(screen.getByTestId('info')).toHaveTextContent('transactions on this account: 1'));
    const unguarded = screen.getByRole('heading', { name: 'Without a guard' }).parentElement;
    await userEvent.click(within(unguarded).getByRole('button', { name: 'Deposit 1.00' }));
    await waitFor(() => expect(screen.getByTestId('info')).toHaveTextContent('transactions on this account: 2'));
  });
});
