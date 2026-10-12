/*
 * Lesson 4 — Navigating from code, the active link, and the query string.
 *
 * Three small tools, one bank screen.
 *
 * 1. useNavigate(): Links are for clicks. Sometimes the code must move the user, for example after
 *    a form is submitted. useNavigate() gives a function: navigate('/accounts/2') goes there,
 *    navigate(-1) goes one step back in history (like the browser's Back button).
 *
 * 2. NavLink: a Link that knows whether its target matches the current URL. When it does, it gets the
 *    CSS class "active" and aria-current="page" automatically, so menus can highlight the current
 *    page (and screen readers can announce it). You can also pass a function as className/style to
 *    choose your own look.
 *
 * 3. useSearchParams(): the query string (the part after "?", e.g. /accounts?type=Savings) is a good
 *    home for state that should survive a refresh and be shareable as a link: filters, sort order,
 *    page number. It works like useState, but the value lives in the URL. Values are strings.
 *    Setting them creates a new URL (a new history entry).
 *
 * The "Open account 2" button uses navigate(); the "Savings / Current / All" buttons use
 * setSearchParams().
 */
import { MemoryRouter, Routes, Route, NavLink, useNavigate, useSearchParams, useParams } from 'react-router-dom';
import { ACCOUNTS, findAccount, UrlBar } from './shared.jsx';

export const title = 'useNavigate, NavLink active class, useSearchParams';

function Menu() {
  // NavLink adds className "active" (and aria-current="page") to the link matching the URL.
  return (
    <nav aria-label="menu" style={{ display: 'flex', gap: 12 }}>
      <NavLink to="/" end>Home</NavLink>
      <NavLink to="/accounts">Accounts</NavLink>
    </nav>
  );
}

function Accounts() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const type = searchParams.get('type'); // a string such as "Savings", or null when absent
  const shown = type ? ACCOUNTS.filter((a) => a.type === type) : ACCOUNTS;
  return (
    <div>
      <h3>Accounts</h3>
      <p>
        Filter:{' '}
        <button onClick={() => setSearchParams({ type: 'Savings' })}>Savings</button>{' '}
        <button onClick={() => setSearchParams({ type: 'Current' })}>Current</button>{' '}
        <button onClick={() => setSearchParams({})}>All</button>
      </p>
      <p>
        The "type" in the query string is: <strong data-testid="type">{type ?? '(none)'}</strong>
      </p>
      <ul data-testid="shown">
        {shown.map((a) => (
          <li key={a.id}>{a.owner} ({a.type})</li>
        ))}
      </ul>
      <button onClick={() => navigate('/accounts/2')}>Open account 2</button>
    </div>
  );
}

function AccountDetail() {
  const navigate = useNavigate();
  const { id } = useParams();
  const account = findAccount(id);
  return (
    <div>
      <h3>{account ? account.owner : `No account ${id}`}</h3>
      <button onClick={() => navigate(-1)}>Back (navigate(-1))</button>
    </div>
  );
}

export function NavigationDemo() {
  return (
    <MemoryRouter initialEntries={['/']}>
      <Menu />
      <UrlBar />
      <Routes>
        <Route path="/" element={<h3>Home</h3>} />
        <Route path="/accounts" element={<Accounts />} />
        <Route path="/accounts/:id" element={<AccountDetail />} />
      </Routes>
    </MemoryRouter>
  );
}

export default function Lesson() {
  return (
    <section>
      <h2>Navigation and the query string</h2>
      <style>{'nav a.active { font-weight: bold; text-decoration: underline; }'}</style>
      <NavigationDemo />
    </section>
  );
}
