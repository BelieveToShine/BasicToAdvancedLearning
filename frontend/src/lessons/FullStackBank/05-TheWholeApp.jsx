/*
 * Mini project, step 5 - the whole app: everything from steps 1-4 wired together.
 *
 * Nothing new is invented here: this file only decides WHICH of the earlier components to show.
 *   list   -> AccountsList (step 1)           click a row ->
 *   detail -> AccountScreen = AccountDetail (step 2) + MoneyForm (step 3)
 *   open   -> OpenAccountForm (step 4)        after a 201 -> the new account's detail screen
 *
 * The "screen" is ONE piece of state: { name: 'list' } | { name: 'detail', id } | { name: 'open' }.
 * Simple state-based navigation is enough to see the full-stack idea. Its price: the address bar does
 * not change and a browser refresh returns to the list. The URL-based version is Topic 24 (React Router).
 *
 * Read the Journey panel at the bottom while you click: every screen change that needs data is one
 * more request, and every request takes the same trip React -> fetch -> Vite proxy -> API -> SQL Server and back.
 *
 * Who owns what (Memory/5-TheWholeApp.svg):
 *   screen, form inputs, loading flags  -> React state (lives in the browser tab, lost on refresh)
 *   accounts, balances, transactions    -> SQL Server rows (the real data); React holds only a COPY of
 *                                          the last response, which can be out of date: that is why
 *                                          we re-fetch after a change.
 */
import { useState } from 'react';
import { Journey, STATE, useFreshCallLog } from './parts.jsx';
import { AccountsList } from './01-AccountsList.jsx';
import { AccountScreen } from './03-MoneyForm.jsx';
import { OpenAccountForm } from './04-OpenAccount.jsx';

export const title = '5. The whole app (list, detail, money, open account)';

export function BankApp() {
  const [screen, setScreen] = useState({ name: 'list' });
  const goList = () => setScreen({ name: 'list' });

  return (
    <div>
      <nav aria-label="screens">
        <button onClick={goList}>Accounts</button> <button onClick={() => setScreen({ name: 'open' })}>Open an account</button>
      </nav>
      {screen.name === 'list' && <AccountsList onOpen={(id) => setScreen({ name: 'detail', id })} />}
      {screen.name === 'detail' && <AccountScreen key={screen.id} id={screen.id} onBack={goList} />}
      {screen.name === 'open' && (
        <OpenAccountForm onCreated={(account) => setScreen({ name: 'detail', id: account.accountId })} />
      )}
    </div>
  );
}

export default function Lesson() {
  useFreshCallLog(); // the Journey panel starts empty for this lesson
  return (
    <div>
      <h2>Step 5: the whole app</h2>
      <BankApp />
      <Journey
        shows={[
          ['which screen (list / detail / open)', STATE],
          ['accounts, balances, transactions', 'SQL Server rows; React shows the last API response'],
          ['errors (404, 409, 422)', 'the API; React only displays them'],
        ]}
      />
    </div>
  );
}
