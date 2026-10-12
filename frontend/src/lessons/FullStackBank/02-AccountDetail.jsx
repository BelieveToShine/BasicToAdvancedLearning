/*
 * Mini project, step 2 - one account: its details and its transactions, newest first.
 *
 * Two independent requests, two independent pieces of state:
 *   GET /api/accounts/{id}               -> the account (owner, number, type, balance)
 *   GET /api/accounts/{id}/transactions  -> its transactions
 * Each has its own loading / error / ok, so one failing does not hide the other.
 *
 * "Newest first" is decided by the API (ORDER BY TransactionId DESC in the SQL). We do NOT sort in
 * React: there must be one place that owns the rule, and the database is better at it. We only
 * display the list in the order we received it.
 *
 * `id` is a prop (owned by the parent: the screen that decided which account to show). When the id
 * changes, the effects run again (the deps list in useLoad). `version` is a second reason to
 * re-fetch: step 3 bumps it after a deposit, so the screen asks the API for the NEW balance instead
 * of calculating it itself. The API is the single source of truth for money.
 *
 * An id that does not exist gives a real 404 from the API; ErrorView prints its title and detail.
 */
import { useState } from 'react';
import { api, money, utc } from './api.js';
import { ErrorView, Journey, useLoad, STATE, FROM_API, useFreshCallLog } from './parts.jsx';
import { AccountsList } from './01-AccountsList.jsx';

export const title = '2. Account detail + transactions (newest first)';

export function AccountDetail({ id, version = 0, onBack }) {
  const [account, reloadAccount] = useLoad(() => api.getAccount(id), [id, version]);
  const [txns, reloadTxns] = useLoad(() => api.listTransactions(id), [id, version]);

  return (
    <div>
      {onBack && <button onClick={onBack}>Back to the list</button>}
      <h3>Account {id}</h3>
      {account.status === 'loading' && <p>Loading account...</p>}
      {account.status === 'error' && <ErrorView error={account.error} onRetry={reloadAccount} />}
      {account.status === 'ok' && (
        <p data-testid="account-summary">
          <strong>{account.data.customerName}</strong> - {account.data.accountNumber} ({account.data.accountType}) - balance{' '}
          <strong data-testid="balance">{money(account.data.balance)}</strong>
        </p>
      )}

      <h4>Transactions (newest first)</h4>
      {txns.status === 'loading' && <p>Loading transactions...</p>}
      {txns.status === 'error' && <ErrorView error={txns.error} onRetry={reloadTxns} />}
      {txns.status === 'ok' && txns.data.length === 0 && <p>No transactions yet.</p>}
      {txns.status === 'ok' && txns.data.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>#</th>
              <th>Kind</th>
              <th>Amount</th>
              <th>When</th>
            </tr>
          </thead>
          <tbody data-testid="txn-rows">
            {txns.data.map((t) => (
              <tr key={t.transactionId}>
                <td>{t.transactionId}</td>
                <td>{t.kind}</td>
                <td style={{ textAlign: 'right' }}>{money(t.amount)}</td>
                <td>{utc(t.createdAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

export default function Lesson() {
  useFreshCallLog(); // the Journey panel starts empty for this lesson
  // STATE: which account is open (null = show the list). A plain "screen" variable, no router needed yet.
  const [openId, setOpenId] = useState(null);
  return (
    <div>
      <h2>Step 2: account detail</h2>
      {openId === null ? <AccountsList onOpen={setOpenId} /> : <AccountDetail id={openId} onBack={() => setOpenId(null)} />}
      {openId === null && (
        <p>
          <button onClick={() => setOpenId(9999)}>Open account 9999 (does not exist: watch the real 404)</button>
        </p>
      )}
      <Journey
        shows={[
          ['which account is open', `${STATE} of the parent (a prop for the detail)`],
          ['owner, number, type, balance', `${FROM_API} <- table Accounts`],
          ['transaction rows and their order', `${FROM_API} <- table Transactions, sorted by the API`],
        ]}
      />
    </div>
  );
}
