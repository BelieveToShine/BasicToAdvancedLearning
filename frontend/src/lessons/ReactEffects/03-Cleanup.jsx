/*
 * LESSON 3 — Cleanup
 *
 * What this shows
 *   Some effects START something that keeps going: a timer, a subscription, a listener. If nobody
 *   stops it, it keeps going after the component is gone or after the inputs changed. An effect can
 *   RETURN a function; React calls it to undo what the effect started:
 *
 *     useEffect(() => {
 *       const stop = feed.subscribe(...);   // start
 *       return () => stop();                // cleanup
 *     }, [accountId]);
 *
 *   When does React call the cleanup?
 *     - Before the effect runs again because a dependency changed (the OLD run is cleaned up first,
 *       then the new run starts), and
 *     - when the component is removed from the page (unmount).
 *
 * StrictMode
 *   While you develop, <StrictMode> (see main.jsx) mounts each component, then immediately runs its
 *   cleanup, then runs the effect again. That is a deliberate check: an effect whose cleanup really
 *   undoes its setup behaves the same either way, and one that leaks is exposed at once. The tests
 *   show "subscribe, unsubscribe, subscribe" under StrictMode and a leaking effect leaving 2
 *   listeners. React documents this as a development-only behaviour; the production build does not do it.
 *
 * The "feed" below is a tiny stand-in for any outside source (a websocket, a browser event ...).
 */
import { useEffect, useState } from 'react';

export const title = 'Effects 3 — Cleanup';

// A made-up balance feed living outside React. It records what happened so we can look at it.
export const feed = (() => {
  const listeners = new Map(); // accountId -> Set of callbacks
  let history = [];
  return {
    subscribe(accountId, callback) {
      if (!listeners.has(accountId)) listeners.set(accountId, new Set());
      listeners.get(accountId).add(callback);
      history.push(`subscribe ${accountId}`);
      return () => {
        listeners.get(accountId).delete(callback);
        history.push(`unsubscribe ${accountId}`);
      };
    },
    publish(accountId, balance) {
      (listeners.get(accountId) ?? []).forEach((cb) => cb(balance));
    },
    listenerCount() {
      let n = 0;
      listeners.forEach((set) => (n += set.size));
      return n;
    },
    history: () => [...history],
    reset() {
      listeners.clear();
      history = [];
    },
  };
})();

// GOOD: returns a cleanup function that undoes the subscribe.
export function BalanceWatcher({ accountId }) {
  const [balance, setBalance] = useState(null);
  useEffect(() => {
    const unsubscribe = feed.subscribe(accountId, (value) => setBalance(value));
    return unsubscribe; // cleanup: runs before the next effect run, and on unmount
  }, [accountId]);
  return <p>Watching {accountId}. Latest balance: {balance ?? 'none yet'}</p>;
}

// BAD (for comparison): subscribes but never unsubscribes.
export function LeakyWatcher({ accountId }) {
  const [balance, setBalance] = useState(null);
  useEffect(() => {
    feed.subscribe(accountId, (value) => setBalance(value));
  }, [accountId]);
  return <p>Leaky watcher on {accountId}. Latest balance: {balance ?? 'none yet'}</p>;
}

// A timer with cleanup. Without the clearInterval the timer would keep running after unmount.
export function Clock() {
  const [seconds, setSeconds] = useState(0);
  useEffect(() => {
    const id = setInterval(() => setSeconds((s) => s + 1), 1000);
    return () => clearInterval(id);
  }, []);
  return <p>Seconds since this clock appeared: {seconds}</p>;
}

export default function Lesson() {
  const [accountId, setAccountId] = useState('ACC-1001');
  const [showGood, setShowGood] = useState(true);
  const [showLeaky, setShowLeaky] = useState(false);
  const [showClock, setShowClock] = useState(true);
  const [inspection, setInspection] = useState(null);
  const [next, setNext] = useState(1000);

  return (
    <div>
      <h2>Effects 3 — Cleanup</h2>
      <label>
        Account{' '}
        <select value={accountId} onChange={(e) => setAccountId(e.target.value)}>
          <option>ACC-1001</option>
          <option>ACC-1002</option>
        </select>
      </label>
      <p>
        <button onClick={() => setShowGood((v) => !v)}>{showGood ? 'Remove' : 'Show'} the good watcher</button>{' '}
        <button onClick={() => setShowLeaky((v) => !v)}>{showLeaky ? 'Remove' : 'Show'} the leaky watcher</button>{' '}
        <button onClick={() => setShowClock((v) => !v)}>{showClock ? 'Remove' : 'Show'} the clock</button>
      </p>
      {showGood && <BalanceWatcher accountId={accountId} />}
      {showLeaky && <LeakyWatcher accountId={accountId} />}
      {showClock && <Clock />}
      <p>
        <button
          onClick={() => {
            feed.publish(accountId, next);
            setNext((n) => n + 250);
          }}
        >
          Publish balance {next} for {accountId}
        </button>{' '}
        <button onClick={() => setInspection({ count: feed.listenerCount(), history: feed.history() })}>
          Inspect the feed now
        </button>
      </p>
      {inspection && (
        <div aria-label="feed inspection">
          <p>Active listeners: {inspection.count}</p>
          <ol>
            {inspection.history.map((h, i) => (
              <li key={i}>{h}</li>
            ))}
          </ol>
        </div>
      )}
      <p>
        In development StrictMode subscribes, unsubscribes and subscribes again on mount, so one leaky watcher shows 2
        listeners here (a production build shows 1).
      </p>
      <p>Try it: remove the good watcher and the count drops. Remove the leaky one and it does not.</p>
    </div>
  );
}
