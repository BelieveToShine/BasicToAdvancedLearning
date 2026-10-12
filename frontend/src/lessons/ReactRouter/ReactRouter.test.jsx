import { render, screen, fireEvent, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import Lesson1, { title as t1, RoutesAndLinksDemo } from './01-RoutesAndLinks.jsx';
import Lesson2, { title as t2, RouteParamsDemo } from './02-RouteParams.jsx';
import Lesson3, { title as t3 } from './03-NestedRoutesAndLayouts.jsx';
import Lesson4, { title as t4 } from './04-NavigationAndSearchParams.jsx';
import Lesson5, { title as t5 } from './05-ProtectedRoute-preview.jsx';

const url = () => screen.getByTestId('url').textContent;

test('every lesson has a title and renders', () => {
  for (const [L, t] of [[Lesson1, t1], [Lesson2, t2], [Lesson3, t3], [Lesson4, t4], [Lesson5, t5]]) {
    expect(typeof t).toBe('string');
    const { unmount } = render(<L />);
    unmount();
  }
});

describe('01 Routes and Links', () => {
  test('the URL decides which component is shown', async () => {
    const user = userEvent.setup();
    render(<Lesson1 />);
    expect(url()).toBe('/');
    expect(screen.getByText(/Home: welcome/)).toBeInTheDocument();
    await user.click(screen.getByRole('link', { name: 'Accounts' }));
    expect(url()).toBe('/accounts');
    expect(screen.getByText(/Accounts: your accounts/)).toBeInTheDocument();
    expect(screen.queryByText(/Home: welcome/)).not.toBeInTheDocument();
  });

  test('Link renders a real <a href>', () => {
    render(<Lesson1 />);
    expect(screen.getByRole('link', { name: 'About' })).toHaveAttribute('href', '/about');
  });

  test('Link stops the browser load (preventDefault); a plain <a> does not', () => {
    render(
      <>
        <RoutesAndLinksDemo />
        <a href="/plain">plain</a>
      </>,
    );
    // A listener on document runs AFTER the router's handler (events bubble up). It records whether the
    // click had already been cancelled, then cancels it itself so jsdom does not try to load a page.
    const seen = [];
    const spy = (e) => {
      seen.push([e.target.textContent, e.defaultPrevented]);
      e.preventDefault();
    };
    document.addEventListener('click', spy);
    fireEvent.click(screen.getByRole('link', { name: 'Accounts' }));
    fireEvent.click(screen.getByText('plain'));
    document.removeEventListener('click', spy);
    expect(seen).toEqual([
      ['Accounts', true], // Link: the router cancelled the browser's page load
      ['plain', false], // plain <a>: nobody cancelled it, so a browser would load /plain
    ]);
  });

  test('state outside <Routes> survives a Link navigation', async () => {
    const user = userEvent.setup();
    render(<Lesson1 />);
    await user.click(screen.getByRole('button', { name: 'Count a click' }));
    await user.click(screen.getByRole('button', { name: 'Count a click' }));
    expect(screen.getByTestId('clicks')).toHaveTextContent('2');
    await user.click(screen.getByRole('link', { name: 'About' }));
    expect(screen.getByText(/About: a tiny/)).toBeInTheDocument();
    expect(screen.getByTestId('clicks')).toHaveTextContent('2');
  });

  test('path="*" catches URLs nothing else matches', () => {
    render(
      <MemoryRouter initialEntries={['/nope']}>
        <Routes>
          <Route path="/" element={<p>home</p>} />
          <Route path="*" element={<p>catch-all</p>} />
        </Routes>
      </MemoryRouter>,
    );
    expect(screen.getByText('catch-all')).toBeInTheDocument();
  });
});

describe('02 Route params', () => {
  test('/accounts/:id reads the id with useParams, as a string', async () => {
    const user = userEvent.setup();
    render(<Lesson2 />);
    await user.click(screen.getByRole('link', { name: 'Ben Carter' }));
    expect(url()).toBe('/accounts/2');
    expect(screen.getByRole('heading', { name: 'Ben Carter' })).toBeInTheDocument();
    expect(screen.getByText(/Current account, balance 85.50/)).toBeInTheDocument();
    expect(screen.getByText(/is the string "2"/)).toBeInTheDocument();
  });

  test('matching URL but missing data: the component says so', () => {
    render(<RouteParamsDemo start="/accounts/99" />);
    expect(screen.getByText('No account with id "99"')).toBeInTheDocument();
    expect(screen.queryByText(/404/)).not.toBeInTheDocument();
  });

  test('no matching route: the * route shows', async () => {
    const user = userEvent.setup();
    render(<Lesson2 />);
    await user.click(screen.getByRole('link', { name: /nonsense/ }));
    expect(screen.getByText(/404: no page/)).toBeInTheDocument();
  });
});

describe('03 Nested routes and layouts', () => {
  test('layout is drawn around the index child, then the :id child', async () => {
    const user = userEvent.setup();
    render(<Lesson3 />);
    expect(screen.getByText('Bank: Accounts section')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Chen Li' })).toHaveAttribute('href', '/accounts/3');
    await user.click(screen.getByRole('link', { name: 'Chen Li' }));
    expect(url()).toBe('/accounts/3');
    expect(screen.getByText('Balance: 4300.00')).toBeInTheDocument();
    expect(screen.getByText('Bank: Accounts section')).toBeInTheDocument();
  });

  test('layout state survives while the child (Outlet) changes', async () => {
    const user = userEvent.setup();
    render(<Lesson3 />);
    await user.type(screen.getByLabelText(/Note to self/), 'call Asha');
    await user.click(screen.getByRole('link', { name: 'Asha Rao' }));
    expect(screen.getByText('Balance: 1200.00')).toBeInTheDocument();
    expect(screen.getByLabelText(/Note to self/)).toHaveValue('call Asha');
    await user.click(screen.getByRole('link', { name: 'Back to the list' }));
    expect(screen.getByLabelText(/Note to self/)).toHaveValue('call Asha');
  });

  test('a route outside the parent has no layout', async () => {
    const user = userEvent.setup();
    render(<Lesson3 />);
    await user.click(screen.getByRole('link', { name: /Leave the section/ }));
    expect(screen.getByText(/Home \(no layout here\)/)).toBeInTheDocument();
    expect(screen.queryByText('Bank: Accounts section')).not.toBeInTheDocument();
  });
});

describe('04 Navigation and search params', () => {
  test('NavLink gets class "active" and aria-current only for the matching URL', async () => {
    const user = userEvent.setup();
    render(<Lesson4 />);
    const nav = screen.getByRole('navigation', { name: 'menu' });
    const home = within(nav).getByRole('link', { name: 'Home' });
    const accounts = within(nav).getByRole('link', { name: 'Accounts' });
    expect(home).toHaveClass('active');
    expect(home).toHaveAttribute('aria-current', 'page');
    expect(accounts).not.toHaveClass('active');
    expect(accounts).not.toHaveAttribute('aria-current');
    await user.click(accounts);
    expect(accounts).toHaveClass('active');
    expect(home).not.toHaveClass('active');
  });

  test('NavLink without "end" stays active on a deeper URL (/accounts/2)', async () => {
    const user = userEvent.setup();
    render(<Lesson4 />);
    await user.click(screen.getByRole('link', { name: 'Accounts' }));
    await user.click(screen.getByRole('button', { name: 'Open account 2' }));
    expect(url()).toBe('/accounts/2');
    const nav = screen.getByRole('navigation', { name: 'menu' });
    expect(within(nav).getByRole('link', { name: 'Accounts' })).toHaveClass('active');
    expect(within(nav).getByRole('link', { name: 'Home' })).not.toHaveClass('active');
  });

  test('setSearchParams writes the filter into the URL and the list follows', async () => {
    const user = userEvent.setup();
    render(<Lesson4 />);
    await user.click(screen.getByRole('link', { name: 'Accounts' }));
    expect(screen.getByTestId('type')).toHaveTextContent('(none)');
    expect(within(screen.getByTestId('shown')).getAllByRole('listitem')).toHaveLength(3);
    await user.click(screen.getByRole('button', { name: 'Savings' }));
    expect(url()).toBe('/accounts?type=Savings');
    expect(screen.getByTestId('type')).toHaveTextContent('Savings');
    expect(screen.getByTestId('shown')).toHaveTextContent('Asha Rao');
    expect(screen.getByTestId('shown')).toHaveTextContent('Chen Li');
    expect(screen.getByTestId('shown')).not.toHaveTextContent('Ben Carter');
    await user.click(screen.getByRole('button', { name: 'All' }));
    expect(url()).toBe('/accounts');
  });

  test('useNavigate moves from code, and navigate(-1) goes back', async () => {
    const user = userEvent.setup();
    render(<Lesson4 />);
    await user.click(screen.getByRole('link', { name: 'Accounts' }));
    await user.click(screen.getByRole('button', { name: 'Savings' }));
    await user.click(screen.getByRole('button', { name: 'Open account 2' }));
    expect(url()).toBe('/accounts/2');
    expect(screen.getByRole('heading', { name: 'Ben Carter' })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Back/ }));
    expect(url()).toBe('/accounts?type=Savings');
  });
});

describe('05 Protected route preview', () => {
  test('not logged in: visiting the protected page redirects to /login with replace', async () => {
    const user = userEvent.setup();
    render(<Lesson5 />);
    expect(screen.getByTestId('status')).toHaveTextContent('not logged in');
    await user.click(screen.getByRole('link', { name: /My accounts/ }));
    expect(url()).toBe('/login');
    expect(screen.queryByText(/Asha Rao, Savings/)).not.toBeInTheDocument();
    expect(screen.getByTestId('navtype')).toHaveTextContent('REPLACE');
    expect(screen.getByTestId('from')).toHaveTextContent('/my-accounts');
  });

  test('Back after the redirect goes to the previous page, not back to /login', async () => {
    const user = userEvent.setup();
    render(<Lesson5 />);
    await user.click(screen.getByRole('link', { name: /My accounts/ }));
    expect(url()).toBe('/login');
    await user.click(screen.getByRole('button', { name: /Back/ }));
    expect(url()).toBe('/');
    expect(screen.getByText('Public home page')).toBeInTheDocument();
  });

  test('after the pretend login the user lands on the page they wanted', async () => {
    const user = userEvent.setup();
    render(<Lesson5 />);
    await user.click(screen.getByRole('link', { name: /My accounts/ }));
    await user.click(screen.getByRole('button', { name: 'Pretend to log in' }));
    expect(url()).toBe('/my-accounts');
    expect(screen.getByText(/Asha Rao, Savings 1200.00/)).toBeInTheDocument();
    expect(screen.getByTestId('status')).toHaveTextContent('logged in');
  });

  test('logging out puts the guard back in charge', async () => {
    const user = userEvent.setup();
    render(<Lesson5 />);
    await user.click(screen.getByRole('link', { name: /My accounts/ }));
    await user.click(screen.getByRole('button', { name: 'Pretend to log in' }));
    await user.click(screen.getByRole('button', { name: 'Log out' }));
    expect(url()).toBe('/login');
  });

  test('the lesson states that the guard is not security', () => {
    render(<Lesson5 />);
    expect(screen.getByText(/only a convenience/)).toBeInTheDocument();
    expect(screen.getByText(/API must check the user/)).toBeInTheDocument();
  });
});
