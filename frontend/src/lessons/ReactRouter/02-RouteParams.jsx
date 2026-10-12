/*
 * Lesson 2 — Route parameters: one route, many URLs.
 *
 * What this shows
 *   We do not want one <Route> per account. A path segment that starts with a colon is a
 *   PARAMETER: path="/accounts/:id" matches /accounts/1, /accounts/2, /accounts/99 ... and the
 *   component reads the matching part with the useParams() hook.
 *
 * Things to remember
 *   - Params always arrive as STRINGS: the URL is text, so id is "2", not the number 2.
 *     Compare with a string (or convert on purpose with Number(id)).
 *   - Two different "not found" cases exist and they are different code:
 *       1. No Route matches the URL at all  -> the path="*" route shows.
 *       2. The URL matches (/accounts/99) but the DATA does not exist -> the component itself
 *          must notice that find() returned undefined and say so. The router cannot know your data.
 *   - The router only gives you the id. Looking the account up (here an array, later an API call)
 *     is your code's job.
 */
import { MemoryRouter, Routes, Route, Link, useParams } from 'react-router-dom';
import { ACCOUNTS, findAccount, UrlBar } from './shared.jsx';

export const title = 'Route params (:id with useParams) and not-found';

function AccountList() {
  return (
    <div>
      <h3>Accounts</h3>
      <ul>
        {ACCOUNTS.map((a) => (
          <li key={a.id}>
            <Link to={`/accounts/${a.id}`}>{a.owner}</Link>
          </li>
        ))}
        <li>
          <Link to="/accounts/99">Account 99 (does not exist)</Link>
        </li>
        <li>
          <Link to="/nonsense">/nonsense (no route matches)</Link>
        </li>
      </ul>
    </div>
  );
}

function AccountDetail() {
  const { id } = useParams(); // id is the text matched by ":id", e.g. "2"
  const account = findAccount(id);
  if (!account) {
    // Case 2: the URL matched the route, but there is no such account in our data.
    return <h3>No account with id "{id}"</h3>;
  }
  return (
    <div>
      <h3>{account.owner}</h3>
      <p>
        {account.type} account, balance {account.balance}
      </p>
      <p>
        The id from the URL is the {typeof id} "{id}".
      </p>
      <Link to="/accounts">Back to the list</Link>
    </div>
  );
}

export function RouteParamsDemo({ start = '/accounts' }) {
  return (
    <MemoryRouter initialEntries={[start]}>
      <UrlBar />
      <Routes>
        <Route path="/accounts" element={<AccountList />} />
        <Route path="/accounts/:id" element={<AccountDetail />} />
        {/* Case 1: nothing above matched the URL. */}
        <Route path="*" element={<div><h3>404: no page for this URL</h3><Link to="/accounts">Go to the list</Link></div>} />
      </Routes>
    </MemoryRouter>
  );
}

export default function Lesson() {
  return (
    <section>
      <h2>Route params</h2>
      <RouteParamsDemo />
    </section>
  );
}
