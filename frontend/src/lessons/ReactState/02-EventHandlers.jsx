/*
 * 02 - Event handlers: reacting to the user.
 *
 * WHAT THIS SHOWS
 *   You attach a function to an element with a prop named after the event: onClick, onChange,
 *   onSubmit. When the user does that thing, React calls your function and gives it ONE argument:
 *   an event object.
 *
 * THE RULE BEGINNERS GET WRONG
 *   Pass the function itself:   onClick={handleClick}      (React calls it later, on click)
 *   Not the result of calling:  onClick={handleClick()}    (runs it NOW, while rendering, and gives
 *                                                           React whatever it returned)
 *   If you need to pass an argument, wrap it:  onClick={() => handleClick(5)}
 *
 * THE EVENT OBJECT
 *   React hands you its own event object (often called a "synthetic event"). It has the familiar
 *   things: event.type ("click"), event.target (the element the user touched), and
 *   event.preventDefault() to stop the browser's built-in action. A form's built-in action on
 *   submit is to reload the page, so a React form handler normally starts with
 *   event.preventDefault().
 *
 * The log below is state, so each event adds a line you can read on the page.
 */
import { useState } from 'react';

export const title = 'Event handlers: onClick, onChange, onSubmit';

export default function Lesson() {
  const [log, setLog] = useState([]);
  const [note, setNote] = useState('');

  function addToLog(text) {
    setLog((previous) => [...previous, text]);
  }

  // The function we pass to onClick. React calls it with the event.
  function handleClick(event) {
    addToLog(`click: type=${event.type}, target=<${event.target.tagName.toLowerCase()}>`);
  }

  // A handler that takes our own argument, so we wrap it in an arrow function below.
  function handleDeposit(amount) {
    addToLog(`deposit button for ${amount}`);
  }

  function handleChange(event) {
    setNote(event.target.value);
    addToLog(`change: type=${event.type}, value="${event.target.value}"`);
  }

  function handleSubmit(event) {
    event.preventDefault(); // without this the browser would reload the page
    addToLog(`submit: note "${note}" (page not reloaded)`);
  }

  return (
    <section>
      <h1>{title}</h1>
      <p>
        <button onClick={handleClick}>onClick={'{handleClick}'}</button>{' '}
        <button onClick={() => handleDeposit(5)}>onClick={'{() => handleDeposit(5)}'}</button>
      </p>
      <form onSubmit={handleSubmit}>
        <label>
          Note: <input value={note} onChange={handleChange} />
        </label>{' '}
        <button type="submit">Save note</button>
      </form>
      <h2>Event log</h2>
      <ul data-testid="log">
        {log.map((line, index) => (
          <li key={index}>{line}</li>
        ))}
      </ul>
    </section>
  );
}
