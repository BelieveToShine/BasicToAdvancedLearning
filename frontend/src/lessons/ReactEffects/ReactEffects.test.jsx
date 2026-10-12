import { Profiler, StrictMode, useEffect } from 'react';
import { act, render, renderHook, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';

import Lesson1, { BalanceTitle, FeeRight, FeeWrong } from './01-WhatIsAnEffect.jsx';
import Lesson2, { Probe, StaleTicker, FixedTicker } from './02-Dependencies.jsx';
import Lesson3, { BalanceWatcher, LeakyWatcher, Clock, feed } from './03-Cleanup.jsx';
import Lesson4, { AccountsList, BrokenAsyncEffect } from './04-FetchingData.jsx';
import Lesson5, { AccountsTable, TotalBalance, useFetch } from './05-CustomHooks.jsx';

const ACCOUNTS = [
  { accountId: 1, customerName: 'Asha Rao', accountNumber: 'ACC-1001', accountType: 'Savings', balance: 1200.5 },
  { accountId: 2, customerName: 'Ben Carter', accountNumber: 'ACC-1002', accountType: 'Current', balance: 300 },
];

// A fake fetch response, shaped like the parts of Response the lessons use.
const okResponse = (data) => ({ ok: true, status: 200, json: () => Promise.resolve(data) });

// A promise we resolve by hand, to control the ORDER in which answers arrive.
function deferred() {
  let resolve;
  const promise = new Promise((r) => (resolve = r));
  return { promise, resolve };
}

afterEach(() => {
  vi.useRealTimers();
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
  document.title = '';
  feed.reset();
});

describe('Lesson 1 — What is an effect?', () => {
  test('the effect runs after the screen is updated: it sees the NEW text in the DOM', async () => {
    const user = userEvent.setup();
    render(<BalanceTitle />);
    expect(screen.getByText(/When my effect ran, the page already said: "Balance: 100"/)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Deposit 50' }));
    expect(screen.getByText(/When my effect ran, the page already said: "Balance: 150"/)).toBeInTheDocument();
  });

  test('the effect updates the browser tab title (something outside React)', async () => {
    const user = userEvent.setup();
    render(<BalanceTitle />);
    expect(document.title).toBe('Balance: 100');
    await user.click(screen.getByRole('button', { name: 'Deposit 50' }));
    expect(document.title).toBe('Balance: 150');
    await user.click(screen.getByRole('button', { name: 'Read the tab title' }));
    expect(screen.getByText('document.title is: "Balance: 150"')).toBeInTheDocument();
  });

  test('calculating during render takes one commit per change; copying into state with an effect takes two', () => {
    const rightCommits = vi.fn();
    const wrongCommits = vi.fn();
    const { rerender: rerenderRight } = render(
      <Profiler id="right" onRender={rightCommits}>
        <FeeRight balance={1000} />
      </Profiler>,
    );
    const { rerender: rerenderWrong } = render(
      <Profiler id="wrong" onRender={wrongCommits}>
        <FeeWrong balance={1000} />
      </Profiler>,
    );
    rightCommits.mockClear();
    wrongCommits.mockClear();

    rerenderRight(
      <Profiler id="right" onRender={rightCommits}>
        <FeeRight balance={2000} />
      </Profiler>,
    );
    rerenderWrong(
      <Profiler id="wrong" onRender={wrongCommits}>
        <FeeWrong balance={2000} />
      </Profiler>,
    );
    expect(rightCommits).toHaveBeenCalledTimes(1);
    expect(wrongCommits).toHaveBeenCalledTimes(2);
    // Both end up showing the same fee.
    expect(screen.getByText('Fee (calculated while rendering): 20.00')).toBeInTheDocument();
    expect(screen.getByText('Fee (copied into state by an effect): 20.00')).toBeInTheDocument();
  });

  test('the lesson page renders and both deposits work', async () => {
    const user = userEvent.setup();
    render(<Lesson1 />);
    await user.click(screen.getByRole('button', { name: 'Deposit 500' }));
    expect(screen.getByText('Account balance: 2500')).toBeInTheDocument();
    expect(screen.getByText('Fee (calculated while rendering): 25.00')).toBeInTheDocument();
  });
});

describe('Lesson 2 — Dependencies', () => {
  async function counts(user) {
    await user.click(screen.getByRole('button', { name: 'Show the run counts' }));
    const read = (prefix) => Number(screen.getByText(new RegExp('^' + prefix.replace(/[[\]()]/g, '\\$&'))).textContent.split(': ')[1]);
    return {
      every: read('no array'),
      once: read('[] (once'),
      owner: read('[owner]'),
    };
  }

  test('no array = every render, [] = once, [owner] = only when owner changes', async () => {
    const user = userEvent.setup();
    const { rerender } = render(<Probe owner="Asha Rao" />);
    expect(await counts(user)).toEqual({ every: 1, once: 1, owner: 1 });

    // A state change re-renders the component, but owner did not change.
    await user.click(screen.getByRole('button', { name: /Add a transaction/ }));
    // every-render effect ran: mount (1) + the first "show counts" click re-render (2) + the new transaction (3)
    expect(await counts(user)).toEqual({ every: 3, once: 1, owner: 1 });

    rerender(<Probe owner="Ben Carter" />);
    const after = await counts(user);
    expect(after.once).toBe(1);
    expect(after.owner).toBe(2);
    // ... + the second "show counts" click re-render (4) + the owner change (5)
    expect(after.every).toBe(5);
  });

  test('stale value: reading count inside a [] effect gets stuck, the updater form keeps counting', () => {
    vi.useFakeTimers();
    render(
      <>
        <StaleTicker />
        <FixedTicker />
      </>,
    );
    act(() => vi.advanceTimersByTime(1000));
    act(() => vi.advanceTimersByTime(1000));
    act(() => vi.advanceTimersByTime(1000));
    expect(screen.getByText('Stale ticker (stuck): 1')).toBeInTheDocument();
    expect(screen.getByText('Fixed ticker (counts up): 3')).toBeInTheDocument();
  });

  test('the lesson page renders', () => {
    render(<Lesson2 />);
    expect(screen.getByLabelText('Owner')).toBeInTheDocument();
  });
});

describe('Lesson 3 — Cleanup', () => {
  test('cleanup runs before the next effect when a dependency changes, and on unmount', () => {
    const { rerender, unmount } = render(<BalanceWatcher accountId="ACC-1001" />);
    expect(feed.history()).toEqual(['subscribe ACC-1001']);
    rerender(<BalanceWatcher accountId="ACC-1002" />);
    expect(feed.history()).toEqual(['subscribe ACC-1001', 'unsubscribe ACC-1001', 'subscribe ACC-1002']);
    expect(feed.listenerCount()).toBe(1);
    unmount();
    expect(feed.history().at(-1)).toBe('unsubscribe ACC-1002');
    expect(feed.listenerCount()).toBe(0);
  });

  test('the subscription really delivers values, and stops after cleanup', () => {
    const { unmount } = render(<BalanceWatcher accountId="ACC-1001" />);
    act(() => feed.publish('ACC-1001', 777));
    expect(screen.getByText(/Latest balance: 777/)).toBeInTheDocument();
    unmount();
    expect(feed.listenerCount()).toBe(0);
  });

  test('without a cleanup the subscription survives unmount (a leak)', () => {
    const { unmount } = render(<LeakyWatcher accountId="ACC-1001" />);
    unmount();
    expect(feed.listenerCount()).toBe(1);
  });

  test('timer: cleanup clears the interval on unmount', () => {
    vi.useFakeTimers();
    const { unmount } = render(<Clock />);
    expect(vi.getTimerCount()).toBe(1);
    act(() => vi.advanceTimersByTime(2000));
    expect(screen.getByText('Seconds since this clock appeared: 2')).toBeInTheDocument();
    unmount();
    expect(vi.getTimerCount()).toBe(0);
  });

  test('StrictMode (development) runs setup, cleanup, setup on mount; without it, setup once', () => {
    const plain = render(<BalanceWatcher accountId="ACC-1001" />);
    expect(feed.history()).toEqual(['subscribe ACC-1001']);
    plain.unmount();
    feed.reset();

    const strict = render(
      <StrictMode>
        <BalanceWatcher accountId="ACC-1001" />
      </StrictMode>,
    );
    expect(feed.history()).toEqual(['subscribe ACC-1001', 'unsubscribe ACC-1001', 'subscribe ACC-1001']);
    expect(feed.listenerCount()).toBe(1); // a correct cleanup: still exactly one listener
    strict.unmount();
    expect(feed.listenerCount()).toBe(0);
  });

  test('StrictMode exposes a missing cleanup: the leaky watcher ends up with 2 listeners', () => {
    render(
      <StrictMode>
        <LeakyWatcher accountId="ACC-1001" />
      </StrictMode>,
    );
    expect(feed.listenerCount()).toBe(2);
  });

  test('the lesson page: inspect shows listeners; removing the good watcher drops the count', async () => {
    const user = userEvent.setup();
    render(<Lesson3 />);
    await user.click(screen.getByRole('button', { name: 'Remove the good watcher' }));
    await user.click(screen.getByRole('button', { name: 'Inspect the feed now' }));
    expect(screen.getByText('Active listeners: 0')).toBeInTheDocument();
  });
});

describe('Lesson 4 — Fetching data', () => {
  test('loading, then data (the API shape: accountId, customerName, accountNumber, accountType, balance)', async () => {
    const d = deferred();
    const fetchMock = vi.fn(() => d.promise);
    vi.stubGlobal('fetch', fetchMock);
    render(<AccountsList />);
    expect(screen.getByText('Loading accounts…')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledWith('/api/accounts');
    await act(async () => d.resolve(okResponse(ACCOUNTS)));
    expect(screen.queryByText('Loading accounts…')).not.toBeInTheDocument();
    expect(screen.getByText('ACC-1001')).toBeInTheDocument();
    expect(screen.getByText('Savings')).toBeInTheDocument();
    expect(screen.getByText('1200.50')).toBeInTheDocument();
    expect(screen.getByText('300.00')).toBeInTheDocument();
  });

  test('a rejected fetch shows the error state', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new Error('Failed to connect'))));
    render(<AccountsList />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load accounts: Failed to connect');
  });

  test('an HTTP error status (ok: false) is turned into the error state by checking response.ok', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve({ ok: false, status: 500, json: () => Promise.resolve({}) })));
    render(<AccountsList />);
    expect(await screen.findByRole('alert')).toHaveTextContent('The server answered 500');
  });

  test('an out-of-date answer is ignored: first request answers LAST but does not overwrite', async () => {
    const user = userEvent.setup();
    const first = deferred();
    const second = deferred();
    const fetchMock = vi.fn().mockReturnValueOnce(first.promise).mockReturnValueOnce(second.promise);
    vi.stubGlobal('fetch', fetchMock);
    render(<AccountsList />);
    await user.click(screen.getByRole('button', { name: 'Reload' }));
    expect(fetchMock).toHaveBeenCalledTimes(2);

    await act(async () => second.resolve(okResponse([ACCOUNTS[1]]))); // newer request answers first
    expect(screen.getByText('ACC-1002')).toBeInTheDocument();
    await act(async () => first.resolve(okResponse([ACCOUNTS[0]]))); // older answer arrives late
    expect(screen.getByText('ACC-1002')).toBeInTheDocument();
    expect(screen.queryByText('ACC-1001')).not.toBeInTheDocument();
  });

  test('an answer arriving after unmount is ignored without errors', async () => {
    const d = deferred();
    vi.stubGlobal('fetch', vi.fn(() => d.promise));
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    const { unmount } = render(<AccountsList />);
    unmount();
    await act(async () => d.resolve(okResponse(ACCOUNTS)));
    expect(consoleError).not.toHaveBeenCalled();
  });

  test('an async effect function: React warns, and the cleanup inside the Promise never runs', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    const onCleanup = vi.fn();
    const { unmount } = render(<BrokenAsyncEffect onCleanup={onCleanup} />);
    expect(consoleError.mock.calls.flat().join(' ')).toContain('useEffect');
    // In the development build React tries to call the returned Promise as the cleanup function.
    expect(() => unmount()).toThrow(TypeError);
    expect(onCleanup).not.toHaveBeenCalled();
  });

  test('an async effect also throws when its dependencies change (cleanup is a Promise)', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
    function Broken({ n }) {
      useEffect(async () => {}, [n]);
      return <p>{n}</p>;
    }
    const { rerender } = render(<Broken n={1} />);
    expect(() => rerender(<Broken n={2} />)).toThrow();
  });

  test('the real API shape renders without any React "unique key" warning', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(okResponse(ACCOUNTS))));
    render(<AccountsList />);
    expect(await screen.findByText('Ben Carter')).toBeInTheDocument();
    render(<AccountsTable />);
    expect(await screen.findByText('ACC-1002 (Current): 300.00')).toBeInTheDocument();
    expect(consoleError.mock.calls.flat().join(' ')).not.toMatch(/unique "key"/);
    expect(consoleError).not.toHaveBeenCalled();
  });

  test('the lesson page renders the list', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(okResponse(ACCOUNTS))));
    render(<Lesson4 />);
    expect(await screen.findByText('ACC-1002')).toBeInTheDocument();
  });
});

describe('Lesson 5 — Custom hooks', () => {
  test('useFetch returns loading, then data, and exposes reload', async () => {
    const fetchMock = vi.fn(() => Promise.resolve(okResponse(ACCOUNTS)));
    vi.stubGlobal('fetch', fetchMock);
    const { result } = renderHook(() => useFetch('/api/accounts'));
    expect(result.current.status).toBe('loading');
    await act(async () => {});
    expect(result.current.status).toBe('success');
    expect(result.current.data).toEqual(ACCOUNTS);
    await act(async () => result.current.reload());
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  test('useFetch fetches again when the url changes, and reports errors', async () => {
    const fetchMock = vi.fn((url) =>
      url === '/bad' ? Promise.resolve({ ok: false, status: 404 }) : Promise.resolve(okResponse([url])),
    );
    vi.stubGlobal('fetch', fetchMock);
    const { result, rerender } = renderHook(({ url }) => useFetch(url), { initialProps: { url: '/a' } });
    await act(async () => {});
    expect(result.current.data).toEqual(['/a']);
    rerender({ url: '/bad' });
    await act(async () => {});
    expect(result.current.status).toBe('error');
    expect(result.current.error).toBe('The server answered 404');
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  test('two components using the same hook share logic, not state: two requests, independent reload', async () => {
    const fetchMock = vi.fn(() => Promise.resolve(okResponse(ACCOUNTS)));
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();
    render(
      <>
        <AccountsTable />
        <TotalBalance />
      </>,
    );
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(await screen.findByText('Total across 2 accounts: 1500.50')).toBeInTheDocument();
    expect(screen.getByText('ACC-1001 (Savings): 1200.50')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Reload the table' }));
    expect(fetchMock).toHaveBeenCalledTimes(3); // only the table asked again
    expect(screen.getByText('Total across 2 accounts: 1500.50')).toBeInTheDocument(); // total untouched
  });

  test('the lesson page shows the error state when the API is down', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new Error('offline'))));
    render(<Lesson5 />);
    expect(await screen.findByText('Table error: offline')).toBeInTheDocument();
    expect(screen.getByText('Total error: offline')).toBeInTheDocument();
  });
});
