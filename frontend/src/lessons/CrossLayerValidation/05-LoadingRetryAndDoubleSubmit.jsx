/*
 * 05 - Loading, retry and double submit.
 *
 * Between "the user clicked" and "the answer arrived" a request takes time, and the user may click again.
 * Without a guard each click is a separate request. A deposit is not "set the balance to X": it ADDS money,
 * so two requests add it twice. Below, the same real API call is wired two ways:
 *
 *   Unguarded  the handler just sends. Two quick clicks -> two POSTs -> two transactions.
 *   Guarded    (1) while a request is pending the button is disabled and says "Sending..." (that also
 *                  tells the user something is happening), and
 *              (2) the handler itself refuses to start while one is in flight, using a ref.
 *
 * WHY BOTH? `disabled` comes from state, and a handler only sees the state of the render it was created in.
 * When one event calls the handler twice in a row, both calls see pending = false (the screen has not been
 * redrawn between them), so a state-only guard lets both through. A ref changes immediately and every call
 * sees the latest value. The test file proves both halves: a real double click is stopped by `disabled`;
 * two direct calls are stopped only by the ref.
 *
 * HOW THE REAL API REACTS (observed, see the handoff note): on a fast local connection a request is over in a few
 * milliseconds, so a double click rarely overlaps with itself. Two requests that DO overlap on the same account are
 * caught by the API's concurrency check (README: RowVersion): one deposit is saved, the other is answered 409 and
 * nothing is saved for it. The money is never doubled that way, but the user sees an error for a click they thought
 * was one action. Two requests that do not overlap (the second one starts after the first finished) both succeed:
 * two deposits. A slow network makes the guard matter, so this page can add a pretend delay (300 ms) before each
 * request is sent; with it, a double click 100 ms apart gives two deposits when there is no guard.
 *
 * `finally` re-enables the button whatever happened, so a failed request never leaves the form stuck.
 * Retry: after a failure the button becomes "Try again". Careful with POST: if the network failed AFTER the
 * server did the work, the answer is lost but the money moved. Retrying such a request blindly can add it
 * twice, which is why the page shows the transaction count to compare. (Reads such as GET are safe to repeat;
 * this API has no "idempotency key" that would make POST safe too.)
 *
 * Every deposit here is 1.00 and really changes the account you pick (the count and balance above update).
 */
import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiDown, bad, box, callApi, classify, friendlyMessage, money, useAccounts } from './bankApi.jsx';

export const title = '5. Loading, retry and double submit';

const DEAD_ADDRESS = 'http://localhost:59999'; // an address nothing answers on: fetch rejects, as when the network is down

function useAccountInfo(accountId, tick) {
  const [info, setInfo] = useState(null);
  useEffect(() => {
    let cancelled = false;
    Promise.all([callApi('GET', `/api/accounts/${accountId}`), callApi('GET', `/api/accounts/${accountId}/transactions`)]).then(([a, t]) => {
      if (!cancelled && a.ok && t.ok) setInfo({ balance: a.data.balance, count: t.data.length });
    });
    return () => {
      cancelled = true;
    };
  }, [accountId, tick]);
  return info;
}

const wait = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

async function postDeposit(accountId, baseUrl = '', latencyMs = 0) {
  if (latencyMs > 0) await wait(latencyMs); // stands in for a slow network: the request leaves later
  return callApi('POST', `${baseUrl}/api/accounts/${accountId}/transactions`, { kind: 'Deposit', amount: 1 });
}

// Version 1: no guard at all.
export function UnguardedDeposit({ accountId, onDone, latencyMs = 0 }) {
  const [answers, setAnswers] = useState([]);

  async function deposit() {
    const result = await postDeposit(accountId, '', latencyMs);
    setAnswers((all) => [...all, `HTTP ${result.status}`]);
    onDone();
  }

  return (
    <div style={box}>
      <h3 style={{ marginTop: 0 }}>Without a guard</h3>
      <button type="button" onClick={deposit}>Deposit 1.00</button>{' '}
      <button
        type="button"
        onClick={() => {
          // What a quick double click does: the second request starts before the first answer is back.
          deposit();
          setTimeout(deposit, 100);
        }}
      >
        Click twice, 100 ms apart
      </button>{' '}
      <button
        type="button"
        onClick={() => {
          deposit();
          deposit();
        }}
      >
        Both at the same instant
      </button>
      <p data-testid="unguarded-answers">Answers so far: {answers.length === 0 ? 'none' : answers.join(', ')}</p>
    </div>
  );
}

// Version 2: guarded, with a Try again after a failure.
export function GuardedDeposit({ accountId, onDone, latencyMs = 0 }) {
  const [pending, setPending] = useState(false); // for the screen: disabled button, "Sending..."
  const inFlight = useRef(false); // for the logic: always current, even between two renders
  const [result, setResult] = useState(null);
  const [failNext, setFailNext] = useState(false);
  const [answers, setAnswers] = useState([]);

  async function deposit() {
    if (inFlight.current) return; // a request is already on its way: ignore this click
    inFlight.current = true;
    setPending(true);
    try {
      const baseUrl = failNext ? DEAD_ADDRESS : ''; // lets you rehearse a network failure on purpose
      if (failNext) setFailNext(false);
      const answer = await postDeposit(accountId, baseUrl, latencyMs);
      setResult(answer);
      setAnswers((all) => [...all, answer.network ? 'no answer' : `HTTP ${answer.status}`]);
      onDone();
    } finally {
      inFlight.current = false; // always: success, failure or exception
      setPending(false);
    }
  }

  const failed = result !== null && !result.ok;
  const retryable = failed && ['network', 'gateway', 'server'].includes(classify(result));
  return (
    <div style={box}>
      <h3 style={{ marginTop: 0 }}>With a guard</h3>
      <label>
        <input type="checkbox" checked={failNext} onChange={(e) => setFailNext(e.target.checked)} /> make the next attempt fail
        (sent to an address nothing answers on, like a dead network)
      </label>
      <br />
      <button type="button" onClick={deposit} disabled={pending}>
        {pending ? 'Sending...' : retryable ? 'Try again' : 'Deposit 1.00'}
      </button>{' '}
      <button
        type="button"
        onClick={() => {
          deposit();
          setTimeout(deposit, 100);
        }}
        disabled={pending}
      >
        Call the handler twice, 100 ms apart
      </button>{' '}
      <button
        type="button"
        onClick={() => {
          deposit();
          deposit();
        }}
        disabled={pending}
      >
        Call the handler twice in a row
      </button>
      {failed && (
        <p role="alert" style={bad} data-testid="guarded-failure">
          {friendlyMessage(result)}
          {retryable && ' The deposit may or may not have been made: compare the transaction count above before you try again.'}
        </p>
      )}
      <p data-testid="guarded-answers">Answers so far: {answers.length === 0 ? 'none' : answers.join(', ')}</p>
    </div>
  );
}

export default function Lesson() {
  const { status, accounts, reload } = useAccounts();
  const [chosen, setChosen] = useState(null);
  const [tick, setTick] = useState(0);
  const [slow, setSlow] = useState(true);
  const refresh = useCallback(() => setTick((t) => t + 1), []);
  const account = accounts.find((a) => a.accountId === chosen) ?? accounts[0];
  const info = useAccountInfo(account ? account.accountId : 0, tick);

  if (status === 'loading') return <p>Loading accounts...</p>;
  if (status === 'down') return <ApiDown onRetry={reload} />;
  if (!account) return <p role="alert">No account to use. Seed data: see FullStackBank/README.md.</p>;

  return (
    <section>
      <h1>{title}</h1>
      <p>
        Account:{' '}
        <select aria-label="Account" value={account.accountId} onChange={(e) => setChosen(Number(e.target.value))}>
          {accounts.map((a) => (
            <option key={a.accountId} value={a.accountId}>
              {a.accountNumber} - {a.customerName}
            </option>
          ))}
        </select>
      </p>
      <p data-testid="info">
        Balance now: <strong>{info ? money(info.balance) : '...'}</strong> - transactions on this account:{' '}
        <strong>{info ? info.count : '...'}</strong>
      </p>
      <p>Each deposit is 1.00 and really changes the account. Compare the numbers above before and after each button.</p>
      <p style={{ fontSize: 14 }}>
        Notes: in the guarded panel the <code>disabled</code> button alone already stops a REAL double click. The "100 ms apart" and
        "twice in a row" buttons call the handler directly, which simulates a stale-closure call a real user cannot make; they show why the
        ref is there. A 409 from the API's concurrency check (overlapping requests) has no "Try again" button on this page, but the right
        reaction is the same: read the account again, then retry.
      </p>
      <p>
        <label>
          <input type="checkbox" checked={slow} onChange={(e) => setSlow(e.target.checked)} /> pretend the network is slow (300 ms
          before each request leaves)
        </label>
      </p>
      <UnguardedDeposit accountId={account.accountId} onDone={refresh} latencyMs={slow ? 300 : 0} />
      <GuardedDeposit accountId={account.accountId} onDone={refresh} latencyMs={slow ? 300 : 0} />
    </section>
  );
}
