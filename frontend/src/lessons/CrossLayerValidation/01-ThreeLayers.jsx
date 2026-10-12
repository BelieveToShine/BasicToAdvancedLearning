/*
 * 01 - Three layers check the same bad input.
 *
 * A value typed in a form travels browser -> API -> database. Each stop can say "no", and each says it
 * for a different reason:
 *
 *   1. BROWSER   fast and friendly: it answers while the user is still typing, with no network trip.
 *                But the user controls the browser. Anyone can skip the form and send the request by hand
 *                (curl, the developer tools, a script, or a plain fetch like the one on this page).
 *                So it is a convenience, never protection.
 *   2. API       the source of truth for the rules. Every request goes through it, whoever sent it.
 *                In this API a data-annotation attribute ([Range], [AllowedValues]...) on the request class
 *                is checked by [ApiController] BEFORE the action runs; a failure is a 400 and the
 *                action's code does not execute.
 *   3. DATABASE  the last line: UNIQUE indexes, CHECK constraints and foreign keys keep the data valid
 *                even if the API has a bug, or someone writes to the table without using the API.
 *
 * What THIS page shows, with the real API (every one of these requests is refused, so no data changes):
 *   A negative amount and B unknown kind  -> the API answers 400 before any SQL runs.
 *   C duplicate account number            -> passes the API's validation (the API does not look the
 *                                            number up first); the database's UNIQUE index refuses the
 *                                            second row, and the API turns that into 409.
 * The database checks for A and B exist too: the panel at the bottom lists what SQL Server said when
 * the same values were inserted directly with sqlcmd.
 */
import { useState } from 'react';
import { ApiDown, bad, box, callApi, classify, useAccounts } from './bankApi.jsx';

export const title = '1. Three layers: browser, API, database';

// ---- Layer 1: the browser's own checks. Plain functions: easy to test, nothing to do with React.
// Each returns a list of messages; an empty list means "looks fine to me".
export function checkTransactionInBrowser({ kind, amount }) {
  const messages = [];
  if (kind !== 'Deposit' && kind !== 'Withdrawal') messages.push("kind must be 'Deposit' or 'Withdrawal'.");
  if (!(amount > 0)) messages.push('amount must be greater than 0.');
  return messages;
}

export function checkAccountInBrowser({ accountNumber, accountType }) {
  const messages = [];
  if (!/^[A-Za-z0-9-]{1,20}$/.test(accountNumber)) messages.push("accountNumber must be 1-20 characters: letters, digits or '-'.");
  if (accountType !== 'Savings' && accountType !== 'Current') messages.push("accountType must be 'Savings' or 'Current'.");
  // Note what is NOT here: "is this number already taken?". The browser only knows the list it loaded a
  // while ago; another user may have taken the number since. Only the database sees every row at once.
  return messages;
}

function buildScenarios(account) {
  return [
    {
      id: 'amount',
      short: 'A. negative amount',
      label: 'A. A negative amount',
      body: { kind: 'Deposit', amount: -5 },
      url: `/api/accounts/${account.accountId}/transactions`,
      browserCheck: checkTransactionInBrowser,
    },
    {
      id: 'kind',
      short: 'B. unknown kind',
      label: "B. An unknown kind ('Bonus')",
      body: { kind: 'Bonus', amount: 5 },
      url: `/api/accounts/${account.accountId}/transactions`,
      browserCheck: checkTransactionInBrowser,
    },
    {
      id: 'duplicate',
      short: 'C. duplicate account number',
      label: `C. A duplicate account number (${account.accountNumber} already exists)`,
      // customerId 1 is the first seeded customer (FullStackBank/README.md).
      body: { customerId: 1, accountNumber: account.accountNumber, accountType: 'Savings' },
      url: '/api/accounts',
      browserCheck: checkAccountInBrowser,
    },
  ];
}

// What the database layer did for one scenario, worked out from the API's answer.
export function databaseVerdict(api) {
  if (!api) return '-';
  if (api.network) return '-';
  // Scope: attribute validation (the checks on the request class). A 400 that needs a look-up, such as
  // "customerId does not exist", is produced inside the action after a SELECT, so SQL did run there.
  if (api.status === 400) return 'not reached: attribute validation answered 400 before the action (and any SQL) ran';
  if (api.status === 409 && api.problem && api.problem.title === 'Duplicate value') {
    return 'caught it: UNIQUE index refused the row (SQL error 2601); the API turned that into 409';
  }
  return `no verdict (HTTP ${api.status})`;
}

function Scenario({ scenario, state, onBrowser, onSend }) {
  const { label, body, url } = scenario;
  const json = JSON.stringify(body);
  return (
    <section style={box} aria-label={label}>
      <h3 style={{ marginTop: 0 }}>{label}</h3>
      <p>
        Request: <code>POST {url}</code> with body <code>{json}</code>
      </p>
      <p>
        <button type="button" onClick={() => onBrowser(scenario)}>1. Ask the browser (layer 1)</button>{' '}
        <button type="button" onClick={() => onSend(scenario)} disabled={state.sending}>
          2. Skip the browser: send it with a raw fetch
        </button>
      </p>
      {state.browser && (
        <p data-testid={`${scenario.id}-browser`}>
          <strong>Browser:</strong>{' '}
          {state.browser.length === 0 ? (
            'looks fine to me (it cannot see anything wrong with this value).'
          ) : (
            <span style={bad}>{state.browser.join(' ')}</span>
          )}
        </p>
      )}
      {state.sending && <p>Sending...</p>}
      {state.api && (
        <div data-testid={`${scenario.id}-api`}>
          <strong>API answered:</strong>{' '}
          {state.api.network ? (
            'no answer at all (network failure)'
          ) : (
            <>
              HTTP {state.api.status} {state.api.problem ? `- ${state.api.problem.title}` : ''}
              <pre style={{ background: '#f4f4f4', padding: 8, overflowX: 'auto' }}>{JSON.stringify(state.api.data, null, 2)}</pre>
            </>
          )}
          <p>
            <strong>Database:</strong> {databaseVerdict(state.api)}
          </p>
        </div>
      )}
      <p style={{ fontSize: 13, color: '#555' }}>
        The same thing without any page at all:{' '}
        <code>
          curl -X POST {url} -H 'Content-Type: application/json' -d '{json}'
        </code>
      </p>
    </section>
  );
}

// What SQL Server answered when these values were inserted DIRECTLY (sqlcmd, no API in between).
// Recorded by running the statements against the bank database; error numbers do not change between versions.
const DATABASE_ROWS = [
  ['A. Transactions row with Amount = -5', '547', 'CHECK constraint CK_Transactions_Amount_Positive'],
  ["B. Transactions row with Kind = 'Bonus'", '547', 'CHECK constraint CK_Transactions_Kind'],
  ['(extra) Transactions row for AccountId 999', '547', 'FOREIGN KEY constraint FK_Transactions_Accounts_AccountId'],
  ['C. Accounts row with an existing AccountNumber', '2601', 'UNIQUE index IX_Accounts_AccountNumber'],
];

export default function Lesson() {
  const { status, accounts, reload } = useAccounts();
  const [cards, setCards] = useState({});

  function patch(id, change) {
    setCards((all) => ({ ...all, [id]: { ...all[id], ...change } }));
  }

  function askBrowser(scenario) {
    patch(scenario.id, { browser: scenario.browserCheck(scenario.body) });
  }

  async function sendRaw(scenario) {
    patch(scenario.id, { sending: true });
    // No browser check here, on purpose: this is what a script or "curl" does.
    const api = await callApi('POST', scenario.url, scenario.body);
    patch(scenario.id, { sending: false, api });
  }

  if (status === 'loading') return <p>Loading accounts...</p>;
  if (status === 'down') return <ApiDown onRetry={reload} />;
  if (status !== 'ready' || accounts.length === 0) return <p role="alert">The API answered, but there is no account to use. Seed data: see FullStackBank/README.md.</p>;

  const scenarios = buildScenarios(accounts[0]);
  return (
    <section>
      <h1>{title}</h1>
      <p>
        The same bad input meets three checkers. Run each scenario: ask the browser first, then send the request
        to the real API yourself, skipping the browser. All three requests below are refused, so nothing is saved.
      </p>
      {scenarios.map((s) => (
        <Scenario key={s.id} scenario={s} state={cards[s.id] ?? {}} onBrowser={askBrowser} onSend={sendRaw} />
      ))}

      <h2>Who caught what (filled in as you run the scenarios)</h2>
      <table border="1" cellPadding="6" style={{ borderCollapse: 'collapse' }} data-testid="summary">
        <thead>
          <tr>
            <th>Input</th>
            <th>1. Browser</th>
            <th>2. API</th>
            <th>3. Database</th>
          </tr>
        </thead>
        <tbody>
          {scenarios.map((s) => {
            const c = cards[s.id] ?? {};
            return (
              <tr key={s.id}>
                <td>{s.short}</td>
                <td>{c.browser ? (c.browser.length ? 'caught it' : 'passed it') : 'not asked yet'}</td>
                <td>{c.api ? (c.api.network ? 'no answer' : `HTTP ${c.api.status} (${classify(c.api)})`) : 'not sent yet'}</td>
                <td>{databaseVerdict(c.api)}</td>
              </tr>
            );
          })}
        </tbody>
      </table>

      <h2>The database layer, asked directly</h2>
      <p>
        Through the API you only reach the database when the API lets the request through (scenario C). To see
        the database's own rules, the same values were inserted straight into SQL Server with sqlcmd:
      </p>
      <table border="1" cellPadding="6" style={{ borderCollapse: 'collapse' }}>
        <thead>
          <tr>
            <th>Direct INSERT</th>
            <th>SQL error number</th>
            <th>Rule that refused it</th>
          </tr>
        </thead>
        <tbody>
          {DATABASE_ROWS.map(([what, number, rule]) => (
            <tr key={what}>
              <td>{what}</td>
              <td>{number}</td>
              <td>{rule}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <p>
        Why keep all three? The browser makes the form pleasant. The API makes the rules true for every client.
        The database keeps the data valid even when a bug or a direct SQL statement gets past the code.
      </p>
    </section>
  );
}
