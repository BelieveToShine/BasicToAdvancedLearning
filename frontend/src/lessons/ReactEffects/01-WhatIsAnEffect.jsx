/*
 * LESSON 1 — What is an effect?
 *
 * What this shows
 *   A component's main job is to turn props + state into JSX. That work is called RENDERING and it
 *   must only CALCULATE: given the same props and state, it should return the same JSX.
 *   Sometimes a component also has to touch something OUTSIDE React: the browser tab title, a timer,
 *   a network request. That kind of work is a "side effect", and useEffect is the place for it.
 *
 *   useEffect(() => { ...do the outside thing... })
 *
 *   React calls your effect function AFTER it has rendered and updated the DOM (the "commit").
 *   So when an effect runs, the DOM already shows the new output. The browser has usually, but not
 *   always, painted it by then; useLayoutEffect exists for work that must happen before paint. The test file proves it:
 *   the effect reads the page and sees the NEW balance, not the old one.
 *
 * Why it matters
 *   - Render must be pure: React may call your component more than once, or throw a render away, so
 *     outside work done during render could be repeated or happen for output nobody sees.
 *   - The opposite mistake is just as common: using an effect to CALCULATE a value you could work out
 *     during render. That costs an extra render for nothing (see FeeRight / FeeWrong below).
 *
 * Rule of thumb
 *   "Can I work this out from props/state right now?"  -> just calculate it in the component body.
 *   "Do I have to synchronise with something outside React?" -> useEffect.
 */
import { useEffect, useState } from 'react';

export const title = 'Effects 1 — What is an effect?';

// An effect that talks to the browser (document.title lives outside React).
export function BalanceTitle() {
  const [balance, setBalance] = useState(100);
  const [seenByEffect, setSeenByEffect] = useState('(effect has not run yet)');
  const [tabTitle, setTabTitle] = useState(null);

  // Calculated during render: no effect needed for this.
  const label = `Balance: ${balance}`;

  useEffect(() => {
    // 1) The outside thing: the browser tab title.
    document.title = label;
    // 2) Proof of timing: by the time this runs, the page already shows the new text.
    setSeenByEffect(document.getElementById('balance-text').textContent);
  }, [label]);

  return (
    <section>
      <h3>An effect that updates the browser tab title</h3>
      <p>
        On the page: <strong id="balance-text">{label}</strong>
      </p>
      <p>When my effect ran, the page already said: "{seenByEffect}"</p>
      <button onClick={() => setBalance((b) => b + 50)}>Deposit 50</button>{' '}
      <button onClick={() => setTabTitle(document.title)}>Read the tab title</button>
      {tabTitle !== null && <p>document.title is: "{tabTitle}"</p>}
    </section>
  );
}

// RIGHT: the fee can be calculated from the balance while rendering. One render per change.
export function FeeRight({ balance }) {
  const fee = balance * 0.01;
  return <p>Fee (calculated while rendering): {fee.toFixed(2)}</p>;
}

// WRONG (shown only to compare): copying a calculated value into state with an effect.
// React renders once with the OLD fee, runs the effect, then renders AGAIN with the new fee.
export function FeeWrong({ balance }) {
  const [fee, setFee] = useState(balance * 0.01);
  useEffect(() => {
    setFee(balance * 0.01);
  }, [balance]);
  return <p>Fee (copied into state by an effect): {fee.toFixed(2)}</p>;
}

export default function Lesson() {
  const [balance, setBalance] = useState(2000);
  return (
    <div>
      <h2>Effects 1 — What is an effect?</h2>
      <p>
        Rendering only calculates. An <em>effect</em> runs after React has updated the DOM, for work that
        lives outside React.
      </p>
      <BalanceTitle />
      <section>
        <h3>Calculate during render, do not use an effect for it</h3>
        <p>Account balance: {balance}</p>
        <button onClick={() => setBalance((b) => b + 500)}>Deposit 500</button>
        <FeeRight balance={balance} />
        <FeeWrong balance={balance} />
        <p>Both lines show the same number, but the second one needed an extra render to get there.</p>
      </section>
    </div>
  );
}
