import { render, screen, within, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { StrictMode } from 'react';
import { vi } from 'vitest';

import Jsx, { Statement, heading, describe as describeElement, balanceOf, formatMoney } from './01-Jsx.jsx';
import Components, { AccountCard as Card2, AccountList, describeTypes } from './02-Components.jsx';
import Props, { AccountCard as Card3, Panel } from './03-Props.jsx';
import Keys, { AccountRows, accounts } from './04-ListsAndKeys.jsx';
import Cond, { TransactionList, BalanceLine } from './05-ConditionalRendering.jsx';

// ---------------------------------------------------------------- 01 JSX
describe('01 JSX', () => {
  test('a JSX tag is just a plain object: type is the tag name, props holds attributes and children', () => {
    expect(heading.type).toBe('h1');
    expect(heading.props).toEqual({ className: 'title', children: 'Hello' });
    expect(describeElement(heading)).toBe('{"type":"h1","props":{"className":"title","children":"Hello"}}');
  });

  test('{} evaluates expressions; the balance 500 - 120.5 + 75 is 454.50', () => {
    expect(balanceOf([{ kind: 'Deposit', amount: 500 }, { kind: 'Withdrawal', amount: 120.5 }, { kind: 'Deposit', amount: 75 }])).toBe(454.5);
    expect(formatMoney(454.5)).toBe('454.50');
    render(<Statement />);
    expect(screen.getByText('Statement for Asha Rao')).toBeInTheDocument();
    expect(screen.getByText('454.50')).toBeInTheDocument();
  });

  test('className becomes the class attribute; style takes an object; htmlFor links label and input', () => {
    const { container } = render(<Statement />);
    expect(container.querySelector('h3').getAttribute('class')).toBe('statement-title');
    expect(container.querySelector('h3').hasAttribute('classname')).toBe(false);
    expect(screen.getByText('454.50')).toHaveStyle({ color: 'rgb(46, 139, 87)' });
    expect(screen.getByLabelText('Note for this statement:')).toBeInTheDocument();
  });

  test('a Fragment adds no wrapper element to the DOM; self-closing tags render', () => {
    const { container } = render(<Statement />);
    const tags = [...container.children].map((el) => el.tagName);
    expect(tags).toEqual(['H3', 'P', 'TABLE', 'BR', 'LABEL', 'INPUT']);
  });

  test('the lesson page prints the element object', () => {
    render(<Jsx />);
    expect(screen.getByTestId('element-json')).toHaveTextContent('{"type":"h1","props":{"className":"title","children":"Hello"}}');
  });
});

// ---------------------------------------------------------------- 02 Components
describe('02 Components', () => {
  test('a capitalised tag refers to your function; a lowercase tag is a plain HTML tag name (string)', () => {
    expect(describeTypes()).toEqual([
      '<AccountCard />  ->  type is a function',
      '<accountcard />  ->  type is a string',
    ]);
    expect((<Card2 owner="x" number="y" balance={0} />).type).toBe(Card2);
  });

  test('a component is a plain function from props to JSX (React is the one that normally calls it)', () => {
    const element = Card2({ owner: 'Asha Rao', number: 'SAV-1001', balance: 454.5 });
    expect(element.type).toBe('div');
  });

  test('AccountList composes three AccountCards, each with its own inputs', () => {
    const { container } = render(<AccountList />);
    expect(container.querySelectorAll('strong')).toHaveLength(3);
    expect([...container.querySelectorAll('strong')].map((n) => n.textContent)).toEqual(['Asha Rao', 'Ben Carter', 'Chen Li']);
    expect(screen.getByText('Balance 454.50')).toBeInTheDocument();
    expect(screen.getByText('Balance 1200.00')).toBeInTheDocument();
    expect(screen.getByText('Balance 0.00')).toBeInTheDocument();
  });

  test('the lesson page renders the list and the type table', () => {
    render(<Components />);
    expect(screen.getByTestId('types')).toHaveTextContent('type is a function');
    expect(screen.getByTestId('types')).toHaveTextContent('type is a string');
  });
});

// ---------------------------------------------------------------- 03 Props
describe('03 Props', () => {
  test('destructured props arrive from the tag attributes', () => {
    render(<Card3 owner="Asha Rao" number="SAV-1001" balance={454.5} />);
    expect(screen.getByText('Asha Rao')).toBeInTheDocument();
    expect(screen.getByText('Balance 454.50')).toBeInTheDocument();
  });

  test('props are read-only: in the development build the props object is frozen and assigning throws', () => {
    let captured;
    function Spy(props) {
      captured = props;
      return null;
    }
    render(<Spy balance={10} />);
    expect(Object.isFrozen(captured)).toBe(true);
    expect(() => {
      captured.balance = 0;
    }).toThrow(TypeError);
    expect(captured.balance).toBe(10);
  });

  test('default value is used when the prop is missing or undefined, but not when it is null', () => {
    const { rerender } = render(<Card3 owner="A" number="1" balance={0} />);
    expect(screen.getByText('(Savings)')).toBeInTheDocument();
    rerender(<Card3 owner="A" number="1" balance={0} type={undefined} />);
    expect(screen.getByText('(Savings)')).toBeInTheDocument();
    rerender(<Card3 owner="A" number="1" balance={0} type="Current" />);
    expect(screen.getByText('(Current)')).toBeInTheDocument();
    rerender(<Card3 owner="A" number="1" balance={0} type={null} />);
    expect(screen.queryByText('(Savings)')).toBeNull();
    expect(screen.getByText('A').parentElement.querySelector('em').textContent).toBe('()');
  });

  test('children is whatever is written between the tags', () => {
    render(
      <Panel heading="Notes">
        <p>inside the frame</p>
      </Panel>,
    );
    expect(screen.getByText('Notes').nextElementSibling.textContent).toBe('inside the frame');
    expect((<Panel heading="h">text</Panel>).props.children).toBe('text');
  });

  test('a child can tell the parent something by calling a function the parent passed down', async () => {
    const onView = vi.fn();
    render(<Card3 owner="Asha Rao" number="SAV-1001" balance={454.5} onView={onView} />);
    await userEvent.click(screen.getByRole('button', { name: 'View SAV-1001' }));
    expect(onView).toHaveBeenCalledTimes(1);
    expect(onView).toHaveBeenCalledWith('SAV-1001');
  });

  test('no onView prop, no button', () => {
    render(<Card3 owner="Asha Rao" number="SAV-1001" balance={454.5} />);
    expect(screen.queryByRole('button')).toBeNull();
  });

  test('the lesson page: the parent hears the child, defaults and children show', async () => {
    render(<Props />);
    expect(screen.getAllByText('(Savings)')).toHaveLength(2); // Asha (default) and Chen (default)
    expect(screen.getByText('(Current)')).toBeInTheDocument();
    expect(screen.getByText('Note: no deposits yet.')).toBeInTheDocument();
    expect(screen.getByTestId('viewed')).toHaveTextContent('nothing yet');
    await userEvent.click(screen.getByRole('button', { name: 'View CUR-2001' }));
    expect(screen.getByTestId('viewed')).toHaveTextContent('CUR-2001');
  });
});

// ---------------------------------------------------------------- 04 Lists and keys
describe('04 Lists and keys', () => {
  const noteInputs = () => screen.getAllByRole('textbox');

  test('map turns the array into one row per account', () => {
    render(<AccountRows list={accounts} />);
    expect(screen.getAllByRole('listitem')).toHaveLength(3);
    expect(screen.getByLabelText('Note for Chen Li')).toBeInTheDocument();
  });

  test('key is for React: it is on the element but is not a prop', () => {
    const el = <li key="SAV-1001">x</li>;
    expect(el.key).toBe('SAV-1001');
    expect(Object.keys(el.props)).toEqual(['children']);
  });

  test('BUG with key = index: remove the first account and the typed note stays in row 0, now Ben Carter', async () => {
    const { rerender } = render(<AccountRows list={accounts} useIndexKey />);
    const firstInput = noteInputs()[0];
    await userEvent.type(firstInput, 'late fee waived');
    expect(screen.getByLabelText('Note for Asha Rao')).toHaveValue('late fee waived');

    rerender(<AccountRows list={accounts.slice(1)} useIndexKey />);
    // Row 0 now shows Ben Carter, but React reused the SAME input element, so Asha's note is still in it.
    expect(noteInputs()[0]).toBe(firstInput);
    expect(screen.getByLabelText('Note for Ben Carter')).toHaveValue('late fee waived');
  });

  test('RIGHT with key = id: the note leaves with Asha and Ben keeps his own (empty) input', async () => {
    const { rerender } = render(<AccountRows list={accounts} useIndexKey={false} />);
    const [asha, ben] = noteInputs();
    await userEvent.type(asha, 'late fee waived');
    await userEvent.type(ben, 'new address');

    rerender(<AccountRows list={accounts.slice(1)} useIndexKey={false} />);
    expect(asha.isConnected).toBe(false);
    expect(noteInputs()[0]).toBe(ben); // Ben's own element is kept
    expect(screen.getByLabelText('Note for Ben Carter')).toHaveValue('new address');
    expect(screen.queryByDisplayValue('late fee waived')).toBeNull();
  });

  test('missing keys make React warn; duplicate keys make React warn too', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    render(
      <ul>
        {accounts.map((a) => (
          <li>{a.owner}</li>
        ))}
      </ul>,
    );
    expect(spy.mock.calls.flat().join(' ')).toMatch(/unique "key" prop/);
    spy.mockClear();
    render(
      <ul>
        {['a', 'b'].map(() => (
          <li key="same">dup</li>
        ))}
      </ul>,
    );
    expect(spy.mock.calls.flat().join(' ')).toMatch(/same key/);
    spy.mockRestore();
  });

  test('keys only have to be unique among siblings, not across two lists', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    render(
      <>
        <AccountRows list={accounts} useIndexKey={false} />
        <AccountRows list={accounts} useIndexKey={false} />
      </>,
    );
    expect(spy).not.toHaveBeenCalled();
    spy.mockRestore();
  });

  test('the lesson page: Remove deletes the row in both lists; index list keeps the note on the wrong row', async () => {
    render(<Keys />);
    const lists = screen.getAllByRole('list');
    const [indexList, idList] = lists;
    await userEvent.type(within(indexList).getByLabelText('Note for Asha Rao'), 'note A');
    await userEvent.type(within(idList).getByLabelText('Note for Asha Rao'), 'note A');
    await userEvent.click(within(indexList).getByRole('button', { name: 'Remove Asha Rao' }));

    expect(within(indexList).getAllByRole('listitem')).toHaveLength(2);
    expect(within(idList).getAllByRole('listitem')).toHaveLength(2);
    expect(within(indexList).getByLabelText('Note for Ben Carter')).toHaveValue('note A'); // the bug
    expect(within(idList).getByLabelText('Note for Ben Carter')).toHaveValue(''); // right

    await userEvent.click(screen.getByRole('button', { name: 'Reset both lists' }));
    expect(within(screen.getAllByRole('list')[0]).getAllByRole('listitem')).toHaveLength(3);
  });
});

// ---------------------------------------------------------------- 05 Conditional rendering
describe('05 Conditional rendering', () => {
  const two = [{ id: 1, kind: 'Deposit', amount: 500 }, { id: 2, kind: 'Withdrawal', amount: 120.5 }];

  test('true, false, null and undefined draw nothing; strings and numbers are drawn', () => {
    const { container } = render(
      <div>
        {true}
        {false}
        {null}
        {undefined}
      </div>,
    );
    expect(container.firstChild.textContent).toBe('');
    expect(container.firstChild.childNodes).toHaveLength(0);
    const { container: c2 } = render(
      <div>
        {0}
        {'x'}
      </div>,
    );
    expect(c2.firstChild.textContent).toBe('0x');
  });

  test('with items, both versions list them', () => {
    const { container } = render(<TransactionList transactions={two} buggy />);
    expect(container.querySelectorAll('li')).toHaveLength(2);
    const { container: c2 } = render(<TransactionList transactions={two} />);
    expect(c2.querySelectorAll('li')).toHaveLength(2);
  });

  test('THE GOTCHA: length && <ul/> draws a stray "0" for an empty list', () => {
    const { container } = render(<TransactionList transactions={[]} buggy />);
    expect(container.textContent).toBe('0');
  });

  test('the fix: length > 0 with a ternary draws a message instead', () => {
    const { container } = render(<TransactionList transactions={[]} />);
    expect(container.textContent).toBe('No transactions yet.');
    expect(container.textContent).not.toContain('0');
  });

  test('a real boolean with && draws nothing when false', () => {
    const { container } = render(<TransactionList transactions={two} />);
    expect(container.textContent).not.toContain('Showing a long list.');
    expect(container.textContent).not.toContain('false');
  });

  test('a badge appears only when the balance is below zero', () => {
    const { rerender } = render(<BalanceLine account={{ owner: 'Asha Rao', balance: 454.5 }} />);
    expect(screen.queryByText('OVERDRAWN')).toBeNull();
    rerender(<BalanceLine account={{ owner: 'Chen Li', balance: -20 }} />);
    expect(screen.getByText('OVERDRAWN')).toBeInTheDocument();
    expect(screen.getByText(/Chen Li: -20.00/)).toBeInTheDocument();
  });

  test('a component can return null: nothing is drawn', () => {
    const { container } = render(<BalanceLine account={null} />);
    expect(container).toBeEmptyDOMElement();
  });

  test('the lesson page: emptying the account shows the stray 0 only in the buggy panel', async () => {
    render(<Cond />);
    const buggy = screen.getByTestId('buggy-panel');
    const fixed = screen.getByTestId('fixed-panel');
    expect(within(buggy).getAllByRole('listitem')).toHaveLength(2);
    await userEvent.click(screen.getByRole('button', { name: 'Empty the account' }));
    expect(buggy.textContent).toBe('length && ... (the bug)0');
    expect(fixed.textContent).toContain('No transactions yet.');
    await userEvent.click(screen.getByRole('button', { name: 'Put the transactions back' }));
    expect(within(buggy).getAllByRole('listitem')).toHaveLength(2);
  });
});

// A sanity check on all pages: they render without React warnings, also inside StrictMode.
describe('every lesson page renders without console errors', () => {
  test.each([['01', Jsx], ['02', Components], ['03', Props], ['04', Keys], ['05', Cond]])('%s', (_n, Page) => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    render(
      <StrictMode>
        <Page />
      </StrictMode>,
    );
    expect(spy).not.toHaveBeenCalled();
    spy.mockRestore();
  });
});
