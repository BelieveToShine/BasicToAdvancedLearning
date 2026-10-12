// Tests for topic 25 (calling the Web API from React). No real network: fetch is replaced by vi.fn that
// returns real Response objects (so status, ok, headers and json() behave like the browser's).
// The real-API check (headless Chromium + running FullStackBank API) is documented in the handoff note.
import '@testing-library/jest-dom/vitest';
import { render, screen, within, waitFor, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';

import Lesson1, { AccountsList } from './01-GetFromTheApi.jsx';
import Lesson2, { AccountsPage } from './02-PostAndRefresh.jsx';
import Lesson3, { FieldErrorForm, StatusLab, STATUS_GUIDE, guideFor, call, readBody as readBody3 } from './03-StatusCodesInTheUi.jsx';
import Lesson4, { AccountsPanel, OpenAccountPanel, ErrorView } from './04-ApiModule.jsx';
import Lesson5, { DirectToTheApi, LocationHeader, OriginTable, ThroughTheProxy, sameOrigin } from './05-ProxyAndCors.jsx';
import { api, ApiError, pathOf, request } from './api.js';

const ACCOUNTS = [
  { accountId: 1, customerName: 'Asha Rao', accountNumber: 'ACC-1001', accountType: 'Savings', balance: 5000 },
  { accountId: 2, customerName: 'Ben Carter', accountNumber: 'ACC-1002', accountType: 'Current', balance: 1200.5 },
];
const json = (body, status = 200, extra = {}) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json; charset=utf-8', ...extra } });
const problem = (status, title, extra = {}) =>
  new Response(JSON.stringify({ type: 'x', title, status, traceId: '00-abc', ...extra }), { status, headers: { 'Content-Type': 'application/problem+json; charset=utf-8' } });
const created = (account, location) => json(account, 201, { Location: location });

let fetchMock;
beforeEach(() => {
  fetchMock = vi.fn();
  vi.stubGlobal('fetch', fetchMock);
});
afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

const bodyOf = (call) => JSON.parse(call[1].body);

describe('Lesson 1 — GET from the API', () => {
  test('shows loading, then the table, plus what step 1 (headers) gave', async () => {
    fetchMock.mockResolvedValue(json(ACCOUNTS));
    render(<AccountsList />);
    expect(screen.getByText('Loading accounts…')).toBeInTheDocument();
    expect(await screen.findByText('Ben Carter')).toBeInTheDocument();
    expect(screen.getByText('1200.50')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledWith('/api/accounts');
    expect(screen.getByText(/Step 1 gave/)).toHaveTextContent('status 200, ok true, Content-Type application/json; charset=utf-8');
  });

  test('a rejected fetch shows the friendly "API not reachable" state', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    render(<AccountsList />);
    expect(await screen.findByRole('alert')).toHaveTextContent('API not reachable — start it (see FullStackBank/README.md)');
  });

  test('a plain-text 502 (what the dev proxy sends when the API is down) is also "not reachable"', async () => {
    fetchMock.mockResolvedValue(new Response('', { status: 502, headers: { 'Content-Type': 'text/plain' } }));
    render(<AccountsList />);
    expect(await screen.findByRole('alert')).toHaveTextContent('API not reachable');
  });

  test('fetch RESOLVES for a 500 problem body: it is an answer, shown as "server answered 500", not as unreachable', async () => {
    fetchMock.mockResolvedValue(problem(500, 'An unexpected error occurred'));
    render(<AccountsList />);
    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('The server answered 500');
    expect(alert).not.toHaveTextContent('not reachable');
  });

  test('Reload sends a second request; the lesson page renders', async () => {
    fetchMock.mockImplementation(async () => json(ACCOUNTS));
    render(<Lesson1 />);
    await screen.findByText('Asha Rao');
    await userEvent.click(screen.getByRole('button', { name: 'Reload' }));
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
  });
});

describe('Lesson 2 — POST and refresh', () => {
  const NEW = { accountId: 7, customerName: 'Asha Rao', accountNumber: 'ACC-2001', accountType: 'Current', balance: 0 };
  function routes() {
    let list = [...ACCOUNTS];
    fetchMock.mockImplementation(async (url, init = {}) => {
      if (init.method === 'POST') {
        const hasType = init.headers && init.headers['Content-Type'] === 'application/json';
        if (!hasType) return problem(415, 'Unsupported Media Type');
        list = [...list, NEW];
        return created(NEW, 'http://localhost:5080/api/accounts/7');
      }
      return json(list);
    });
  }

  test('POST sends method, JSON Content-Type and a JSON body with customerId as a NUMBER; the 201 and Location are shown', async () => {
    routes();
    render(<AccountsPage />);
    await screen.findByText(/ACC-1002/);
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    const post = fetchMock.mock.calls.find((c) => c[1]?.method === 'POST');
    expect(post[0]).toBe('/api/accounts');
    expect(post[1].headers).toEqual({ 'Content-Type': 'application/json' });
    expect(bodyOf(post)).toEqual({ customerId: 1, accountNumber: 'ACC-2001', accountType: 'Current' });
    const status = await screen.findByRole('status');
    expect(status).toHaveTextContent('Status: 201');
    expect(status).toHaveTextContent('Created account ACC-2001 (id 7), balance 0.00.');
    expect(status).toHaveTextContent('Location header: http://localhost:5080/api/accounts/7');
  });

  test('strategy A adds the returned account to state with NO extra GET', async () => {
    routes();
    render(<AccountsPage />);
    await screen.findByText(/ACC-1002/);
    expect(screen.getByTestId('get-count')).toHaveTextContent('1');
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    expect(await screen.findByText(/ACC-2001 — Asha Rao/)).toBeInTheDocument();
    expect(screen.getByTestId('get-count')).toHaveTextContent('1');
  });

  test('strategy B fetches the list again (a second GET)', async () => {
    routes();
    render(<AccountsPage />);
    await screen.findByText(/ACC-1002/);
    await userEvent.click(screen.getByLabelText(/B\) fetching/));
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    expect(await screen.findByText(/ACC-2001 — Asha Rao/)).toBeInTheDocument();
    expect(screen.getByTestId('get-count')).toHaveTextContent('2');
  });

  test('without the Content-Type header the request has none and the API answers 415', async () => {
    routes();
    render(<AccountsPage />);
    await screen.findByText(/ACC-1002/);
    await userEvent.click(screen.getByRole('button', { name: /without the Content-Type/ }));
    expect(await screen.findByRole('status')).toHaveTextContent('Status: 415');
    const post = fetchMock.mock.calls.find((c) => c[1]?.method === 'POST');
    expect(post[1].headers).toEqual({});
  });

  test('the buttons are disabled while the POST is in flight (no double submit)', async () => {
    let release;
    fetchMock.mockImplementation((url, init = {}) =>
      init.method === 'POST' ? new Promise((r) => (release = () => r(created(NEW, 'http://x/api/accounts/7')))) : Promise.resolve(json(ACCOUNTS)),
    );
    render(<AccountsPage />);
    await screen.findByText(/ACC-1002/);
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    expect(screen.getByRole('button', { name: 'Sending…' })).toBeDisabled();
    release();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Open account' })).toBeEnabled());
  });

  test('a 409 shows the problem title and detail, and the list is unchanged', async () => {
    fetchMock.mockImplementation(async (url, init = {}) => (init.method === 'POST' ? problem(409, 'Duplicate value', { detail: 'A record with the same unique value already exists.' }) : json(ACCOUNTS)));
    render(<AccountsPage />);
    await screen.findByText(/ACC-1002/);
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    expect(await screen.findByRole('status')).toHaveTextContent('Duplicate value: A record with the same unique value already exists.');
    expect(screen.getAllByRole('listitem')).toHaveLength(2);
  });

  test('API stopped (dev proxy answers 502 text/plain): list and POST both show the friendly message', async () => {
    fetchMock.mockImplementation(async () => new Response('', { status: 502, headers: { 'Content-Type': 'text/plain' } }));
    render(<AccountsPage />);
    expect(await screen.findByRole('alert')).toHaveTextContent('API not reachable');
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    expect(await screen.findByRole('status')).toHaveTextContent('Status: 502 — API not reachable');
  });

  test('the lesson page renders', async () => {
    fetchMock.mockResolvedValue(json(ACCOUNTS));
    render(<Lesson2 />);
    expect(await screen.findByText(/ACC-1001/)).toBeInTheDocument();
  });
});

describe('Lesson 3 — Status codes in the UI', () => {
  test('response.ok is true exactly for 2xx', () => {
    for (const s of [200, 201, 204]) expect(new Response(null, { status: s }).ok).toBe(true);
    for (const s of [400, 404, 409, 422, 500, 502]) expect(new Response(null, { status: s }).ok).toBe(false);
  });

  test('a 204 has no body: response.json() throws, our readBody returns null', async () => {
    await expect(new Response(null, { status: 204 }).json()).rejects.toThrow();
    expect(await readBody3(new Response(null, { status: 204 }))).toBeNull();
  });

  test('response.json() parses an application/problem+json body, and readBody does too', async () => {
    const make = () => problem(422, 'Insufficient funds', { detail: 'Account 4 has a balance of 0.00' });
    const viaJson = await make().json();
    expect(viaJson.title).toBe('Insufficient funds');
    const viaRead = await readBody3(make());
    expect(viaRead.status).toBe(422);
    expect(make().headers.get('Content-Type')).toContain('problem+json');
  });

  test('plain text from a proxy is kept as text, not parsed', async () => {
    expect(await readBody3(new Response('Bad Gateway', { status: 502, headers: { 'Content-Type': 'text/plain' } }))).toBe('Bad Gateway');
  });

  test('the guide has a row for each status the API uses and guideFor falls back sensibly', () => {
    for (const code of ['200', '201', '204', '400', '404', '409', '422', '500', 'none']) expect(STATUS_GUIDE.some((g) => g.code === code)).toBe(true);
    expect(guideFor(null).code).toBe('none');
    expect(guideFor(503).code).toBe('500');
    expect(guideFor(409).meaning).toBe('Conflict');
  });

  test('lab 422: creates a temporary account, the withdrawal is refused, the account is deleted again', async () => {
    fetchMock
      .mockResolvedValueOnce(created({ accountId: 9, accountNumber: 'LAB-X', customerName: 'Asha Rao' }, 'http://h/api/accounts/9'))
      .mockResolvedValueOnce(problem(422, 'Insufficient funds', { detail: 'balance 0.00' }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));
    render(<StatusLab />);
    await userEvent.click(screen.getByRole('button', { name: '422' }));
    expect(await screen.findByText(/Insufficient funds — balance 0.00/)).toBeInTheDocument();
    const calls = fetchMock.mock.calls;
    expect(calls[1][0]).toBe('/api/accounts/9/transactions');
    expect(bodyOf(calls[1])).toEqual({ kind: 'Withdrawal', amount: 10 });
    expect(calls[2]).toEqual(['/api/accounts/9', expect.objectContaining({ method: 'DELETE' })]);
    expect(screen.getByRole('status')).toHaveTextContent('business rule refused it');
  });

  test('lab 204 shows ok true and no body for the DELETE', async () => {
    fetchMock
      .mockResolvedValueOnce(created({ accountId: 9, accountNumber: 'LAB-X' }, 'http://h/api/accounts/9'))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));
    render(<StatusLab />);
    await userEvent.click(screen.getByRole('button', { name: '204' }));
    expect(await screen.findByText('(no body)')).toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent('Do not call response.json()');
  });

  test('lab "No answer": fetch rejects and there is no status', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    render(<StatusLab />);
    await userEvent.click(screen.getByRole('button', { name: 'No answer' }));
    expect(await screen.findByText(/fetch rejected with TypeError — there is no status/)).toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent('Retry');
  });

  test('a 400 puts each message next to its own input; an unknown key ($) is still shown', async () => {
    fetchMock.mockResolvedValue(
      problem(400, 'One or more validation errors occurred.', { errors: { accountNumber: ['accountNumber is required.'], accountType: ["accountType must be 'Savings' or 'Current'."], $: ['bad json'] } }),
    );
    render(<FieldErrorForm />);
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    const numberField = screen.getByLabelText('accountNumber').closest('p');
    expect(within(numberField).getByRole('alert')).toHaveTextContent('accountNumber is required.');
    expect(within(screen.getByLabelText('accountType').closest('p')).getByRole('alert')).toHaveTextContent("must be 'Savings' or 'Current'");
    expect(screen.getByLabelText('customerId').closest('p').querySelector('[role=alert]')).toBeNull();
    expect(screen.getByText(/\$: bad json/)).toBeInTheDocument();
  });

  test('a 409 in the form gives one message for the whole form', async () => {
    fetchMock.mockResolvedValue(problem(409, 'Duplicate value', { detail: 'already exists' }));
    render(<FieldErrorForm />);
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    expect(await screen.findByRole('status')).toHaveTextContent('409: Duplicate value — already exists');
  });

  test('the lesson page renders the guide and the lab buttons', () => {
    render(<Lesson3 />);
    expect(screen.getByText('Unprocessable Content (older name: Unprocessable Entity)')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'No answer' })).toBeInTheDocument();
  });
});

describe('Lesson 4 — the API module', () => {
  test('request: Accept always, Content-Type only with a body, body is JSON text, base path /api', async () => {
    fetchMock.mockResolvedValue(json([]));
    await api.listAccounts();
    expect(fetchMock).toHaveBeenLastCalledWith('/api/accounts', { method: 'GET', headers: { Accept: 'application/json' }, body: undefined });
    fetchMock.mockResolvedValue(created(ACCOUNTS[0], 'http://h/api/accounts/1'));
    await api.createAccount({ customerId: 1, accountNumber: 'A', accountType: 'Current' });
    const [url, init] = fetchMock.mock.lastCall;
    expect(url).toBe('/api/accounts');
    expect(init.headers).toEqual({ Accept: 'application/json', 'Content-Type': 'application/json' });
    expect(init.body).toBe('{"customerId":1,"accountNumber":"A","accountType":"Current"}');
  });

  test('createAccount returns the account and the Location; 204 delete resolves with nothing', async () => {
    fetchMock.mockResolvedValue(created(ACCOUNTS[0], 'http://h/api/accounts/1'));
    expect(await api.createAccount({})).toEqual({ account: ACCOUNTS[0], location: 'http://h/api/accounts/1' });
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }));
    expect(await api.deleteAccount(1)).toBeUndefined();
  });

  test.each([
    [problem(400, 'One or more validation errors occurred.', { errors: { amount: ['amount must be greater than 0.'] } }), 'validation', 400],
    [problem(404, 'Account not found', { detail: 'There is no account with id 9.' }), 'notFound', 404],
    [problem(409, 'Duplicate value'), 'conflict', 409],
    [problem(422, 'Insufficient funds'), 'rule', 422],
    [problem(500, 'An unexpected error occurred'), 'server', 500],
    [new Response('', { status: 502, headers: { 'Content-Type': 'text/plain' } }), 'network', 502],
  ])('every failure becomes an ApiError with the same shape (%#)', async (response, kind, status) => {
    fetchMock.mockResolvedValue(response);
    const err = await request('GET', '/x').catch((e) => e);
    expect(err).toBeInstanceOf(ApiError);
    expect(err).toBeInstanceOf(Error);
    expect(err.kind).toBe(kind);
    expect(err.status).toBe(status);
  });

  test('ApiError carries title, detail, fieldErrors and traceId from the problem body', async () => {
    fetchMock.mockResolvedValue(problem(400, 'Bad', { detail: 'd', errors: { accountNumber: ['required'] } }));
    const err = await api.getAccount(1).catch((e) => e);
    expect(err).toMatchObject({ title: 'Bad', detail: 'd', fieldErrors: { accountNumber: ['required'] }, traceId: '00-abc' });
  });

  test('a rejected fetch becomes ApiError kind network with status 0', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    const err = await api.listAccounts().catch((e) => e);
    expect(err).toMatchObject({ kind: 'network', status: 0, title: 'API not reachable' });
  });

  test('pathOf keeps only the path of a Location URL', () => {
    expect(pathOf('http://localhost:5080/api/accounts/7')).toBe('/api/accounts/7');
  });

  test('AccountsPanel: delete answered 409 shows the message through ErrorView', async () => {
    fetchMock.mockImplementation(async (url, init = {}) => (init.method === 'DELETE' ? problem(409, 'Account has transactions', { detail: 'It has history.' }) : json(ACCOUNTS)));
    render(<AccountsPanel />);
    await screen.findByText('ACC-1001');
    await userEvent.click(screen.getByRole('button', { name: 'Delete ACC-1001' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Account has transactions — It has history. (status 409)');
  });

  test('AccountsPanel: delete answered 204 reloads the list', async () => {
    let list = [...ACCOUNTS];
    fetchMock.mockImplementation(async (url, init = {}) => {
      if (init.method === 'DELETE') {
        list = list.slice(1);
        return new Response(null, { status: 204 });
      }
      return json(list);
    });
    render(<AccountsPanel />);
    await screen.findByText('ACC-1001');
    await userEvent.click(screen.getByRole('button', { name: 'Delete ACC-1001' }));
    await waitFor(() => expect(screen.queryByText('ACC-1001')).toBeNull());
    expect(screen.getByText('ACC-1002')).toBeInTheDocument();
  });

  test('AccountsPanel: network failure shows the friendly message', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    render(<AccountsPanel />);
    expect(await screen.findByRole('alert')).toHaveTextContent('API not reachable — start it (see FullStackBank/README.md)');
  });

  test('OpenAccountPanel: 400 lists field errors; 201 reports the new account', async () => {
    fetchMock.mockResolvedValueOnce(problem(400, 'Invalid', { errors: { accountNumber: ['accountNumber is required.'] } }));
    const onCreated = vi.fn();
    render(<OpenAccountPanel onCreated={onCreated} />);
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('accountNumber: accountNumber is required.');
    fetchMock.mockResolvedValueOnce(created({ accountId: 5, accountNumber: 'ACC-9', balance: 0 }, 'http://h/api/accounts/5'));
    await userEvent.type(screen.getByLabelText('Account number'), 'ACC-9');
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    expect(await screen.findByRole('status')).toHaveTextContent('Created ACC-9 (id 5).');
    expect(onCreated).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole('alert')).toBeNull();
  });

  test('an error that is not an ApiError is labelled as a front-end bug, not as an API message', () => {
    render(<ErrorView error={new TypeError('x is undefined')} />);
    expect(screen.getByRole('alert')).toHaveTextContent('Unexpected error in the front end: x is undefined');
  });

  test('the lesson page shows the real source of api.js', async () => {
    fetchMock.mockResolvedValue(json(ACCOUNTS));
    render(<Lesson4 />);
    await screen.findByText('ACC-1001');
    expect(screen.getByText(/export class ApiError extends Error/)).toBeInTheDocument();
  });
});

describe('Lesson 5 — proxy and CORS', () => {
  test('same origin means same scheme, host AND port', () => {
    const page = 'http://localhost:5173/';
    expect(sameOrigin(page, 'http://localhost:5173/api/accounts')).toBe(true);
    expect(sameOrigin(page, 'http://localhost:5080/api/accounts')).toBe(false);
    expect(sameOrigin(page, 'https://localhost:5173/api/accounts')).toBe(false);
    expect(sameOrigin(page, 'http://127.0.0.1:5173/api/accounts')).toBe(false);
  });

  test('the origin table marks exactly the first row as same origin', () => {
    render(<OriginTable />);
    expect(screen.getAllByText('same origin')).toHaveLength(1);
    expect(screen.getAllByText('different origin')).toHaveLength(3);
  });

  test('a relative URL resolves against the page origin', () => {
    expect(new URL('/api/accounts', window.location.href).origin).toBe(window.location.origin);
  });

  test('ThroughTheProxy requests the relative URL and shows the URL the browser used', async () => {
    const res = json(ACCOUNTS);
    Object.defineProperty(res, 'url', { value: `${window.location.origin}/api/accounts` });
    fetchMock.mockResolvedValue(res);
    render(<ThroughTheProxy />);
    await userEvent.click(screen.getByRole('button', { name: 'GET /api/accounts' }));
    expect(fetchMock).toHaveBeenCalledWith('/api/accounts');
    expect(await screen.findByRole('status')).toHaveTextContent(`2 accounts. The browser requested: ${window.location.origin}/api/accounts`);
  });

  test('DirectToTheApi: the call goes to the absolute API address; a rejection shows the error name and both possible causes', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    render(<DirectToTheApi />);
    await userEvent.click(screen.getByRole('button', { name: 'Direct GET' }));
    expect(fetchMock.mock.calls[0][0]).toBe('http://localhost:5080/api/accounts');
    expect(await screen.findByRole('status')).toHaveTextContent('GET: fetch rejected (TypeError). Either the browser blocked the answer');
    await userEvent.click(screen.getByRole('button', { name: 'Direct POST (JSON)' }));
    const post = fetchMock.mock.calls[1];
    expect(post[1].method).toBe('POST');
    expect(post[1].headers['Content-Type']).toBe('application/json'); // the header that makes the browser send a preflight
  });

  test('DirectToTheApi: if the answer IS readable (CORS allowed) the page shows the status', async () => {
    fetchMock.mockResolvedValue(json(ACCOUNTS));
    render(<DirectToTheApi />);
    await userEvent.click(screen.getByRole('button', { name: 'Direct GET' }));
    expect(await screen.findByRole('status')).toHaveTextContent('allowed to read the answer: status 200');
  });

  test('LocationHeader: Location origin differs from the page origin; the tidy-up DELETE uses only the path', async () => {
    fetchMock
      .mockResolvedValueOnce(created({ accountId: 7, accountNumber: 'LOC-1' }, 'http://localhost:5080/api/accounts/7'))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));
    render(<LocationHeader />);
    await userEvent.click(screen.getByRole('button', { name: /inspect Location/ }));
    expect(await screen.findByText('different origin')).toBeInTheDocument();
    expect(screen.getByText('/api/accounts/7')).toBeInTheDocument();
    expect(fetchMock.mock.calls[1][0]).toBe('/api/accounts/7');
  });

  test('LocationHeader: API down shows the friendly error', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    render(<LocationHeader />);
    await userEvent.click(screen.getByRole('button', { name: /inspect Location/ }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Is the API running?');
  });

  test('the lesson page renders all sections', () => {
    render(<Lesson5 />);
    expect(screen.getByText('Same origin or not?')).toBeInTheDocument();
    expect(screen.getByText(/Directly to the API's own address/)).toBeInTheDocument();
  });
});

describe('Review fixes — API stopped (the dev proxy answers 502 text/plain)', () => {
  const down = () => new Response('', { status: 502, headers: { 'Content-Type': 'text/plain' } });

  test('lesson 5 ThroughTheProxy: 502 is NOT shown as success with 0 accounts', async () => {
    fetchMock.mockResolvedValue(down());
    render(<ThroughTheProxy />);
    await userEvent.click(screen.getByRole('button', { name: 'GET /api/accounts' }));
    const status = await screen.findByRole('status');
    expect(status).toHaveTextContent('Status 502: API not reachable — start it (see FullStackBank/README.md).');
    expect(status).not.toHaveTextContent('0 accounts');
  });

  test('lesson 5 ThroughTheProxy: another non-ok status is reported as just "Status N"', async () => {
    fetchMock.mockResolvedValue(problem(500, 'An unexpected error occurred'));
    render(<ThroughTheProxy />);
    await userEvent.click(screen.getByRole('button', { name: 'GET /api/accounts' }));
    const status = await screen.findByRole('status');
    expect(status).toHaveTextContent('Status 500.');
    expect(status).not.toHaveTextContent('accounts,');
  });

  test('lesson 5 DirectToTheApi: the rejection text does not claim a cause it cannot know', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));
    render(<DirectToTheApi />);
    await userEvent.click(screen.getByRole('button', { name: 'Direct GET' }));
    const status = await screen.findByRole('status');
    expect(status).toHaveTextContent('Either the browser blocked the answer');
    expect(status).toHaveTextContent('or nothing is listening at that address');
  });

  test('lesson 3 call(): a plain-text 502 counts as "no answer"; a problem+json 502 does not', async () => {
    fetchMock.mockResolvedValueOnce(down());
    const e = await call('GET', '/api/accounts');
    expect(e.status).toBeNull();
    expect(e.unreachable).toBe(true);
    fetchMock.mockResolvedValueOnce(problem(502, 'Bad gateway from the API'));
    expect((await call('GET', '/api/accounts')).status).toBe(502);
  });

  test('lesson 3 guideFor: 502-504 without a problem body maps to the "none" row, 500 stays 500', () => {
    expect(guideFor(502, 'text/plain').code).toBe('none');
    expect(guideFor(503, 'text/plain').code).toBe('none');
    expect(guideFor(500, 'application/problem+json').code).toBe('500');
    expect(guideFor(500).code).toBe('500');
  });

  test('lesson 3 lab with the API down: row shows no status and the "no answer" advice, not the traceId advice', async () => {
    fetchMock.mockResolvedValue(down());
    render(<StatusLab />);
    await userEvent.click(screen.getByRole('button', { name: '200' }));
    expect(await screen.findByText(/API not reachable \(proxy answered 502\)/)).toBeInTheDocument();
    const advice = screen.getByRole('status');
    expect(advice).toHaveTextContent('cannot reach the API');
    expect(advice).not.toHaveTextContent('traceId');
  });

  test('lesson 3 FieldErrorForm with the API down shows the friendly banner, not "502: Request failed"', async () => {
    fetchMock.mockResolvedValue(down());
    render(<FieldErrorForm />);
    await userEvent.click(screen.getByRole('button', { name: 'Open account' }));
    const banner = await screen.findByRole('status');
    expect(banner).toHaveTextContent('API not reachable — start it (see FullStackBank/README.md).');
    expect(banner).not.toHaveTextContent('Request failed');
  });

  test('api.js: a failure while the body is being read becomes an ApiError network error', async () => {
    const broken = { ok: true, status: 200, headers: new Headers({ 'Content-Type': 'application/json' }), text: () => Promise.reject(new TypeError('terminated')) };
    fetchMock.mockResolvedValue(broken);
    const err = await api.listAccounts().catch((e) => e);
    expect(err).toBeInstanceOf(ApiError);
    expect(err.kind).toBe('network');
  });
});
