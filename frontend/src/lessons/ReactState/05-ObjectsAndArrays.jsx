/*
 * 05 - Objects and arrays in state: never change them, replace them.
 *
 * React decides whether to re-render by checking whether the state value is a DIFFERENT value
 * (Object.is). For an object or array that means a different reference, i.e. a new copy.
 *   accounts[0].balance += 10   edits the old object in place. The reference is the same, so React
 *                               is told the SAME array, sees no difference, and does not re-render.
 *   setAccounts(accounts.map(...))  builds a new array (and new objects where something changed)
 *                               and hands that to React. Re-render, screen updated.
 *
 * COPY RECIPES USED BELOW
 *   update an object:   { ...account, balance: account.balance + 10 }
 *   add to an array:    [...accounts, newAccount]
 *   remove from array:  accounts.filter((a) => a.id !== id)
 *   update one item:    accounts.map((a) => (a.id === id ? { ...a, balance: ... } : a))
 *
 * LIFTING STATE UP: AccountList and TotalBalance are siblings and both need the accounts. Siblings
 * cannot read each other's state, so the accounts live in their common parent (Bank), which passes
 * the data and the functions to change it down as props.
 */
import { useState } from 'react';

export const title = 'Objects and arrays: copy, never mutate; lifting state up';

const START = [
  { id: 1, owner: 'Asha Rao', balance: 100 },
  { id: 2, owner: 'Ben Carter', balance: 250 },
];

function AccountList({ accounts, onDeposit, onRemove }) {
  return (
    <ul>
      {accounts.map((account) => (
        <li key={account.id}>
          {account.owner}: <span data-testid={`balance-${account.id}`}>{account.balance}</span>{' '}
          <button onClick={() => onDeposit(account.id)}>Deposit 10 to {account.owner}</button>{' '}
          <button onClick={() => onRemove(account.id)}>Remove {account.owner}</button>
        </li>
      ))}
    </ul>
  );
}

function TotalBalance({ accounts }) {
  const total = accounts.reduce((sum, account) => sum + account.balance, 0); // derived
  return (
    <p>
      Total across all accounts: <strong data-testid="total">{total}</strong>
    </p>
  );
}

export default function Lesson() {
  // Lifted state: the parent owns it; both siblings below use it.
  // A fresh copy of START for each mounted lesson, so the wrong-way mutation below cannot leak into START.
  const [accounts, setAccounts] = useState(() => START.map((a) => ({ ...a })));
  const [rerenders, setRerenders] = useState(0);

  function deposit(id) {
    setAccounts(accounts.map((a) => (a.id === id ? { ...a, balance: a.balance + 10 } : a)));
  }
  function remove(id) {
    setAccounts(accounts.filter((a) => a.id !== id));
  }
  function addAccount() {
    const nextId = accounts.reduce((max, a) => Math.max(max, a.id), 0) + 1;
    setAccounts([...accounts, { id: nextId, owner: `New customer ${nextId}`, balance: 0 }]);
  }

  // The WRONG way, kept here on purpose so you can see what happens.
  function mutateWrong() {
    if (accounts.length === 0) return;
    accounts[0].balance += 10; // edits the existing object in place
    setAccounts(accounts); // the same array reference: React sees "no change"
  }

  return (
    <section>
      <h1>{title}</h1>
      <AccountList accounts={accounts} onDeposit={deposit} onRemove={remove} />
      <TotalBalance accounts={accounts} />
      <p>
        <button onClick={addAccount}>Add account</button>{' '}
        <button onClick={mutateWrong}>Mutate first account (wrong way)</button>{' '}
        <button onClick={() => setRerenders(rerenders + 1)}>Unrelated re-render ({rerenders})</button>
      </p>
      <p>
        The wrong-way button changes the data in memory but the screen does not update. Press
        &quot;Unrelated re-render&quot;: that render reads the already-edited object, so the new number
        appears late. Edits made behind React's back make the screen untrustworthy.
      </p>
    </section>
  );
}
