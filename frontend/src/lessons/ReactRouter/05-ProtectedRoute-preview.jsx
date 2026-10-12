/*
 * Lesson 5 (preview) — A route guard that redirects when you are "not logged in".
 *
 * What this shows
 *   A guard component wraps a page. If the user is not logged in it renders <Navigate to="/login" />,
 *   which sends the router to the login page instead of showing the page. "replace" makes the
 *   redirect REPLACE the current history entry rather than add one, so pressing Back does not
 *   bounce the user straight back to the protected URL and into the redirect again. We also pass
 *   where the user wanted to go in the navigation "state" so that, after login, we can send them there.
 *
 * IMPORTANT — this is a user-interface convenience, NOT security
 *   - A client-side guard only decides what the browser SHOWS. The JavaScript runs on the user's
 *     machine, so the user can change it. Hiding a screen does not protect data: the server must not
 *     return private data to a person who is not allowed to see it.
 *   - The real protection is the API: every request for private data must be checked on the SERVER
 *     (Phase 4: JWT validation), which answers 401/403 whatever the browser's screens do.
 *   - Here "logged in" is just a true/false button. There is no password and no token. The real
 *     login (JWT) comes in a later phase and will replace this placeholder.
 */
import { useState } from 'react';
import { MemoryRouter, Routes, Route, Link, Navigate, useLocation, useNavigate, useNavigationType } from 'react-router-dom';
import { UrlBar } from './shared.jsx';

export const title = 'Protected route preview (Navigate redirect; UX only)';

function ProtectedRoute({ isLoggedIn, children }) {
  const location = useLocation();
  if (!isLoggedIn) {
    // Remember where the user was going, so Login can send them there afterwards.
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }
  return children;
}

function Login({ onLogin }) {
  const navigate = useNavigate();
  const location = useLocation();
  const navigationType = useNavigationType(); // "REPLACE" when we arrived through <Navigate replace>
  const from = location.state?.from ?? '/';
  return (
    <div>
      <h3>Login (placeholder)</h3>
      <p>
        You were redirected here. Arrived by: <strong data-testid="navtype">{navigationType}</strong>. Wanted page:{' '}
        <strong data-testid="from">{from}</strong>
      </p>
      <button
        onClick={() => {
          onLogin();
          navigate(from, { replace: true });
        }}
      >
        Pretend to log in
      </button>
    </div>
  );
}

function BackButton() {
  const navigate = useNavigate();
  return <button onClick={() => navigate(-1)}>Back (navigate(-1))</button>;
}

export function ProtectedRouteDemo() {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  return (
    <MemoryRouter initialEntries={['/']}>
      <UrlBar />
      <p>
        Pretend login state: <strong data-testid="status">{isLoggedIn ? 'logged in' : 'not logged in'}</strong>{' '}
        {isLoggedIn && <button onClick={() => setIsLoggedIn(false)}>Log out</button>}
      </p>
      <nav style={{ display: 'flex', gap: 12 }}>
        <BackButton />
        <Link to="/">Home</Link>
        <Link to="/my-accounts">My accounts (protected)</Link>
      </nav>
      <Routes>
        <Route path="/" element={<h3>Public home page</h3>} />
        <Route path="/login" element={<Login onLogin={() => setIsLoggedIn(true)} />} />
        <Route
          path="/my-accounts"
          element={
            <ProtectedRoute isLoggedIn={isLoggedIn}>
              <h3>My accounts: Asha Rao, Savings 1200.00</h3>
            </ProtectedRoute>
          }
        />
      </Routes>
      <p style={{ fontStyle: 'italic' }}>
        Reminder: this guard is only a convenience for the screen. The API must check the user on
        every request.
      </p>
    </MemoryRouter>
  );
}

export default function Lesson() {
  return (
    <section>
      <h2>Protected route (preview)</h2>
      <ProtectedRouteDemo />
    </section>
  );
}
