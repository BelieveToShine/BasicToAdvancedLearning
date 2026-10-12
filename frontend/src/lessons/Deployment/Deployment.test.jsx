// Tests for the Deployment lesson. fetch is mocked, so the tests are deterministic (no network).
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Lesson, { ApiCheck, configLocations, devVsProd, title } from './01-WhatDeploymentMeans.jsx';
import App from '../../App.jsx';

afterEach(() => vi.restoreAllMocks());

function mockFetch(impl) {
  const fn = vi.fn(impl);
  vi.stubGlobal('fetch', fn);
  return fn;
}

function reply(body, status = 200) {
  return () => Promise.resolve({ ok: status >= 200 && status < 300, status, text: () => Promise.resolve(body) });
}

test('the lesson shows both tables with every row', () => {
  render(<Lesson />);
  expect(screen.getAllByRole('table')).toHaveLength(2);
  expect(screen.getAllByRole('row')).toHaveLength(2 + configLocations.length + devVsProd.length);
  expect(screen.getByText(/Environment variable BTAL_SQLSERVER/)).toBeInTheDocument();
});

test('the check uses a RELATIVE address (no host baked into the build) and reports a list', async () => {
  const fetchMock = mockFetch(reply(JSON.stringify([{ accountId: 1 }, { accountId: 2 }, { accountId: 3 }])));
  render(<ApiCheck />);
  await userEvent.click(screen.getByRole('button', { name: /check get \/api\/accounts/i }));
  expect(await screen.findByText(/list of 3 account/)).toBeInTheDocument();
  expect(fetchMock).toHaveBeenCalledWith('/api/accounts');
});

test('an HTML answer (forwarding missing) is reported as "not JSON"', async () => {
  mockFetch(reply('<!doctype html><html></html>'));
  render(<ApiCheck />);
  await userEvent.click(screen.getByRole('button'));
  expect(await screen.findByText(/not JSON/)).toBeInTheDocument();
});

test('a 502 from the proxy is explained as "the API behind the web server did not answer"', async () => {
  mockFetch(reply('<html>Bad Gateway</html>', 502));
  render(<ApiCheck />);
  await userEvent.click(screen.getByRole('button'));
  expect(await screen.findByText(/HTTP 502: the web server is running, but it got no answer from the API behind it\. Is the API running\?/)).toBeInTheDocument();
});

test('a JSON error answer is reported with its status', async () => {
  mockFetch(reply(JSON.stringify({ title: 'x', status: 500 }), 500));
  render(<ApiCheck />);
  await userEvent.click(screen.getByRole('button'));
  expect(await screen.findByText(/HTTP 500 \(an error\)/)).toBeInTheDocument();
});

test('a network failure shows the friendly "not reachable" state, not a blank page', async () => {
  mockFetch(() => Promise.reject(new TypeError('Failed to fetch')));
  render(<ApiCheck />);
  await userEvent.click(screen.getByRole('button'));
  const status = await screen.findByRole('status');
  expect(within(status).getByText(/API not reachable/)).toBeInTheDocument();
});

test('the lesson is listed in the picker', () => {
  render(<App />);
  expect(screen.getByRole('button', { name: new RegExp(`Deployment / ${title}`) })).toBeInTheDocument();
});
