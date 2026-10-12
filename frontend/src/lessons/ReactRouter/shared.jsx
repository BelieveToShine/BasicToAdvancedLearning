// Small helpers shared by the ReactRouter lessons. (The lesson picker only loads files whose name
// starts with a digit, so this file is not listed as a lesson.)
import { useLocation } from 'react-router-dom';

// The bank example: three accounts. `balance` is kept as a string so the lessons show exact text.
export const ACCOUNTS = [
  { id: '1', owner: 'Asha Rao', type: 'Savings', balance: '1200.00' },
  { id: '2', owner: 'Ben Carter', type: 'Current', balance: '85.50' },
  { id: '3', owner: 'Chen Li', type: 'Savings', balance: '4300.00' },
];

export function findAccount(id) {
  return ACCOUNTS.find((a) => a.id === id);
}

// Shows the router's current location. In a MemoryRouter there is no address bar, so this panel
// plays the part of the address bar: it is the "URL" the router is currently matching.
export function UrlBar() {
  const { pathname, search } = useLocation();
  return (
    <p style={{ fontFamily: 'monospace', background: '#eee', padding: 6 }}>
      URL the router sees: <strong data-testid="url">{pathname + search}</strong>
    </p>
  );
}
