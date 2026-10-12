/*
 * 01 - useState: a value the component remembers.
 *
 * WHAT THIS SHOWS
 *   A React component is just a function, and React calls that function again every time something
 *   changes (this is called "rendering"). Every call starts from scratch: ordinary variables declared
 *   inside the function are created again with their starting value.
 *   `useState` is how a component keeps a value BETWEEN those calls. React stores the value for you,
 *   and hands it back on the next call.
 *
 * WHY IT MATTERS
 *   If you keep a bank balance in an ordinary variable, it silently resets to its starting value on
 *   the next render. If you keep it in state, it survives, and calling the setter also tells React
 *   "something changed, render again so the screen is updated".
 *
 * TWO THINGS TO TRY ON THE PAGE
 *   1. Press "Deposit 10". The state balance goes 100 -> 110 -> 120 ... but the ordinary variable
 *      is back at 100 every time, because the function ran again from the top.
 *   2. Change the opening balance in the parent. The child's state does NOT follow, because the
 *      value passed to useState(...) is only used on the very first render of that component.
 */
import { useState } from 'react';

export const title = 'useState: a value that survives re-renders';

const STARTING_BALANCE = 100;

function BalanceCard() {
  // State: React remembers this between renders. STARTING_BALANCE is used only the first time.
  const [balance, setBalance] = useState(STARTING_BALANCE);

  // Ordinary variable: created fresh on EVERY call of this function.
  let ordinaryBalance = STARTING_BALANCE;
  const [lastCall, setLastCall] = useState(null); // what the ordinary variable held at the end of the last click

  function deposit() {
    ordinaryBalance = ordinaryBalance + 10; // changes only this one call's variable
    setLastCall(ordinaryBalance);
    setBalance(balance + 10); // asks React to remember the new value and render again
  }

  return (
    <div style={{ border: '1px solid #999', padding: 12, marginBottom: 12 }}>
      <p>
        State balance: <strong data-testid="state-balance">{balance}</strong>
      </p>
      <p>
        Ordinary variable: <strong data-testid="ordinary-balance">{ordinaryBalance}</strong>
        {' '}(back to its starting value on every render)
        {lastCall !== null && <> (after += 10 inside the last click: {lastCall})</>}
      </p>
      <button onClick={deposit}>Deposit 10</button>
    </div>
  );
}

// The child gets its opening balance as a prop, but only reads it as the INITIAL state.
function Account({ openingBalance }) {
  const [balance, setBalance] = useState(openingBalance);
  return (
    <div style={{ border: '1px solid #999', padding: 12 }}>
      <p>
        Prop openingBalance: <strong data-testid="prop-value">{openingBalance}</strong>
      </p>
      <p>
        State balance: <strong data-testid="initial-state-balance">{balance}</strong>
      </p>
      <button onClick={() => setBalance(balance + 1)}>Deposit 1</button>
    </div>
  );
}

export default function Lesson() {
  const [opening, setOpening] = useState(100);
  return (
    <section>
      <h1>{title}</h1>
      <h2>1. State survives, ordinary variables do not</h2>
      <BalanceCard />
      <h2>2. The initial value is only used on the first render</h2>
      <button onClick={() => setOpening(opening + 400)}>Change opening balance (parent)</button>
      <Account openingBalance={opening} />
    </section>
  );
}
