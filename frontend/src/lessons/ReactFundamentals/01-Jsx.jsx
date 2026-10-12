/*
 * JSX — what it is and the handful of rules that trip up beginners.
 *
 * WHAT THIS SHOWS
 *   JSX looks like HTML inside JavaScript, but it IS JavaScript. Before the browser sees it, a build
 *   tool (Vite here) rewrites each <tag ...> into a plain function call that returns a small
 *   description object called a "React element". React later turns those descriptions into real DOM.
 *   The statement below is a bank statement drawn with JSX.
 *
 * THE RULES (each one is proved in ReactFundamentals.test.jsx)
 *   1. Curly braces {} drop any JavaScript EXPRESSION into the markup: {customer.name}, {a + b},
 *      {formatMoney(balance)}. An expression is anything that produces a value. Statements such as
 *      `if` or `for` are not expressions, so they cannot go inside {} (use a ternary, && or a variable
 *      computed before the return - lesson 05 - or .map - lesson 04).
 *   2. The HTML attribute `class` is written `className`, because `class` is a reserved word in
 *      JavaScript. (Likewise `for` on a <label> is written `htmlFor`.)
 *   3. Every tag must be closed. Tags with no children are self-closing: <br />, <input />.
 *   4. A component returns ONE thing. To return several siblings without adding an extra <div> to the
 *      page, wrap them in a Fragment: <> ... </>. A Fragment adds nothing to the DOM.
 *   5. style takes a JavaScript object, so you see double braces: style={{ color: 'red' }}
 *      (outer {} = "JavaScript here", inner {} = the object).
 *
 * WHY IT MATTERS
 *   Because JSX is just JavaScript, "what shows on screen" is computed with ordinary values and
 *   functions - there is no separate template language to learn.
 */

const customer = { name: 'Asha Rao', accountNumber: 'SAV-1001' };

// Three transactions, written out by hand. (Rendering a whole array with .map comes in lesson 04.)
const transactions = [
  { id: 1, kind: 'Deposit', amount: 500 },
  { id: 2, kind: 'Withdrawal', amount: 120.5 },
  { id: 3, kind: 'Deposit', amount: 75 },
];

// toFixed(2) always gives two decimals, so the output is the same on every computer.
export function formatMoney(n) {
  return n.toFixed(2);
}

// Deposits add to the balance, withdrawals subtract: 500 - 120.5 + 75 = 454.5
export function balanceOf(list) {
  return list.reduce((sum, t) => (t.kind === 'Deposit' ? sum + t.amount : sum - t.amount), 0);
}

export function Statement() {
  const balance = balanceOf(transactions);

  // Rule 4: three siblings (<h3>, <p>, <table>, ...) and no wrapper <div>: this is a Fragment.
  return (
    <>
      {/* Rule 2: className, not class */}
      <h3 className="statement-title">Statement for {customer.name}</h3>
      <p>
        Account {customer.accountNumber} - balance{' '}
        {/* Rules 1 and 5: {} runs JavaScript; the style object needs the second pair of braces. */}
        <strong style={{ color: balance < 0 ? 'crimson' : 'seagreen' }}>{formatMoney(balance)}</strong>
      </p>
      <table border="1" cellPadding="4">
        <tbody>
          <tr>
            <td>{transactions[0].kind}</td>
            <td>{formatMoney(transactions[0].amount)}</td>
          </tr>
          <tr>
            <td>{transactions[1].kind}</td>
            <td>{formatMoney(transactions[1].amount)}</td>
          </tr>
          <tr>
            <td>{transactions[2].kind}</td>
            <td>{formatMoney(transactions[2].amount)}</td>
          </tr>
        </tbody>
      </table>
      {/* Rule 3: <br /> and <input /> are self-closing. */}
      <br />
      <label htmlFor="stmt-note">Note for this statement: </label>
      <input id="stmt-note" />
    </>
  );
}

// A JSX tag is only a description. Here we keep one in a variable and look inside it.
export const heading = <h1 className="title">Hello</h1>;

// The same thing, shown as plain data: type is the tag name, props holds the attributes + children.
export function describe(element) {
  return JSON.stringify({ type: element.type, props: element.props });
}

export const title = '01 JSX - JavaScript that describes the screen';

export default function Lesson() {
  return (
    <div>
      <h2>JSX</h2>
      <section>
        <h3>The statement, drawn with JSX</h3>
        <Statement />
      </section>
      <section>
        <h3>What a JSX tag really is</h3>
        <p>
          In the code, <code>{'const heading = <h1 className="title">Hello</h1>;'}</code> does not touch the page.
          It builds this plain object (type and props shown):
        </p>
        <pre data-testid="element-json">{describe(heading)}</pre>
        <p>React reads descriptions like this and updates the real DOM to match.</p>
      </section>
    </div>
  );
}
