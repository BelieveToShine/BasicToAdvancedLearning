/*
 * Mini project, step 4 - open a new account: POST /api/accounts.
 *
 * Body:  { "customerId": 1, "accountNumber": "ACC-2001", "accountType": "Current" }
 * Answer: 201 Created and the new account (it starts with balance 0; money arrives later as transactions).
 *
 * What is new compared with step 3:
 *   - the answer is the new RESOURCE (an account with its server-generated accountId), so we can
 *     go straight to its detail screen using that id;
 *   - the interesting failure is 409 Conflict: the account number must be unique, and the database
 *     enforces it (a UNIQUE index). Open ACC-1001 again to see the real 409 in the page;
 *   - an unknown customer gives 400 with errors.customerId (the body names a customer that does not exist).
 *
 * The API has no "list customers" endpoint, so the customer is typed as a number. The seed data is
 * 1 Asha Rao, 2 Ben Carter, 3 Chen Li. (A real screen would have a customers endpoint and a dropdown.)
 */
import { useState } from 'react';
import { api, money } from './api.js';
import { ErrorView, Journey, STATE, useFreshCallLog } from './parts.jsx';
import { AccountsList } from './01-AccountsList.jsx';

export const title = '4. Open an account (POST /api/accounts -> 201, 409 duplicate)';

export function OpenAccountForm({ onCreated }) {
  const [customerId, setCustomerId] = useState('1');
  const [accountNumber, setAccountNumber] = useState('');
  const [accountType, setAccountType] = useState('Savings');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(null);

  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const account = await api.openAccount(Number(customerId), accountNumber, accountType);
      onCreated?.(account); // the new account, as the API stored it
    } catch (e) {
      setError(e);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={submit} aria-label="open account form">
      <h4>Open an account</h4>
      <p>
        <label>
          Customer id{' '}
          <input type="number" value={customerId} onChange={(e) => setCustomerId(e.target.value)} style={{ width: 60 }} />
        </label>{' '}
        <small>(seed data: 1 Asha Rao, 2 Ben Carter, 3 Chen Li)</small>
      </p>
      <p>
        <label>
          Account number <input value={accountNumber} onChange={(e) => setAccountNumber(e.target.value)} />
        </label>
      </p>
      <p>
        <label>
          Type{' '}
          <select value={accountType} onChange={(e) => setAccountType(e.target.value)}>
            <option>Savings</option>
            <option>Current</option>
          </select>
        </label>
      </p>
      <button type="submit" disabled={busy}>
        {busy ? 'Sending...' : 'Open account'}
      </button>
      {error && <ErrorView error={error} />}
    </form>
  );
}

export default function Lesson() {
  useFreshCallLog(); // the Journey panel starts empty for this lesson
  const [screen, setScreen] = useState('list'); // STATE: 'list' | 'form'
  const [created, setCreated] = useState(null); // STATE: the account we just opened (the 201 body)
  return (
    <div>
      <h2>Step 4: open an account</h2>
      {screen === 'list' ? (
        <>
          {created && (
            <p role="status">
              Opened account <strong>{created.accountNumber}</strong> (accountId {created.accountId}, balance {money(created.balance)}). The list below
              was fetched again, so it includes it.
            </p>
          )}
          <p>
            <button onClick={() => setScreen('form')}>Open a new account</button>
          </p>
          <AccountsList />
        </>
      ) : (
        <>
          <OpenAccountForm
            onCreated={(a) => {
              setCreated(a);
              setScreen('list'); // the list mounts again and fetches again
            }}
          />
          <p>
            <button onClick={() => setScreen('list')}>Cancel</button>
          </p>
        </>
      )}
      <Journey
        shows={[
          ['which screen is showing', STATE],
          ['the typed customer id, number, type', `${STATE} (the form's own state)`],
          ['accountId of the new account', 'created by SQL Server (IDENTITY) and returned in the 201 body'],
          ['"duplicate" message', 'the API (409 problem+json) after the UNIQUE index refused the INSERT'],
        ]}
      />
    </div>
  );
}
