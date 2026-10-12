/*
 * 03 - How state updates really work.
 *
 * THREE FACTS, EACH SHOWN BY A BUTTON BELOW
 *
 * 1. Calling setBalance(...) does NOT change the `balance` variable in the render that is running.
 *    That render's `balance` is a fixed snapshot. The new value shows up in the NEXT render, which
 *    React schedules. So reading `balance` on the line after setBalance gives the OLD value.
 *
 * 2. If the new value depends on the old one, pass a function: setBalance(b => b + 10).
 *    React calls it with the latest pending value. Writing setBalance(balance + 10) three times
 *    uses the same snapshot three times, so the result is only +10, not +30.
 *
 * 3. Batching: several set calls inside one event handler are grouped into ONE re-render.
 *    React renders once after the handler finishes, not after each call.
 */
import { useState } from 'react';

export const title = 'State updates: snapshots, functional updates, batching';

export default function Lesson() {
  const [balance, setBalance] = useState(100);
  const [log, setLog] = useState([]);

  function addToLog(text) {
    setLog((previous) => [...previous, text]);
  }

  // Fact 1: the variable is a snapshot for this render.
  function depositAndLook() {
    setBalance(balance + 10);
    addToLog(`just after setBalance(balance + 10), balance is still ${balance}`);
  }

  // Fact 2, wrong way: three updates all start from the same snapshot.
  function depositThreeTimesWrong() {
    setBalance(balance + 10);
    setBalance(balance + 10);
    setBalance(balance + 10);
  }

  // Fact 2, right way: each update receives the result of the one before it.
  function depositThreeTimesRight() {
    setBalance((current) => current + 10);
    setBalance((current) => current + 10);
    setBalance((current) => current + 10);
  }

  return (
    <section>
      <h1>{title}</h1>
      <p>
        Balance: <strong data-testid="balance">{balance}</strong>
      </p>
      <p>
        <button onClick={depositAndLook}>1. Deposit 10 and look at balance</button>
      </p>
      <p>
        <button onClick={depositThreeTimesWrong}>2a. setBalance(balance + 10) x3</button>{' '}
        <button onClick={depositThreeTimesRight}>2b. setBalance(b =&gt; b + 10) x3</button>
      </p>
      <p>
        Fact 3: each button above calls the setter more than once (2a and 2b three times), yet React
        renders once per click, after the handler finishes.
      </p>
      <h2>What the handler saw</h2>
      <ul data-testid="log">
        {log.map((line, index) => (
          <li key={index}>{line}</li>
        ))}
      </ul>
    </section>
  );
}
