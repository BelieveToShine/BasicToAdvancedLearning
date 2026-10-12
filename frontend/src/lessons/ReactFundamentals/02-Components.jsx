/*
 * COMPONENTS — a function that returns JSX, used like a tag.
 *
 * WHAT THIS SHOWS
 *   A React component is a plain JavaScript function whose name starts with a CAPITAL letter and which
 *   returns JSX. Once defined, you use it like your own HTML tag: <AccountCard />.
 *   Small components are combined ("composed") into bigger ones: AccountList puts three AccountCards
 *   inside itself, and the page then puts AccountList inside the lesson.
 *
 * WHY A CAPITAL LETTER?
 *   JSX has to decide what <something /> means. A lowercase name such as <div> or <accountcard> is
 *   treated as a built-in HTML tag name (a string). A capitalised name such as <AccountCard> is treated
 *   as a variable in your code that holds a component. So <accountCard /> would NOT find your function.
 *   The page below prints the `type` of both kinds of element so you can see it.
 *
 * WHO CALLS THE FUNCTION?
 *   React does. You write <AccountCard ... /> and React calls AccountCard(props) when it needs to know
 *   what to show. Each <AccountCard /> in the markup is a separate use, with its own inputs.
 *   (The inputs, called props, are explained properly in lesson 03.)
 *
 * WHY IT MATTERS
 *   Breaking a screen into small named pieces means each piece is easy to read, test and reuse.
 */

export function AccountCard({ owner, number, balance }) {
  return (
    <div style={{ border: '1px solid #888', borderRadius: 6, padding: 8, margin: 4, width: 220 }}>
      <strong>{owner}</strong>
      <div>Account {number}</div>
      <div>Balance {balance.toFixed(2)}</div>
    </div>
  );
}

// Composition: AccountList does not know HOW a card looks; it only decides WHICH cards to show.
export function AccountList() {
  return (
    <div>
      <AccountCard owner="Asha Rao" number="SAV-1001" balance={454.5} />
      <AccountCard owner="Ben Carter" number="CUR-2001" balance={1200} />
      <AccountCard owner="Chen Li" number="SAV-3001" balance={0} />
    </div>
  );
}

// Two elements made from a capitalised and a lowercase name. We only create them; we never render the
// lowercase one. (accountcard is not a real HTML tag.)
const capitalised = <AccountCard owner="x" number="y" balance={0} />;
const lowercase = <accountcard />;

export function describeTypes() {
  return [`<AccountCard />  ->  type is a ${typeof capitalised.type}`, `<accountcard />  ->  type is a ${typeof lowercase.type}`];
}

export const title = '02 Components - functions that return JSX';

export default function Lesson() {
  return (
    <div>
      <h2>Components</h2>
      <p>AccountList is a component made of three AccountCard components:</p>
      <AccountList />
      <h3>Capital letter or not?</h3>
      <pre data-testid="types">{describeTypes().join('\n')}</pre>
    </div>
  );
}
