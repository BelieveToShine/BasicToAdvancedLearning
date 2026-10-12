/*
 * Lesson 3 — Nested routes and layouts: the frame stays, the middle changes.
 *
 * What this shows
 *   Most pages share a frame (header, menu). Instead of repeating it, nest <Route>s inside a parent
 *   Route whose element is a LAYOUT component. The layout renders the shared frame and puts
 *   <Outlet /> where the matching child should appear.
 *
 *   <Route path="/accounts" element={<AccountsLayout />}>   <- parent: the frame
 *     <Route index element={<AccountList />} />             <- shown at exactly /accounts
 *     <Route path=":id" element={<AccountDetail />} />      <- shown at /accounts/:id
 *   </Route>
 *
 * Why it matters
 *   When you move from /accounts to /accounts/2, the layout is the SAME element in the same
 *   place in the tree, so React keeps it (and its state) and only swaps what is inside <Outlet />.
 *   The note box in the layout proves this: what you typed is still there after the child changes.
 *
 * Notes
 *   - A child path is relative to its parent: ":id" under "/accounts" means "/accounts/:id".
 *   - The "index" route is the child used when the URL ends exactly at the parent.
 */
import { useState } from 'react';
import { MemoryRouter, Routes, Route, Link, Outlet, useParams } from 'react-router-dom';
import { ACCOUNTS, findAccount, UrlBar } from './shared.jsx';

export const title = 'Nested routes and layouts (Outlet)';

function AccountsLayout() {
  // State that belongs to the layout. If the layout were re-created on every URL change, this
  // text would be lost; it is kept because the layout stays mounted.
  const [note, setNote] = useState('');
  return (
    <div style={{ border: '2px solid #888', padding: 8 }}>
      <header>
        <strong>Bank: Accounts section</strong> (this frame is the layout)
        <p>
          <label>
            Note to self (lives in the layout):{' '}
            <input value={note} onChange={(e) => setNote(e.target.value)} />
          </label>
        </p>
      </header>
      <hr />
      {/* The matching child route is drawn here. */}
      <Outlet />
    </div>
  );
}

function AccountList() {
  return (
    <ul>
      {ACCOUNTS.map((a) => (
        <li key={a.id}>
          {/* A link without a leading "/" is relative to the route it is drawn in: "2" -> /accounts/2 */}
          <Link to={a.id}>{a.owner}</Link>
        </li>
      ))}
    </ul>
  );
}

function AccountDetail() {
  const { id } = useParams();
  const account = findAccount(id);
  return account ? (
    <div>
      <h3>{account.owner}</h3>
      <p>Balance: {account.balance}</p>
      <Link to="/accounts">Back to the list</Link>
    </div>
  ) : (
    <p>No account with id "{id}"</p>
  );
}

export function NestedRoutesDemo() {
  return (
    <MemoryRouter initialEntries={['/accounts']}>
      <UrlBar />
      <Routes>
        <Route path="/" element={<p>Home (no layout here)</p>} />
        <Route path="/accounts" element={<AccountsLayout />}>
          <Route index element={<AccountList />} />
          <Route path=":id" element={<AccountDetail />} />
        </Route>
      </Routes>
      <p>
        <Link to="/">Leave the section (Home)</Link>
      </p>
    </MemoryRouter>
  );
}

export default function Lesson() {
  return (
    <section>
      <h2>Nested routes and layouts</h2>
      <NestedRoutesDemo />
    </section>
  );
}
