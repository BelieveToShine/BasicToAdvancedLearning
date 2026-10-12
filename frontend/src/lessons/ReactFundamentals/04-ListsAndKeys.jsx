/*
 * LISTS AND KEYS — drawing an array, and why every item needs a stable key.
 *
 * WHAT THIS SHOWS
 *   To draw a list, turn an array of data into an array of JSX with .map():
 *       accounts.map((a) => <li key={a.id}>...</li>)
 *   React accepts arrays of elements as children.
 *
 * WHY `key`?
 *   When the list changes (an item is removed, added or reordered) React has to work out WHICH old
 *   row matches WHICH new row so it can reuse the existing DOM element and the things attached to it.
 *   The key is the label it matches on. A good key is
 *     - unique among its siblings (two rows in the same list must not share one), and
 *     - stable: the same item keeps the same key every time (an id from your data).
 *   Keys do not have to be unique across the whole page, only among siblings in one list.
 *   `key` is for React only: it is not passed to your component as a prop.
 *
 * THE BUG THIS LESSON SHOWS
 *   Using the array index as the key (key={i}) means "the first row is always key 0". If you remove the
 *   first account, the old second account moves to position 0 and React matches it with the OLD row 0.
 *   The row's data (the text) is updated, but things React does not control, such as what you typed
 *   into an <input> that React does not manage, stay in the old DOM element - so the note you typed
 *   for Asha now sits next to Ben. With key={a.id} the note leaves together with Asha.
 *   Rule of thumb: index keys are acceptable only for a list that never reorders or loses items.
 *
 *   Below, both lists get the same accounts and the same Remove buttons. Type a note in the FIRST row
 *   of each list, then remove the first account.
 *
 * (The Remove buttons use state, which is Topic 22 - treat useState as "a variable that makes the page
 *  redraw when it changes".)
 */
import { useState } from 'react';

export const accounts = [
  { id: 'SAV-1001', owner: 'Asha Rao' },
  { id: 'CUR-2001', owner: 'Ben Carter' },
  { id: 'SAV-3001', owner: 'Chen Li' },
];

// useIndexKey = true  -> the buggy version (key = position)
// useIndexKey = false -> the right version (key = the account's own id)
export function AccountRows({ list, useIndexKey, onRemove }) {
  return (
    <ul>
      {list.map((a, i) => (
        <li key={useIndexKey ? i : a.id}>
          {/* This input is "uncontrolled": the browser keeps what you type, React does not. */}
          <label>
            Note for {a.owner} <input />
          </label>{' '}
          {onRemove && <button onClick={() => onRemove(a.id)}>Remove {a.owner}</button>}
        </li>
      ))}
    </ul>
  );
}

export const title = '04 Lists and keys - map, and why key matters';

export default function Lesson() {
  const [list, setList] = useState(accounts); // preview of Topic 22
  const remove = (id) => setList((current) => current.filter((a) => a.id !== id));
  return (
    <div>
      <h2>Lists and keys</h2>
      <p>Type a note in the first row of BOTH lists, then press the first Remove button in either list.</p>
      <p>Both lists share the same data, so one Remove click removes that account from both lists.</p>
      <button onClick={() => setList(accounts)}>Reset both lists</button>
      <div style={{ display: 'flex', gap: 32 }}>
        <div>
          <h3>key = index (the bug)</h3>
          <AccountRows list={list} useIndexKey onRemove={remove} />
        </div>
        <div>
          <h3>key = account id (right)</h3>
          <AccountRows list={list} useIndexKey={false} onRemove={remove} />
        </div>
      </div>
    </div>
  );
}
