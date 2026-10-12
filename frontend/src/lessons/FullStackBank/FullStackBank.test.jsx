// Tests for the FullStackBank mini project. fetch is replaced by a small in-memory fake API that
// answers the same JSON/status codes as the real Web API (FullStackBank/README.md), so the tests are
// deterministic and need no network. The REAL API is checked separately (see the handoff note).
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, within, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { callLog } from './api.js';
import Lesson1, { title as t1 } from './01-AccountsList.jsx';
import Lesson2, { title as t2 } from './02-AccountDetail.jsx';
import Lesson3, { title as t3 } from './03-MoneyForm.jsx';
import Lesson4, { title as t4 } from './04-OpenAccount.jsx';
import Lesson5, { title as t5 } from './05-TheWholeApp.jsx';

const json = (status, body) => new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json; charset=utf-8' } });
const problem = (status, title, extra = {}) =>
  new Response(JSON.stringify({ title, status, traceId: '00-test', ...extra }), { status, headers: { 'content-type': 'application/problem+json' } });

// The fake API: same rules as the real one for the cases we test.
function makeFakeApi() {
  const accounts = [
    { accountId: 1, customerName: 'Asha Rao', accountNumber: 'ACC-1001', accountType: 'Savings', balance: 5000 },
    { accountId: 2, customerName: 'Ben Carter', accountNumber: 'ACC-1002', accountType: 'Current', balance: 1200.5 },
    { accountId: 3, customerName: 'Chen Li', accountNumber: 'ACC-1003', accountType: 'Savings', balance: 300 },
  ];
  const txns = { 1: [{ transactionId: 1, kind: 'Deposit', amount: 5000, createdAt: '2026-10-12T10:00:00.1234567Z' }], 2: [], 3: [] };
  let nextTxn = 10;
  const calls = [];
  const fn = vi.fn(async (url, init = {}) => {
    const method = init.method ?? 'GET';
    const body = init.body ? JSON.parse(init.body) : undefined;
    calls.push({ method, url, body });
    let m;
    if (method === 'GET' && (m = url.match(/^\/api\/accounts(\?type=(.*))?$/))) {
      if (m[2] && !['Savings', 'Current'].includes(m[2])) return problem(400, 'One or more validation errors occurred.', { errors: { type: ["type must be 'Savings' or 'Current'."] } });
      return json(200, accounts.filter((a) => !m[2] || a.accountType === m[2]));
    }
    if (method === 'GET' && (m = url.match(/^\/api\/accounts\/(\d+)$/))) {
      const a = accounts.find((x) => x.accountId === +m[1]);
      return a ? json(200, a) : problem(404, 'Account not found', { detail: `There is no account with id ${m[1]}.` });
    }
    if (method === 'GET' && (m = url.match(/^\/api\/accounts\/(\d+)\/transactions$/))) {
      return txns[+m[1]] ? json(200, txns[+m[1]]) : problem(404, 'Account not found', { detail: `There is no account with id ${m[1]}.` });
    }
    if (method === 'POST' && (m = url.match(/^\/api\/accounts\/(\d+)\/transactions$/))) {
      const a = accounts.find((x) => x.accountId === +m[1]);
      // same rules as the real API: 0.01 .. 1,000,000,000,000.00 and at most 2 decimals (checked before the action runs)
      if (!(body.amount >= 0.01 && body.amount <= 1e12))
        return problem(400, 'One or more validation errors occurred.', { errors: { amount: ['amount must be between 0.01 and 1,000,000,000,000.00.'] } });
      if (Math.round(body.amount * 100) / 100 !== body.amount)
        return problem(400, 'One or more validation errors occurred.', { errors: { amount: ['amount can have at most 2 decimal places.'] } });
      if (!a) return problem(404, 'Account not found', { detail: `There is no account with id ${m[1]}.` });
      if (body.kind === 'Withdrawal' && body.amount > a.balance)
        return problem(422, 'Insufficient funds', { detail: `Account ${a.accountId} has a balance of ${a.balance.toFixed(2)}; a withdrawal of ${body.amount.toFixed(2)} is not allowed.` });
      a.balance += body.kind === 'Deposit' ? body.amount : -body.amount;
      const t = { transactionId: nextTxn++, kind: body.kind, amount: body.amount, createdAt: '2026-10-12T11:00:00.0000000Z' };
      txns[a.accountId] = [t, ...txns[a.accountId]]; // newest first, like the API
      return json(201, t);
    }
    if (method === 'POST' && url === '/api/accounts') {
      if (![1, 2, 3].includes(body.customerId)) return problem(400, 'One or more validation errors occurred.', { errors: { customerId: [`Customer ${body.customerId} does not exist.`] } });
      if (accounts.some((x) => x.accountNumber === body.accountNumber)) return problem(409, 'Duplicate value', { detail: 'A record with the same unique value (for example the account number) already exists.' });
      const a = { accountId: accounts.length + 1, customerName: { 1: 'Asha Rao', 2: 'Ben Carter', 3: 'Chen Li' }[body.customerId], accountNumber: body.accountNumber, accountType: body.accountType, balance: 0 };
      accounts.push(a);
      txns[a.accountId] = [];
      return json(201, a);
    }
    return problem(404, 'Not Found');
  });
  return { fn, calls, accounts };
}

let fake;
beforeEach(() => {
  fake = makeFakeApi();
  vi.stubGlobal('fetch', fake.fn);
  callLog.clear();
});
afterEach(() => vi.unstubAllGlobals());

const bodyRows = () => within(screen.getByRole('table', { name: 'accounts' })).getAllByRole('row').slice(1); // without the header row

describe('every lesson has a title and renders', () => {
  it.each([[Lesson1, t1], [Lesson2, t2], [Lesson3, t3], [Lesson4, t4], [Lesson5, t5]])('%#', async (Lesson, title) => {
    expect(typeof title).toBe('string');
    render(<Lesson />);
    expect(screen.getByRole('region', { name: /journey/i })).toBeInTheDocument();
    await screen.findByText('ACC-1001'); // lessons 1-5 all start on the list
  });
});

describe('01 accounts list', () => {
  it('shows Loading first, then the rows the API sent', async () => {
    render(<Lesson1 />);
    expect(screen.getByText('Loading accounts...')).toBeInTheDocument();
    await screen.findByText('ACC-1001');
    expect(bodyRows()).toHaveLength(3);
    expect(screen.getByText('5000.00')).toBeInTheDocument();
    expect(screen.getByText('1200.50')).toBeInTheDocument(); // money shown with 2 decimals
  });

  it('the filter is STATE; changing it makes a new request with ?type= and the API does the filtering', async () => {
    const user = userEvent.setup();
    render(<Lesson1 />);
    await screen.findByText('ACC-1001');
    expect(fake.calls[0].url).toBe('/api/accounts');
    await user.click(screen.getByRole('button', { name: 'Savings' }));
    await waitFor(() => expect(bodyRows()).toHaveLength(2));
    expect(fake.calls.at(-1).url).toBe('/api/accounts?type=Savings');
    expect(screen.getByRole('button', { name: 'Savings' })).toHaveAttribute('aria-pressed', 'true');
    await user.click(screen.getByRole('button', { name: 'Current' }));
    await waitFor(() => expect(bodyRows()).toHaveLength(1));
    expect(fake.calls.at(-1).url).toBe('/api/accounts?type=Current');
    await user.click(screen.getByRole('button', { name: 'All' }));
    await waitFor(() => expect(bodyRows()).toHaveLength(3));
    expect(fake.calls.at(-1).url).toBe('/api/accounts'); // no ?type= again
  });

  it('a late answer for an old filter does not overwrite the newer one', async () => {
    const user = userEvent.setup();
    let release;
    const slowSavings = new Promise((r) => (release = r));
    const original = fake.fn.getMockImplementation();
    fake.fn.mockImplementation(async (url, init) => {
      if (url === '/api/accounts?type=Savings') await slowSavings; // this answer arrives LAST
      return original(url, init);
    });
    render(<Lesson1 />);
    await screen.findByText('ACC-1001');
    await user.click(screen.getByRole('button', { name: 'Savings' })); // slow
    await user.click(screen.getByRole('button', { name: 'Current' })); // fast
    await screen.findByText('ACC-1002');
    release(); // now the old Savings answer arrives
    await new Promise((r) => setTimeout(r, 20));
    expect(bodyRows()).toHaveLength(1); // still the Current list
    expect(screen.queryByText('ACC-1001')).not.toBeInTheDocument();
  });

  it('API not reachable (fetch rejects): friendly message, retry works, never a blank page', async () => {
    const user = userEvent.setup();
    fake.fn.mockRejectedValueOnce(new TypeError('Failed to fetch'));
    render(<Lesson1 />);
    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('API not reachable');
    expect(alert).toHaveTextContent('FullStackBank/README.md');
    await user.click(within(alert).getByRole('button', { name: 'Try again' }));
    await screen.findByText('ACC-1001');
  });

  it('a non-JSON error answer (what the Vite proxy sends when the API is down) is also "not reachable"', async () => {
    fake.fn.mockResolvedValueOnce(new Response('', { status: 500, headers: { 'content-type': 'text/plain' } }));
    render(<Lesson1 />);
    expect(await screen.findByRole('alert')).toHaveTextContent('API not reachable');
  });

  it('a JSON error (problem+json) is shown with its title: not the same as unreachable', async () => {
    fake.fn.mockResolvedValueOnce(problem(500, 'An unexpected error occurred', { detail: 'Something went wrong.' }));
    render(<Lesson1 />);
    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('500 An unexpected error occurred');
    expect(alert).not.toHaveTextContent('not reachable');
  });
});

describe('02 account detail', () => {
  it('shows the account and the transactions in exactly the order the API sent (newest first, no sorting in React)', async () => {
    const user = userEvent.setup();
    fake.accounts[0].balance = 4900;
    // the API's order is newest first: ids 7, 5, 2 (NOT ascending, NOT alphabetical)
    const original = fake.fn.getMockImplementation();
    fake.fn.mockImplementation(async (url, init) =>
      url === '/api/accounts/1/transactions'
        ? json(200, [
            { transactionId: 7, kind: 'Withdrawal', amount: 100, createdAt: '2026-10-12T12:00:00Z' },
            { transactionId: 5, kind: 'Deposit', amount: 50.5, createdAt: '2026-10-12T11:00:00Z' },
            { transactionId: 2, kind: 'Deposit', amount: 5000, createdAt: '2026-10-12T10:00:00Z' },
          ])
        : original(url, init),
    );
    render(<Lesson2 />);
    await user.click(await screen.findByRole('button', { name: 'Open ACC-1001' }));
    expect(await screen.findByTestId('balance')).toHaveTextContent('4900.00');
    expect(screen.getByTestId('account-summary')).toHaveTextContent('Asha Rao - ACC-1001 (Savings)');
    const ids = within(screen.getByTestId('txn-rows')).getAllByRole('row').map((r) => r.cells[0].textContent);
    expect(ids).toEqual(['7', '5', '2']);
    expect(screen.getByText('2026-10-12 12:00:00 UTC')).toBeInTheDocument();
    expect(fake.fn.mock.calls.map((c) => c[0])).toEqual(expect.arrayContaining(['/api/accounts/1', '/api/accounts/1/transactions']));
  });

  it('an unknown id shows the API 404 (title + detail); the page does not break', async () => {
    const user = userEvent.setup();
    render(<Lesson2 />);
    await user.click(await screen.findByRole('button', { name: /Open account 9999/ }));
    const alerts = await screen.findAllByRole('alert');
    expect(alerts).toHaveLength(2); // the account request and the transactions request each failed on their own
    expect(alerts[0]).toHaveTextContent('404 Account not found');
    expect(alerts[0]).toHaveTextContent('There is no account with id 9999.');
    await user.click(screen.getByRole('button', { name: 'Back to the list' }));
    await screen.findByText('ACC-1001');
  });

  it('an account with no transactions says so', async () => {
    const user = userEvent.setup();
    render(<Lesson2 />);
    await user.click(await screen.findByRole('button', { name: 'Open ACC-1002' }));
    expect(await screen.findByText('No transactions yet.')).toBeInTheDocument();
  });
});

describe('03 money form', () => {
  async function openAccount1(user) {
    render(<Lesson3 />);
    await user.click(await screen.findByRole('button', { name: 'Open ACC-1001' }));
    await screen.findByTestId('balance');
  }

  it('POSTs {kind, amount as a NUMBER}, then re-fetches the account: the balance comes from the API', async () => {
    const user = userEvent.setup();
    await openAccount1(user);
    expect(screen.getByTestId('balance')).toHaveTextContent('5000.00');
    await user.type(screen.getByLabelText('Amount'), '100.5');
    await user.click(screen.getByRole('button', { name: 'Send' }));
    expect(await screen.findByRole('status')).toHaveTextContent('Recorded Deposit #10 of 100.50');
    const post = fake.calls.find((c) => c.method === 'POST');
    expect(post).toEqual({ method: 'POST', url: '/api/accounts/1/transactions', body: { kind: 'Deposit', amount: 100.5 } });
    expect(typeof post.body.amount).toBe('number');
    await waitFor(() => expect(screen.getByTestId('balance')).toHaveTextContent('5100.50'));
    // order of requests: the POST came first, the account GET after it
    const urls = fake.calls.map((c) => `${c.method} ${c.url}`);
    expect(urls.lastIndexOf('GET /api/accounts/1')).toBeGreaterThan(urls.indexOf('POST /api/accounts/1/transactions'));
    // the new transaction is the first (newest) row
    await waitFor(() => expect(within(screen.getByTestId('txn-rows')).getAllByRole('row')[0].cells[0]).toHaveTextContent('10'));
    expect(screen.getByLabelText('Amount')).toHaveValue(null); // input cleared
  });

  it('the balance is not changed by React when the POST fails (422 insufficient funds is shown)', async () => {
    const user = userEvent.setup();
    await openAccount1(user);
    await user.selectOptions(screen.getByLabelText('Kind'), 'Withdrawal');
    await user.type(screen.getByLabelText('Amount'), '999999');
    await user.click(screen.getByRole('button', { name: 'Send' }));
    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('422 Insufficient funds');
    expect(alert).toHaveTextContent('balance of 5000.00');
    expect(screen.getByTestId('balance')).toHaveTextContent('5000.00');
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('a 400 shows the per-field message (amount 0)', async () => {
    const user = userEvent.setup();
    await openAccount1(user);
    await user.type(screen.getByLabelText('Amount'), '0');
    await user.click(screen.getByRole('button', { name: 'Send' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('amount: amount must be between 0.01 and 1,000,000,000,000.00.');
  });

  it('the button is disabled while the request is in flight (no double submit)', async () => {
    const user = userEvent.setup();
    await openAccount1(user);
    let release;
    const gate = new Promise((r) => (release = r));
    const original = fake.fn.getMockImplementation();
    fake.fn.mockImplementation(async (url, init) => {
      if (init?.method === 'POST') await gate;
      return original(url, init);
    });
    await user.type(screen.getByLabelText('Amount'), '10');
    await user.click(screen.getByRole('button', { name: 'Send' }));
    const busy = screen.getByRole('button', { name: 'Sending...' });
    expect(busy).toBeDisabled();
    release();
    await screen.findByRole('status');
    expect(screen.getByRole('button', { name: 'Send' })).toBeEnabled();
    expect(fake.calls.filter((c) => c.method === 'POST')).toHaveLength(1);
  });
});

describe('04 open an account', () => {
  async function fill(user, number, customer = '1') {
    await user.click(await screen.findByRole('button', { name: 'Open a new account' }));
    await user.clear(screen.getByLabelText('Customer id'));
    await user.type(screen.getByLabelText('Customer id'), customer);
    await user.type(screen.getByLabelText('Account number'), number);
    await user.selectOptions(screen.getByLabelText('Type'), 'Current');
    await user.click(screen.getByRole('button', { name: 'Open account' }));
  }

  it('201: sends the body, tells what the API returned, and the list fetched again includes the new account', async () => {
    const user = userEvent.setup();
    render(<Lesson4 />);
    await fill(user, 'ACC-2001');
    expect(await screen.findByRole('status')).toHaveTextContent('Opened account ACC-2001 (accountId 4, balance 0.00)');
    expect(fake.calls.find((c) => c.method === 'POST').body).toEqual({ customerId: 1, accountNumber: 'ACC-2001', accountType: 'Current' });
    await waitFor(() => expect(bodyRows()).toHaveLength(4)); // 3 seeded + the new one
    expect(within(screen.getByRole('table', { name: 'accounts' })).getByText('ACC-2001')).toBeInTheDocument();
  });

  it('409 duplicate account number is shown in the form, which stays open with the typed values', async () => {
    const user = userEvent.setup();
    render(<Lesson4 />);
    await fill(user, 'ACC-1001');
    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('409 Duplicate value');
    expect(screen.getByLabelText('Account number')).toHaveValue('ACC-1001');
    expect(screen.getByRole('button', { name: 'Open account' })).toBeEnabled();
  });

  it('400 for an unknown customer shows errors.customerId', async () => {
    const user = userEvent.setup();
    render(<Lesson4 />);
    await fill(user, 'ACC-3000', '99');
    expect(await screen.findByRole('alert')).toHaveTextContent('customerId: Customer 99 does not exist.');
  });
});

describe('05 the whole app', () => {
  it('list -> detail -> deposit -> back -> open account -> new account detail, all through the API', async () => {
    const user = userEvent.setup();
    render(<Lesson5 />);
    await user.click(await screen.findByRole('button', { name: 'Open ACC-1003' }));
    await waitFor(() => expect(screen.getByTestId('balance')).toHaveTextContent('300.00'));
    await user.type(screen.getByLabelText('Amount'), '25');
    await user.click(screen.getByRole('button', { name: 'Send' }));
    await waitFor(() => expect(screen.getByTestId('balance')).toHaveTextContent('325.00'));

    await user.click(screen.getByRole('button', { name: 'Back to the list' }));
    expect(await screen.findByText('325.00')).toBeInTheDocument(); // the list shows the stored value

    await user.click(screen.getByRole('button', { name: 'Open an account' }));
    await user.type(screen.getByLabelText('Account number'), 'ACC-2002');
    await user.click(screen.getByRole('button', { name: 'Open account' }));
    // the 201 body's accountId (4) takes us to the new account's detail screen
    expect(await screen.findByText('Account 4')).toBeInTheDocument();
    await waitFor(() => expect(screen.getByTestId('account-summary')).toHaveTextContent('ACC-2002'));
    expect(screen.getByTestId('balance')).toHaveTextContent('0.00');
  });

  it('the journey panel names the newest request, its answer and the SQL, and keeps a history', async () => {
    const user = userEvent.setup();
    render(<Lesson5 />);
    await screen.findByText('ACC-1001');
    const panel = screen.getByRole('region', { name: /journey/i });
    expect(within(panel).getByTestId('last-call')).toHaveTextContent('GET /api/accounts');
    expect(within(panel).getByTestId('last-status')).toHaveTextContent('200');
    expect(panel).toHaveTextContent('SQL that ran: SELECT ... FROM Accounts');
    expect(panel).toHaveTextContent('StrictMode runs effects twice');
    await user.click(screen.getByRole('button', { name: 'Open ACC-1001' }));
    await waitFor(() => expect(within(panel).getByTestId('call-history')).toHaveTextContent('GET /api/accounts/1/transactions 200'));
    expect(within(panel).getByTestId('call-history').textContent).toContain('GET /api/accounts 200  |  ');
  });

  it('when the API is down every screen says so instead of going blank', async () => {
    fake.fn.mockRejectedValue(new TypeError('Failed to fetch'));
    render(<Lesson5 />);
    expect(await screen.findByRole('alert')).toHaveTextContent('API not reachable');
    expect(within(screen.getByRole('region', { name: /journey/i })).getByTestId('last-status')).toHaveTextContent('no answer');
  });
});

describe('the Journey panel only claims SQL that really ran', () => {
  const sqlLine = () => screen.getByTestId('last-call').textContent;

  it('422: only the SELECT ran, nothing was written', async () => {
    const user = userEvent.setup();
    render(<Lesson3 />);
    await user.click(await screen.findByRole('button', { name: 'Open ACC-1001' }));
    await screen.findByTestId('balance');
    await user.selectOptions(screen.getByLabelText('Kind'), 'Withdrawal');
    await user.type(screen.getByLabelText('Amount'), '999999');
    await user.click(screen.getByRole('button', { name: 'Send' }));
    await screen.findByRole('alert');
    expect(sqlLine()).toContain('answer 422');
    expect(sqlLine()).toContain('only (the rule refused it; nothing written)');
    expect(sqlLine()).not.toContain('INSERT INTO Transactions');
  });

  it('400 on the amount: no SQL ran', async () => {
    const user = userEvent.setup();
    render(<Lesson3 />);
    await user.click(await screen.findByRole('button', { name: 'Open ACC-1001' }));
    await screen.findByTestId('balance');
    await user.type(screen.getByLabelText('Amount'), '0');
    await user.click(screen.getByRole('button', { name: 'Send' }));
    await screen.findByRole('alert');
    expect(sqlLine()).toContain('no SQL ran: validation failed before the controller action');
  });

  it('400 on an unknown customer: the customer lookup ran, nothing inserted; 409: INSERT refused by the UNIQUE index', async () => {
    const user = userEvent.setup();
    render(<Lesson4 />);
    await user.click(await screen.findByRole('button', { name: 'Open a new account' }));
    await user.clear(screen.getByLabelText('Customer id'));
    await user.type(screen.getByLabelText('Customer id'), '99');
    await user.type(screen.getByLabelText('Account number'), 'ACC-3000');
    await user.click(screen.getByRole('button', { name: 'Open account' }));
    await screen.findByRole('alert');
    expect(sqlLine()).toContain('SELECT ... FROM Customers');
    await user.clear(screen.getByLabelText('Customer id'));
    await user.type(screen.getByLabelText('Customer id'), '1');
    await user.clear(screen.getByLabelText('Account number'));
    await user.type(screen.getByLabelText('Account number'), 'ACC-1001');
    await user.click(screen.getByRole('button', { name: 'Open account' }));
    await waitFor(() => expect(sqlLine()).toContain('refused by the UNIQUE index'));
  });

  it('404: a SELECT that found no row; API down: no SQL ran because the request never reached the API', async () => {
    const user = userEvent.setup();
    render(<Lesson2 />);
    await user.click(await screen.findByRole('button', { name: /Open account 9999/ }));
    await screen.findAllByRole('alert');
    expect(sqlLine()).toContain('found no row');
    expect(sqlLine()).not.toContain('ORDER BY');
  });

  it('no answer / non-JSON proxy error: no SQL ran', async () => {
    fake.fn.mockRejectedValue(new TypeError('Failed to fetch'));
    const { unmount } = render(<Lesson1 />);
    await screen.findByRole('alert');
    expect(sqlLine()).toContain('the request never reached the API, so no SQL ran');
    unmount();
    fake.fn.mockReset();
    fake.fn.mockResolvedValue(new Response('', { status: 502, headers: { 'content-type': 'text/plain' } }));
    render(<Lesson1 />);
    await screen.findByRole('alert');
    expect(sqlLine()).toContain('answer 502');
    expect(sqlLine()).toContain('never reached the API');
  });

  it('a lesson starts with an empty log: leftovers from the previous lesson are not shown', async () => {
    const { unmount } = render(<Lesson1 />);
    await screen.findByText('ACC-1001');
    unmount();
    render(<Lesson5 />);
    await screen.findByText('ACC-1001');
    expect(screen.getByTestId('last-call')).toHaveTextContent('GET /api/accounts');
    expect(screen.queryByTestId('call-history')).not.toBeInTheDocument(); // only one call so far in THIS lesson
  });
});
