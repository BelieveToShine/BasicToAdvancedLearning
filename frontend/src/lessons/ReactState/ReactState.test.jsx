import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Profiler, useState } from 'react';
import Lesson1 from './01-UseState.jsx';
import Lesson2 from './02-EventHandlers.jsx';
import Lesson3 from './03-StateUpdates.jsx';
import Lesson4, { validateAmount } from './04-ControlledInputs.jsx';
import Lesson5 from './05-ObjectsAndArrays.jsx';

// Counts how many times React committed (finished a render and updated the screen).
function renderCounted(ui) {
  const counter = { commits: 0 };
  const view = render(
    <Profiler id="p" onRender={() => counter.commits++}>
      {ui}
    </Profiler>,
  );
  return { counter, ...view };
}
const text = (id) => screen.getByTestId(id).textContent;

describe('01 useState', () => {
  test('state survives re-renders, the ordinary variable resets to its start', async () => {
    const user = userEvent.setup();
    render(<Lesson1 />);
    expect(text('state-balance')).toBe('100');
    await user.click(screen.getByRole('button', { name: 'Deposit 10' }));
    await user.click(screen.getByRole('button', { name: 'Deposit 10' }));
    expect(text('state-balance')).toBe('120');
    expect(text('ordinary-balance')).toBe('100');
  });

  test('the initial value is only used on the first render', async () => {
    const user = userEvent.setup();
    render(<Lesson1 />);
    await user.click(screen.getByRole('button', { name: 'Deposit 1' }));
    await user.click(screen.getByRole('button', { name: /Change opening balance/ }));
    expect(text('prop-value')).toBe('500'); // the prop changed...
    expect(text('initial-state-balance')).toBe('101'); // ...the state did not follow
  });
});

describe('02 event handlers', () => {
  test('pass the function, do not call it: calling runs it during render', () => {
    const handler = vi.fn();
    const { unmount } = render(<button onClick={handler()}>wrong</button>);
    expect(handler).toHaveBeenCalledTimes(1); // ran while rendering, with no click at all
    unmount();
    const handler2 = vi.fn();
    render(<button onClick={handler2}>right</button>);
    expect(handler2).not.toHaveBeenCalled();
    fireEvent.click(screen.getByText('right'));
    expect(handler2).toHaveBeenCalledTimes(1);
  });

  test('onClick receives an event with type and target', async () => {
    const user = userEvent.setup();
    render(<Lesson2 />);
    await user.click(screen.getByRole('button', { name: /handleClick/ }));
    expect(text('log')).toContain('click: type=click, target=<button>');
  });

  test('a wrapped arrow function can pass an argument', async () => {
    const user = userEvent.setup();
    render(<Lesson2 />);
    await user.click(screen.getByRole('button', { name: /handleDeposit\(5\)/ }));
    expect(text('log')).toContain('deposit button for 5');
  });

  test('onChange fires per keystroke with the new value', async () => {
    const user = userEvent.setup();
    render(<Lesson2 />);
    await user.type(screen.getByLabelText(/Note/), 'ab');
    const lines = screen.getByTestId('log').querySelectorAll('li');
    expect(lines[0].textContent).toBe('change: type=change, value="a"');
    expect(lines[1].textContent).toBe('change: type=change, value="ab"');
  });

  test('onSubmit handler calls preventDefault', () => {
    render(<Lesson2 />);
    const form = screen.getByRole('button', { name: 'Save note' }).closest('form');
    // fireEvent returns false when a handler called preventDefault
    expect(fireEvent.submit(form)).toBe(false);
    expect(text('log')).toContain('page not reloaded');
  });

  test('without preventDefault a submit event is not cancelled', () => {
    render(
      <form onSubmit={() => {}}>
        <button type="submit">go</button>
      </form>,
    );
    expect(fireEvent.submit(screen.getByText('go').closest('form'))).toBe(true);
  });
});

describe('03 state updates', () => {
  test('the variable is a snapshot: it still holds the old value after setBalance', async () => {
    const user = userEvent.setup();
    render(<Lesson3 />);
    await user.click(screen.getByRole('button', { name: /^1\./ }));
    expect(text('log')).toContain('balance is still 100');
    expect(text('balance')).toBe('110'); // the NEXT render has the new value
  });

  test('setBalance(balance + 10) x3 adds 10, functional x3 adds 30', async () => {
    const user = userEvent.setup();
    render(<Lesson3 />);
    await user.click(screen.getByRole('button', { name: /^2a/ }));
    expect(text('balance')).toBe('110');
    await user.click(screen.getByRole('button', { name: /^2b/ }));
    expect(text('balance')).toBe('140');
  });

  test.each([[/^1\./, 2], [/^2a/, 3], [/^2b/, 3]])('button %s (%i setter calls) renders once per click (batching)', async (name) => {
    const user = userEvent.setup();
    const { counter } = renderCounted(<Lesson3 />);
    expect(counter.commits).toBe(1); // first render
    await user.click(screen.getByRole('button', { name }));
    expect(counter.commits).toBe(2); // exactly one more
  });
});

describe('04 controlled inputs', () => {
  test('the input shows exactly what is in state', async () => {
    const user = userEvent.setup();
    render(<Lesson4 />);
    await user.type(screen.getByLabelText(/Deposit amount/), '25.5');
    expect(screen.getByLabelText(/Deposit amount/)).toHaveValue('25.5');
    expect(text('mirror')).toBe('25.5');
  });

  test('validateAmount rules', () => {
    expect(validateAmount('')).toMatch(/Enter an amount/);
    expect(validateAmount('1e3')).toMatch(/Enter a number/);
    expect(validateAmount('.5')).toBe('');
    expect(validateAmount('abc')).toMatch(/Enter a number/);
    expect(validateAmount('1.234')).toMatch(/Enter a number/);
    expect(validateAmount('-5')).toMatch(/Enter a number/);
    expect(validateAmount('0')).toMatch(/more than zero/);
    expect(validateAmount('25.50')).toBe('');
  });

  test('a bad amount shows a message and leaves the balance alone', async () => {
    const user = userEvent.setup();
    render(<Lesson4 />);
    expect(screen.queryByRole('alert')).toBeNull(); // no message before the first try
    await user.type(screen.getByLabelText(/Deposit amount/), 'abc');
    await user.click(screen.getByRole('button', { name: 'Deposit' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Enter a number');
    expect(text('balance')).toBe('100.00');
  });

  test('cents stay exact: 0.1 + 0.2 + 0.1 deposits give 100.40', async () => {
    const user = userEvent.setup();
    render(<Lesson4 />);
    for (const v of ['0.1', '0.2', '0.1']) {
      await user.type(screen.getByLabelText(/Deposit amount/), v);
      await user.click(screen.getByRole('button', { name: 'Deposit' }));
    }
    expect(text('balance')).toBe('100.40');
  });

  test('a good amount is added and the input is cleared', async () => {
    const user = userEvent.setup();
    render(<Lesson4 />);
    await user.type(screen.getByLabelText(/Deposit amount/), '25');
    await user.click(screen.getByRole('button', { name: 'Deposit' }));
    expect(text('balance')).toBe('125.00');
    expect(screen.getByLabelText(/Deposit amount/)).toHaveValue('');
    expect(screen.queryByRole('alert')).toBeNull();
  });
});

describe('05 objects and arrays', () => {
  test('update by copying: owner row and total change', async () => {
    const user = userEvent.setup();
    render(<Lesson5 />);
    expect(text('total')).toBe('350');
    await user.click(screen.getByRole('button', { name: 'Deposit 10 to Asha Rao' }));
    expect(text('balance-1')).toBe('110');
    expect(text('balance-2')).toBe('250'); // untouched item kept
    expect(text('total')).toBe('360'); // the sibling sees the lifted state
  });

  test('add and remove', async () => {
    const user = userEvent.setup();
    render(<Lesson5 />);
    await user.click(screen.getByRole('button', { name: 'Add account' }));
    expect(screen.getByText(/New customer 3:/)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Remove Ben Carter' }));
    expect(screen.queryByText(/Ben Carter:/)).toBeNull();
    expect(text('total')).toBe('100');
  });

  test('mutating does not re-render; an unrelated re-render shows the edited data late', async () => {
    const user = userEvent.setup();
    const { counter } = renderCounted(<Lesson5 />);
    const before = counter.commits;
    await user.click(screen.getByRole('button', { name: /Mutate first account/ }));
    expect(counter.commits).toBe(before); // React was never told: no re-render
    expect(text('balance-1')).toBe('100'); // screen is stale
    await user.click(screen.getByRole('button', { name: /Unrelated re-render/ }));
    expect(text('balance-1')).toBe('110'); // the data had changed all along
  });

  test('the wrong-way mutation does not leak into a freshly mounted lesson', async () => {
    const user = userEvent.setup();
    const first = render(<Lesson5 />);
    await user.click(screen.getByRole('button', { name: /Mutate first account/ }));
    await user.click(screen.getByRole('button', { name: /Unrelated re-render/ }));
    expect(text('balance-1')).toBe('110');
    first.unmount();
    render(<Lesson5 />);
    expect(text('balance-1')).toBe('100');
    expect(text('total')).toBe('350');
  });

  test('a copy does re-render', async () => {
    const user = userEvent.setup();
    const { counter } = renderCounted(<Lesson5 />);
    const before = counter.commits;
    await user.click(screen.getByRole('button', { name: 'Deposit 10 to Asha Rao' }));
    expect(counter.commits).toBe(before + 1);
  });

  test('siblings cannot share state unless it is lifted (own state stays separate)', async () => {
    // A tiny demonstration of the claim in lesson 05's comment.
    function Counter() {
      const [n, setN] = useState(0);
      return <button onClick={() => setN(n + 1)}>count {n}</button>;
    }
    render(
      <>
        <Counter />
        <Counter />
      </>,
    );
    const buttons = screen.getAllByRole('button');
    await userEvent.click(buttons[0]);
    expect(buttons[0]).toHaveTextContent('count 1');
    expect(buttons[1]).toHaveTextContent('count 0');
  });
});
