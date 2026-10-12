/*
 * 03 - Business-rule errors (422) and conflicts (409): a message for the WHOLE form, not for one field.
 *
 * Lesson 02 showed 400: "something in your request is invalid", with a message per field. Two other
 * answers are different in kind (rules from FullStackBank/README.md, "422 vs 409"):
 *
 *   400  The request breaks a rule that can be checked from the request alone (amount <= 0, unknown kind).
 *        -> a message next to the input, the user edits that input.
 *   422  The request is valid and the account exists, but a BUSINESS RULE refuses it: "no overdraft".
 *        Whether 50.00 is allowed depends on the balance, which is not in the request.
 *        -> a message for the whole form (the amount field is not "wrong" in itself).
 *   409  The request clashes with data that already exists (or changed at the same moment): a duplicate
 *        account number, deleting an account that has history, two requests changing one account at once.
 *        -> a message for the whole form, usually with a suggestion ("choose another number").
 *
 * Other APIs return 409 for insufficient funds; both choices are defensible. What matters is to pick one
 * rule and use it consistently, so the front end can rely on it. THIS API's rule is the one above.
 *
 * The screen decides where a message goes by classify() (bankApi.jsx), i.e. by the status code, never by
 * reading the English text. Everything attempted on this page is refused by the API, so no data changes
 * (the delete button first checks that the account really has transactions).
 */
import { useState } from 'react';
import { ApiDown, bad, box, callApi, classify, friendlyMessage, money, splitProblemErrors, useAccounts } from './bankApi.jsx';

export const title = '3. Business-rule errors: 422 and 409';

const FIELDS = ['kind', 'amount'];
const LABELS = { business: 'Business rule (422)', conflict: 'Conflict (409)', notFound: 'Not found (404)' };

// The form-level message: one box above the form, not tied to an input.
export function FormMessage({ result }) {
  const kind = classify(result);
  const p = result.problem;
  return (
    <div role="alert" data-testid="form-message" style={{ ...bad, border: '1px solid #cc0000', background: '#fdecea', padding: 10 }}>
      <strong>{LABELS[kind] ?? 'The request failed'}</strong>
      {p ? (
        <>
          <br />
          {p.title}
          {p.detail ? ` - ${p.detail}` : ''}
        </>
      ) : (
        <>
          <br />
          {friendlyMessage(result)}
        </>
      )}
    </div>
  );
}

export default function Lesson() {
  const { status, accounts, reload } = useAccounts();
  const [chosen, setChosen] = useState(null); // account id picked by the user; null = the first one
  const [form, setForm] = useState({ kind: 'Withdrawal', amount: '' });
  const [result, setResult] = useState(null);
  const [note, setNote] = useState('');

  if (status === 'loading') return <p>Loading accounts...</p>;
  if (status === 'down') return <ApiDown onRetry={reload} />;
  if (status !== 'ready' || accounts.length === 0) return <p role="alert">No account to use. Seed data: see FullStackBank/README.md.</p>;

  const account = accounts.find((a) => a.accountId === chosen) ?? accounts[0];

  async function submit(event) {
    event.preventDefault();
    setNote('');
    const answer = await callApi('POST', `/api/accounts/${account.accountId}/transactions`, {
      kind: form.kind,
      amount: form.amount.trim() === '' ? undefined : Number(form.amount),
    });
    setResult(answer);
    if (answer.ok) reload(); // the balance changed: read the accounts again
  }

  async function tryDuplicate() {
    setNote('');
    setResult(await callApi('POST', '/api/accounts', { customerId: 1, accountNumber: account.accountNumber, accountType: 'Savings' }));
  }

  async function tryDeleteWithHistory() {
    setNote('');
    // Safety check: only send the DELETE when the account has history, otherwise it would really be deleted.
    const history = await callApi('GET', `/api/accounts/${account.accountId}/transactions`);
    if (!history.ok || history.data.length === 0) {
      setResult(null);
      setNote(`Account ${account.accountId} has no transactions, so a DELETE would succeed and really remove it. Pick an account with history.`);
      return;
    }
    setResult(await callApi('DELETE', `/api/accounts/${account.accountId}`));
  }

  const kind = result ? classify(result) : null;
  const split = kind === 'field' ? splitProblemErrors(result.problem, FIELDS) : { byField: {}, general: [] };
  const tooMuch = money(Number(account.balance) + 1);

  return (
    <section>
      <h1>{title}</h1>
      <p>
        Account:{' '}
        <select aria-label="Account" value={account.accountId} onChange={(e) => setChosen(Number(e.target.value))}>
          {accounts.map((a) => (
            <option key={a.accountId} value={a.accountId}>
              {a.accountNumber} - {a.customerName} - balance {money(a.balance)}
            </option>
          ))}
        </select>
      </p>

      <h2>A form that can fail three different ways</h2>
      <p>
        Try:{' '}
        <button type="button" onClick={() => setForm({ kind: 'Withdrawal', amount: tooMuch })}>
          withdraw {tooMuch} (1.00 more than the balance)
        </button>{' '}
        <button type="button" onClick={() => setForm({ kind: 'Withdrawal', amount: '-5' })}>
          withdraw -5
        </button>
      </p>
      <form onSubmit={submit} noValidate style={box}>
        {result && kind !== 'field' && kind !== 'ok' && <FormMessage result={result} />}
        {kind === 'ok' && <p role="status">Accepted: {result.data.kind} of {money(result.data.amount)}.</p>}
        {split.general.length > 0 && <p role="alert" style={bad}>{split.general.join(' ')}</p>}
        <p>
          <label htmlFor="kind">Kind</label>
          <br />
          <select
            id="kind"
            value={form.kind}
            onChange={(e) => setForm({ ...form, kind: e.target.value })}
            aria-invalid={split.byField.kind ? 'true' : undefined}
            aria-describedby={split.byField.kind ? 'kind-error' : undefined}
          >
            <option>Withdrawal</option>
            <option>Deposit</option>
          </select>
          {split.byField.kind && <span id="kind-error" style={{ ...bad, display: 'block' }}>{split.byField.kind.join(' ')}</span>}
        </p>
        <p>
          <label htmlFor="amount">Amount</label>
          <br />
          <input
            id="amount"
            value={form.amount}
            onChange={(e) => setForm({ ...form, amount: e.target.value })}
            aria-invalid={split.byField.amount ? 'true' : undefined}
            aria-describedby={split.byField.amount ? 'amount-error' : undefined}
            style={split.byField.amount ? { borderColor: '#cc0000', borderWidth: 2 } : undefined}
          />
          {split.byField.amount && <span id="amount-error" style={{ ...bad, display: 'block' }}>{split.byField.amount.join(' ')}</span>}
        </p>
        <button type="submit">Send</button>
      </form>

      <h2>Conflicts (409): not about a field either</h2>
      <p>
        <button type="button" onClick={tryDuplicate}>Open an account with the number {account.accountNumber} (already taken)</button>{' '}
        <button type="button" onClick={tryDeleteWithHistory}>Delete account {account.accountId} (if it has transactions)</button>
      </p>
      {note && <p role="status">{note}</p>}

      <h2>Which status for which problem (in this API)</h2>
      <table border="1" cellPadding="6" style={{ borderCollapse: 'collapse' }}>
        <thead>
          <tr><th>Status</th><th>Meaning</th><th>Where the message goes</th><th>Example above</th></tr>
        </thead>
        <tbody>
          <tr><td>400</td><td>the request itself is invalid</td><td>next to the input</td><td>amount -5</td></tr>
          <tr><td>422</td><td>valid request, a business rule says no</td><td>whole form</td><td>withdraw more than the balance</td></tr>
          <tr><td>409</td><td>clashes with existing data or a simultaneous change</td><td>whole form</td><td>duplicate number; delete with history</td></tr>
        </tbody>
      </table>
    </section>
  );
}
