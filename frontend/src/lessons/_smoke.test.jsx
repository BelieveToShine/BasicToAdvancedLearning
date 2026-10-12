import { render, screen } from '@testing-library/react';
import App from '../App.jsx';

test('the lesson picker renders', () => {
  render(<App />);
  expect(screen.getByText('Lessons')).toBeInTheDocument();
});
