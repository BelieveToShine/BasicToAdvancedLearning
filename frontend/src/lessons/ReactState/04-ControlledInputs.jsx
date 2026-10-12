/*
 * 04 - Controlled inputs: the state is the single source of truth.
 *
 * A "controlled" input gets its text from state (value={text}) and reports every keystroke back
 * (onChange -> setText). React puts the text on screen; the input never holds a value that React
 * does not know about. That makes it easy to validate, clear or change what is typed.
 *
 * DERIVED VALUES: the validation message is NOT stored in state. It is worked out from `text`
 * during each render. One source of truth cannot disagree with itself; a separate "error" state
 * could get out of sync.
 *
 * The form is a deposit form for a bank account: only an amount like 25 or 25.50 (more than zero,
 * at most two decimal places, so 25, 25.50 and .5 are accepted; 1e3 and -5 are not).
 */
import { useState } from 'react';

export const title = 'Controlled inputs: a deposit form with validation';

// Returns a message for a bad amount, or '' when it is fine.
export function validateAmount(text) {
  if (text.trim() === '') return 'Enter an amount.';
  if (!/^(\d+(\.\d{1,2})?|\.\d{1,2})$/.test(text.trim())) return 'Enter a number like 25 or 25.50 (at most 2 decimals).';
  if (Number(text) <= 0) return 'The amount must be more than zero.';
  return '';
}

export default function Lesson() {
  // Money is kept as whole cents: decimal fractions like 0.1 + 0.2 are not exact in JavaScript numbers.
  const [balanceCents, setBalanceCents] = useState(10000);
  const [text, setText] = useState('');
  const [submitted, setSubmitted] = useState(false); // show the message only after the first try

  const message = validateAmount(text); // derived, not state

  function handleSubmit(event) {
    event.preventDefault();
    setSubmitted(true);
    if (message !== '') return;
    setBalanceCents((current) => current + Math.round(Number(text) * 100));
    setText('');
    setSubmitted(false);
  }

  return (
    <section>
      <h1>{title}</h1>
      <p>
        Balance: <strong data-testid="balance">{(balanceCents / 100).toFixed(2)}</strong>
      </p>
      <form onSubmit={handleSubmit}>
        <label>
          Deposit amount:{' '}
          <input value={text} onChange={(event) => setText(event.target.value)} />
        </label>{' '}
        <button type="submit">Deposit</button>
      </form>
      {submitted && message !== '' && (
        <p role="alert" style={{ color: '#b00020' }}>
          {message}
        </p>
      )}
      <p>
        The input currently holds (read from state): <code data-testid="mirror">{text}</code>
      </p>
    </section>
  );
}
