/*
 * PROPS — the inputs a parent hands to a component.
 *
 * WHAT THIS SHOWS
 *   Props are the attributes you write on a component tag. React collects them into one object and
 *   passes it as the function's only argument:
 *       <AccountCard owner="Asha Rao" balance={454.5} />   ->   AccountCard({ owner: 'Asha Rao', balance: 454.5 })
 *
 * FIVE THINGS TO KNOW (each is proved in ReactFundamentals.test.jsx)
 *   1. Props are READ-ONLY. The parent owns that data; a child must never change it
 *      (no `props.balance = 0`). In development builds React freezes the props object, so trying to
 *      assign to it throws an error. If a child needs something different, it makes a new value
 *      (`const shown = balance * 2`) or asks the parent to change it (see 5).
 *   2. Destructuring in the parameter - function AccountCard({ owner, balance }) - is just a shorter
 *      way to write props.owner / props.balance. Nothing magical.
 *   3. Default values: `type = 'Savings'` is used when the parent leaves the prop out (or passes
 *      undefined). It is NOT used when the parent passes null: null is a real value, so it stays null.
 *   4. `children` is the special prop holding whatever is written BETWEEN the tags:
 *      <Panel title="Notes"> ...this... </Panel>. It lets a component be a reusable frame.
 *   5. Data flows DOWN (parent -> child). To let a child tell the parent something, the parent passes
 *      a FUNCTION as a prop and the child calls it. Below, onView is such a function. How the parent
 *      then remembers the answer is "state", which is Topic 22 - here it is only a preview, so treat
 *      the useState line as "a variable that makes the page redraw when it changes".
 */
import { useState } from 'react';

export function AccountCard({ owner, number, balance, type = 'Savings', onView, children }) {
  return (
    <div style={{ border: '1px solid #888', borderRadius: 6, padding: 8, margin: 4, width: 260 }}>
      <strong>{owner}</strong> <em>({type})</em>
      <div>Account {number}</div>
      <div>Balance {balance.toFixed(2)}</div>
      {children}
      {/* Point 5: the child calls the function the parent gave it, passing its own account number. */}
      {onView && <button onClick={() => onView(number)}>View {number}</button>}
    </div>
  );
}

// A generic frame: it does not know what it will contain. That is what `children` is for.
export function Panel({ heading, children }) {
  return (
    <section style={{ background: '#f3f6fb', padding: 8, margin: '8px 0' }}>
      <h4 style={{ margin: 0 }}>{heading}</h4>
      {children}
    </section>
  );
}

export const title = '03 Props - read-only inputs from the parent';

export default function Lesson() {
  const [viewed, setViewed] = useState('nothing yet'); // preview of Topic 22
  return (
    <div>
      <h2>Props</h2>
      <Panel heading="Three cards, three different sets of props">
        {/* No type given: the default 'Savings' is used. */}
        <AccountCard owner="Asha Rao" number="SAV-1001" balance={454.5} onView={setViewed} />
        <AccountCard owner="Ben Carter" number="CUR-2001" balance={1200} type="Current" onView={setViewed} />
        {/* Text between the tags arrives as props.children. */}
        <AccountCard owner="Chen Li" number="SAV-3001" balance={0}>
          <p style={{ margin: '4px 0' }}>Note: no deposits yet.</p>
        </AccountCard>
      </Panel>
      <p data-testid="viewed">
        The parent last heard: <strong>{viewed}</strong>
      </p>
    </div>
  );
}
