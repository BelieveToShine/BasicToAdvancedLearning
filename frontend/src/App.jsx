import { useState } from 'react';

// Lesson picker. Every file at src/lessons/<Topic>/<NN>-<Name>.jsx (name starts with a digit; tests are <Topic>.test.jsx) is discovered automatically:
// it must `export default` a component and may `export const title = '...'`.
// (A topic never needs to edit this file — the same idea as Program.cs's ILearningTopic in the C# topics.)
const modules = import.meta.glob('./lessons/*/[0-9]*.jsx', { eager: true });

const lessons = Object.entries(modules)
  .map(([path, mod]) => {
    const [, topic, file] = path.match(/\.\/lessons\/([^/]+)\/([^/]+)\.jsx$/);
    return { path, topic, file, title: mod.title ?? file, Component: mod.default };
  })
  .sort((a, b) => a.path.localeCompare(b.path));

export default function App() {
  const [selected, setSelected] = useState(lessons[0]?.path);
  const current = lessons.find((l) => l.path === selected);

  return (
    <div style={{ display: 'flex', fontFamily: 'Arial, Helvetica, sans-serif' }}>
      <nav style={{ width: 280, padding: 16, borderRight: '1px solid #ccc', minHeight: '100vh' }}>
        <h2>Lessons</h2>
        {lessons.length === 0 && <p>No lessons yet.</p>}
        <ul style={{ listStyle: 'none', padding: 0 }}>
          {lessons.map((l) => (
            <li key={l.path}>
              <button
                onClick={() => setSelected(l.path)}
                style={{ fontWeight: l.path === selected ? 'bold' : 'normal', cursor: 'pointer' }}
              >
                {l.topic} / {l.title}
              </button>
            </li>
          ))}
        </ul>
      </nav>
      <main style={{ padding: 24, flex: 1 }}>{current ? <current.Component /> : null}</main>
    </div>
  );
}
