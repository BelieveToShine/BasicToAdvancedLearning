/*
 * LESSON 2 — The dependency array
 *
 * What this shows
 *   The second argument of useEffect tells React WHEN to run the effect again:
 *
 *     useEffect(fn)            no array   -> after EVERY render
 *     useEffect(fn, [])        empty      -> after the first render only (on mount)
 *     useEffect(fn, [owner])   with values -> after the first render, then again only when
 *                                            `owner` is different from last render
 *
 *   React compares each dependency with the value from the previous render (using Object.is).
 *   Rule: list every reactive value the effect READS (props, state, anything declared in the component body). If you leave one out, the effect keeps using
 *   the value it saw when it was created — a "stale value". StaleTicker below shows the bug and
 *   FixedTicker the fix.
 *
 * Why it matters
 *   No array can repeat expensive work (or loop forever if the effect sets state). A wrong array
 *   leaves the screen out of date. The tests count how often each kind of effect really runs.
 *
 * Note for the browser page: this app runs in StrictMode while you develop, which on purpose runs
 * each effect an extra time on mount (lesson 3 explains and proves it): ALL THREE effects run twice
 * on mount (measured: no array 3, [] 2, [owner] 2, instead of 2, 1, 1). The tests run without
 * StrictMode and show the exact numbers; the page says the same in a visible note.
 */
import { useEffect, useRef, useState } from 'react';

export const title = 'Effects 2 — Dependencies';

export function Probe({ owner }) {
  const [transactions, setTransactions] = useState(0);
  const [snapshot, setSnapshot] = useState(null);
  const runs = useRef({ everyRender: 0, onceOnMount: 0, whenOwnerChanges: 0 });

  // No array: runs after every render.
  useEffect(() => {
    runs.current.everyRender += 1;
  });

  // Empty array: runs after the first render only.
  useEffect(() => {
    runs.current.onceOnMount += 1;
  }, []);

  // [owner]: runs after the first render and again whenever owner changes.
  useEffect(() => {
    runs.current.whenOwnerChanges += 1;
  }, [owner]);

  return (
    <section>
      <h3>How often does each kind of effect run?</h3>
      <p>Account owner: {owner}. Transactions added: {transactions}</p>
      <button onClick={() => setTransactions((t) => t + 1)}>Add a transaction (changes state, not owner)</button>{' '}
      <button onClick={() => setSnapshot({ ...runs.current })}>Show the run counts</button>
      {snapshot && (
        <ul aria-label="run counts">
          <li>no array (every render): {snapshot.everyRender}</li>
          <li>[] (once on mount): {snapshot.onceOnMount}</li>
          <li>[owner] (when owner changes): {snapshot.whenOwnerChanges}</li>
        </ul>
      )}
      {snapshot && <p>(Counts as of the moment you clicked.)</p>}
    </section>
  );
}

// BUG: the effect reads `count` but the array is []. The effect was created when count was 0,
// so every tick does setCount(0 + 1) and the number gets stuck at 1.
export function StaleTicker() {
  const [count, setCount] = useState(0);
  useEffect(() => {
    const id = setInterval(() => setCount(count + 1), 1000);
    return () => clearInterval(id); // cleanup: lesson 3
  }, []);
  return <p>Stale ticker (stuck): {count}</p>;
}

// FIX: do not read `count` inside the effect. Ask React to add 1 to whatever the latest value is.
// Now the effect really has no dependencies, so [] is honest.
export function FixedTicker() {
  const [count, setCount] = useState(0);
  useEffect(() => {
    const id = setInterval(() => setCount((c) => c + 1), 1000);
    return () => clearInterval(id);
  }, []);
  return <p>Fixed ticker (counts up): {count}</p>;
}

export default function Lesson() {
  const [owner, setOwner] = useState('Asha Rao');
  return (
    <div>
      <h2>Effects 2 — The dependency array</h2>
      <label>
        Owner{' '}
        <select value={owner} onChange={(e) => setOwner(e.target.value)}>
          <option>Asha Rao</option>
          <option>Ben Carter</option>
        </select>
      </label>
      <p>
        Running <code>npm run dev</code>? StrictMode runs every effect twice on mount (setup, cleanup, setup), so each
        count is one higher than the label says; a production build shows the exact numbers.
      </p>
      <Probe owner={owner} />
      <h3>The stale value pitfall (one tick per second)</h3>
      <StaleTicker />
      <FixedTicker />
    </div>
  );
}
