/*
 * CONDITIONAL RENDERING — showing different JSX depending on the data.
 *
 * WHAT THIS SHOWS
 *   JSX has no special `if` tag. You use ordinary JavaScript to decide what to put in the markup.
 *   Four everyday tools:
 *     1. condition && <X />      "show X only when condition is true"
 *     2. cond ? <A /> : <B />    "show A or B" (the ternary operator)
 *     3. return null;            a component can decide to show nothing at all
 *     4. a normal `if` before the return (it works there because it is not inside the JSX)
 *
 *   What React draws for each kind of value: true, false, null and undefined draw NOTHING;
 *   strings and numbers are drawn as text.
 *
 * THE `0 &&` GOTCHA
 *   `a && b` gives back `a` when `a` is falsy. For a boolean that is `false`, which draws nothing - fine.
 *   But the number 0 is also falsy, and React DOES draw the number 0. So
 *       {transactions.length && <ul>...</ul>}
 *   draws a stray "0" on the page when the list is empty. Make the condition a real boolean:
 *       {transactions.length > 0 && <ul>...</ul>}   or use a ternary.
 *   Below, the same empty account is drawn with the bug and with the fix.
 *
 * (The two buttons at the bottom use state, which is Topic 22 - treat useState as "a variable that
 *  makes the page redraw when it changes".)
 */
import { useState } from 'react';

export function TransactionList({ transactions, buggy = false }) {
  return (
    <div>
      {/* Tool 1 with a real boolean: false draws nothing. */}
      {transactions.length > 5 && <p>Showing a long list.</p>}
      {buggy
        ? // THE BUG: transactions.length is the number 0 when the list is empty, and React draws it.
          transactions.length && (
            <ul>
              {transactions.map((t) => (
                <li key={t.id}>{t.kind} {t.amount.toFixed(2)}</li>
              ))}
            </ul>
          )
        : // Tool 2: the ternary gives an explicit message for the empty case.
          transactions.length > 0 ? (
            <ul>
              {transactions.map((t) => (
                <li key={t.id}>{t.kind} {t.amount.toFixed(2)}</li>
              ))}
            </ul>
          ) : (
            <p>No transactions yet.</p>
          )}
    </div>
  );
}

// Tool 1 again: a badge that exists only when the balance is below zero.
// Tool 3: with no account at all there is nothing to draw, so return null.
export function BalanceLine({ account }) {
  if (!account) return null;
  return (
    <p>
      {account.owner}: {account.balance.toFixed(2)} {account.balance < 0 && <strong style={{ color: 'crimson' }}>OVERDRAWN</strong>}
    </p>
  );
}

export const title = '05 Conditional rendering - &&, ternary, null and the 0 gotcha';

const sample = [
  { id: 1, kind: 'Deposit', amount: 500 },
  { id: 2, kind: 'Withdrawal', amount: 120.5 },
];

export default function Lesson() {
  const [list, setList] = useState(sample); // preview of Topic 22
  return (
    <div>
      <h2>Conditional rendering</h2>
      <button onClick={() => setList([])}>Empty the account</button>{' '}
      <button onClick={() => setList(sample)}>Put the transactions back</button>
      <div style={{ display: 'flex', gap: 32 }}>
        <div data-testid="buggy-panel">
          <h3>length && ... (the bug)</h3>
          <TransactionList transactions={list} buggy />
        </div>
        <div data-testid="fixed-panel">
          <h3>length &gt; 0 ? ... : ... (fixed)</h3>
          <TransactionList transactions={list} />
        </div>
      </div>
      <h3>Badges and null</h3>
      <BalanceLine account={{ owner: 'Asha Rao', balance: 454.5 }} />
      <BalanceLine account={{ owner: 'Chen Li', balance: -20 }} />
      <BalanceLine account={null} />
      <p>(The third line above is a BalanceLine given no account: it returned null, so nothing is drawn.)</p>
    </div>
  );
}
