/*
 * Mini project, step 3 - deposit / withdraw: the first request that CHANGES data.
 *
 * Reading was GET. Now the form sends POST /api/accounts/{id}/transactions with a JSON body:
 *     { "kind": "Deposit" | "Withdrawal", "amount": 100.5 }
 * The amount is always positive; "kind" says which way the money moves.
 *
 * Rule of this step: React does NOT calculate the new balance. After a successful POST the form
 * tells its parent (onDone), the parent bumps `version`, and AccountDetail re-fetches the account
 * and its transactions. Why? The server owns the money rules and the data (and other users or tabs
 * may have changed it too). The screen shows what the API says, not what we guess.
 *
 * Success = 201 Created + the new transaction. Failures come back as problem+json and are SHOWN:
 *   - 422 "Insufficient funds": the request is well-formed but a business rule refuses it
 *     (withdrawing more than the balance). Try it: withdraw 1000000.
 *   - 400 with `errors`: invalid input (e.g. amount 0 or more than 2 decimals).
 * (Validating in the browser too, so the user is told before sending, is topic 27's subject.
 *  The server check stays either way: anyone can call the API without our page.)
 *
 * While the request is in flight the button is disabled, so a normal double click cannot send it twice.
 * (Two button.click() calls in one JavaScript task, or form.requestSubmit(), get past a disabled button; the
 * hard guard is a ref that remembers "already sending" - topic 27 covers it.)
 */
import { useState } from 'react';
import { api, money } from './api.js';
import { ErrorView, Journey, STATE, FROM_API, useFreshCallLog } from './parts.jsx';
import { AccountDetail } from './02-AccountDetail.jsx';
import { AccountsList } from './01-AccountsList.jsx';

export const title = '3. Deposit / withdraw (POST, then re-fetch the balance)';

export function MoneyForm({ accountId, onDone }) {
  const [kind, setKind] = useState('Deposit'); // STATE: the controls (what the user is typing)
  const [amount, setAmount] = useState('');
  const [busy, setBusy] = useState(false); // STATE: a request is in flight
  const [error, setError] = useState(null); // STATE: the last failure, or null
  const [done, setDone] = useState(null); // STATE: the last 201 body, or null

  async function submit(event) {
    event.preventDefault(); // stop the browser's own form submit (a full page reload)
    setBusy(true);
    setError(null);
    setDone(null);
    try {
      // The input holds TEXT; the API wants a JSON number, so convert.
      const created = await api.postTransaction(accountId, kind, Number(amount));
      setDone(created);
      setAmount('');
      onDone?.(created); // the parent decides what to refresh
    } catch (e) {
      setError(e);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={submit} aria-label="money form">
      <h4>Deposit or withdraw</h4>
      <label>
        Kind{' '}
        <select value={kind} onChange={(e) => setKind(e.target.value)}>
          <option>Deposit</option>
          <option>Withdrawal</option>
        </select>
      </label>{' '}
      <label>
        Amount{' '}
        <input type="number" step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} style={{ width: 100 }} />
      </label>{' '}
      <button type="submit" disabled={busy}>
        {busy ? 'Sending...' : 'Send'}
      </button>
      {error && <ErrorView error={error} />}
      {done && (
        <p role="status">
          Recorded {done.kind} #{done.transactionId} of {money(done.amount)}. (The balance above was re-fetched, not calculated.)
        </p>
      )}
    </form>
  );
}

// Detail + form together: the parent owns `version`; changing it makes AccountDetail ask the API again.
export function AccountScreen({ id, onBack }) {
  const [version, setVersion] = useState(0); // STATE: "how many times data changed"
  return (
    <div>
      <AccountDetail id={id} version={version} onBack={onBack} />
      <MoneyForm accountId={id} onDone={() => setVersion((v) => v + 1)} />
    </div>
  );
}

export default function Lesson() {
  useFreshCallLog(); // the Journey panel starts empty for this lesson
  const [openId, setOpenId] = useState(null);
  return (
    <div>
      <h2>Step 3: deposit and withdraw</h2>
      {openId === null ? <AccountsList onOpen={setOpenId} /> : <AccountScreen id={openId} onBack={() => setOpenId(null)} />}
      <Journey
        shows={[
          ['the typed kind and amount', `${STATE} (the form's own state)`],
          ['the new balance and transaction list', `${FROM_API} <- SQL Server, fetched AGAIN after the POST`],
          ['"Insufficient funds" message', 'the API (422 problem+json); the rule lives on the server'],
        ]}
      />
    </div>
  );
}
