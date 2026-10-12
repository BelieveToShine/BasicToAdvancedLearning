/*
 * 02 - Field errors from the API's ProblemDetails (a 400 answer).
 *
 * When a request body breaks a validation rule, the API answers 400 with a JSON body like:
 *
 *   { "title": "One or more validation errors occurred.", "status": 400,
 *     "errors": { "accountNumber": ["accountNumber must be 1-20 characters: ..."],
 *                 "accountType":   ["accountType must be 'Savings' or 'Current'."] },
 *     "traceId": "00-..." }
 *
 * `errors` is an object: each KEY is the camelCase name of a request field and each VALUE is an ARRAY of
 * messages for that field (a field can break more than one rule). The form shows each message right next to
 * the input it belongs to, so the user sees exactly what to fix.
 *
 * Two details worth knowing (both verified against the real API):
 *  - Some 400s are not about a field you can see: a key such as "$.customerId" (a JSON path) means the value had
 *    the wrong type or format ("The value for 'customerId' has the wrong type or format."), key "$" means the
 *    body was not valid JSON, key "body" means it was empty. None of these is an input of the form, so their
 *    messages go into the "general" box.
 *  - "customerId: Customer 99 does not exist" is also a field error although the browser could never know it:
 *    it needs the database. A 400 means "your request has a problem", not "a browser rule failed".
 *
 * This form has NO checks of its own and is typed in free text, so you can send wrong values on purpose
 * (lesson 01 showed why the server must check anyway; a real form would have both).
 */
import { useState } from 'react';
import { ApiDown, bad, box, callApi, classify, friendlyMessage, isApiDown, splitProblemErrors, useAccounts } from './bankApi.jsx';

export const title = '2. Field errors from the 400 answer';

export const FIELDS = ['customerId', 'accountNumber', 'accountType'];

// One message list under one input. aria-describedby on the input points here, so a screen reader
// reads the message together with the field.
function FieldError({ id, messages }) {
  if (!messages || messages.length === 0) return null;
  return (
    <span id={id} style={{ ...bad, display: 'block', fontSize: 14 }}>
      {messages.join(' ')}
    </span>
  );
}

function Field({ name, label, type = 'text', values, setValue, errors, hint }) {
  const errorId = `${name}-error`;
  const messages = errors[name];
  return (
    <p>
      <label htmlFor={name}>{label}</label>
      <br />
      <input
        id={name}
        type={type}
        value={values[name]}
        onChange={(event) => setValue(name, event.target.value)}
        aria-invalid={messages ? 'true' : undefined}
        aria-describedby={messages ? errorId : undefined}
        style={messages ? { borderColor: '#cc0000', borderWidth: 2 } : undefined}
      />
      {hint && <small> {hint}</small>}
      <FieldError id={errorId} messages={messages} />
    </p>
  );
}

export default function Lesson() {
  const { status, reload } = useAccounts();
  const [values, setValues] = useState({ customerId: '1', accountNumber: 'bad number!', accountType: 'Gold' });
  const [pending, setPending] = useState(false);
  const [result, setResult] = useState(null);

  function setValue(name, text) {
    setValues((current) => ({ ...current, [name]: text }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setPending(true);
    const body = {
      // A blank number box is left out of the JSON; the API then reports customerId like any other field.
      customerId: values.customerId.trim() === '' ? undefined : Number(values.customerId),
      accountNumber: values.accountNumber,
      accountType: values.accountType,
    };
    const answer = await callApi('POST', '/api/accounts', body);
    setResult(answer);
    setPending(false);
  }

  async function deletePracticeAccount() {
    const answer = await callApi('DELETE', `/api/accounts/${result.data.accountId}`);
    setResult(answer.ok ? { ok: true, status: 204, data: null, problem: null, deleted: true } : answer);
  }

  if (status === 'loading') return <p>Checking the API...</p>;
  if (status === 'down') return <ApiDown onRetry={reload} />;

  // Derived from the last answer, not stored: the same result always gives the same screen.
  const kind = result ? classify(result) : null;
  const split = kind === 'field' ? splitProblemErrors(result.problem, FIELDS) : { byField: {}, general: [] };

  return (
    <section>
      <h1>{title}</h1>
      <p>
        Open a bank account. The fields are plain text on purpose and the page checks nothing itself, so the
        API's answer is all you see.
      </p>
      <p>
        Try:{' '}
        <button type="button" onClick={() => setValues({ customerId: '1', accountNumber: 'bad number!', accountType: 'Gold' })}>
          two bad fields
        </button>{' '}
        <button type="button" onClick={() => setValues({ customerId: '', accountNumber: '', accountType: '' })}>
          everything empty
        </button>{' '}
        <button type="button" onClick={() => setValues({ customerId: '99', accountNumber: 'OK-123', accountType: 'Savings' })}>
          customer that does not exist
        </button>{' '}
        <button
          type="button"
          onClick={() => setValues({ customerId: '1', accountNumber: `PRACTICE-${Date.now() % 100000}`, accountType: 'Current' })}
        >
          valid values (creates an account)
        </button>
      </p>
      <form onSubmit={handleSubmit} noValidate style={box}>
        <Field name="customerId" label="Customer id" type="number" values={values} setValue={setValue} errors={split.byField} />
        <Field name="accountNumber" label="Account number" values={values} setValue={setValue} errors={split.byField} hint="1-20 letters, digits or -" />
        <Field name="accountType" label="Account type" values={values} setValue={setValue} errors={split.byField} hint="Savings or Current" />
        <button type="submit" disabled={pending}>
          {pending ? 'Sending...' : 'Open account'}
        </button>
      </form>

      {split.general.length > 0 && (
        <div role="alert" style={{ ...bad, border: '1px solid #cc0000', padding: 8 }} data-testid="general-errors">
          {split.general.map((message) => (
            <p key={message} style={{ margin: 0 }}>{message}</p>
          ))}
        </div>
      )}

      {result && kind === 'ok' && !result.deleted && (
        <p role="status" data-testid="created">
          Created account {result.data.accountNumber} (id {result.data.accountId}), balance {result.data.balance}.{' '}
          <button type="button" onClick={deletePracticeAccount}>Delete it again (DELETE, allowed while it has no transactions)</button>
        </p>
      )}
      {result && result.deleted && <p role="status">Practice account deleted.</p>}
      {result && kind !== 'field' && kind !== 'ok' && (
        <p role="alert" style={bad} data-testid="other-failure">
          {isApiDown(result) ? 'API not reachable.' : ''} {friendlyMessage(result)}
        </p>
      )}

      {result && result.problem && (
        <>
          <h2>What the API sent (status {result.status})</h2>
          <pre style={{ background: '#f4f4f4', padding: 8, overflowX: 'auto' }} data-testid="raw-problem">
            {JSON.stringify(result.problem, null, 2)}
          </pre>
        </>
      )}
      <p style={{ fontSize: 14 }}>
        The code that maps it: <code>for (const [key, messages] of Object.entries(problem.errors))</code> - a key that is
        one of the form's field names goes under that input; any other key goes into the general box above.
        The messages stay until the next submit; this page does not clear one while you type.
      </p>
    </section>
  );
}
