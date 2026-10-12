/*
 * Lesson 1 — Routes and Links: the URL decides which component is on screen.
 *
 * What this shows
 *   In a single-page app the browser loads ONE html page. A router watches the URL and decides which
 *   component to render for it: <Routes> holds a list of <Route path="..." element={...} /> entries and
 *   shows the element of the entry that matches the current URL.
 *
 * Link vs a plain <a>
 *   A plain <a href="/accounts"> asks the browser to go and fetch that address, which throws away
 *   the running page and its state. <Link to="/accounts"> renders an <a> too, but when you click it
 *   the router calls preventDefault() (so the browser does NOT load a new page), changes its own
 *   location, and re-renders only the part that matches. State kept outside <Routes> (the counter
 *   below) survives the click. The tests prove both halves: preventDefault is called for Link and
 *   not for a plain <a>, and the counter survives.
 *
 * Why MemoryRouter here
 *   The router normally reads and writes the browser's address bar (BrowserRouter). BrowserRouter
 *   would rewrite the real address bar, which the lesson picker does not know about (a refresh
 *   would lose the lesson), so we use MemoryRouter: the same router, but it keeps the URL in memory. In a real app you would use BrowserRouter (or
 *   createBrowserRouter); everything else you see here stays the same.
 */
import { useState } from 'react';
import { MemoryRouter, Routes, Route, Link } from 'react-router-dom';
import { UrlBar } from './shared.jsx';

export const title = 'Routes and Links (URL decides the component)';

function Home() {
  return <h3>Home: welcome to the bank</h3>;
}
function Accounts() {
  return <h3>Accounts: your accounts will be listed here</h3>;
}
function About() {
  return <h3>About: a tiny demo bank</h3>;
}

export function RoutesAndLinksDemo() {
  // This state lives OUTSIDE <Routes>. If a click on a Link reloaded the page, it would reset to 0.
  const [clicks, setClicks] = useState(0);
  return (
    <MemoryRouter initialEntries={['/']}>
      <UrlBar />
      <nav aria-label="main" style={{ display: 'flex', gap: 12 }}>
        <Link to="/">Home</Link>
        <Link to="/accounts">Accounts</Link>
        <Link to="/about">About</Link>
      </nav>
      <p>
        <button onClick={() => setClicks(clicks + 1)}>Count a click</button> clicks so far:{' '}
        <strong data-testid="clicks">{clicks}</strong> (kept while you move between pages)
      </p>
      {/* Only the Route whose path matches the URL renders. path="*" matches anything left over. */}
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/accounts" element={<Accounts />} />
        <Route path="/about" element={<About />} />
        <Route path="*" element={<h3>No page for this URL</h3>} />
      </Routes>
    </MemoryRouter>
  );
}

export default function Lesson() {
  return (
    <section>
      <h2>Routes and Links</h2>
      <RoutesAndLinksDemo />
    </section>
  );
}
