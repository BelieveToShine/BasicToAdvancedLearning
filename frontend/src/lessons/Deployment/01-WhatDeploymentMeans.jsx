/*
 * LESSON 1 — What "deploying" means (optional lesson)
 *
 * What this shows
 *   So far the app ran with `dotnet run` and `npm run dev` on your own computer. DEPLOYING means: put the
 *   finished program on a machine where other people can use it, and keep it running. In this repo the
 *   hands-on guide is FullStackBank/deploy/README.md (Docker + docker compose); this page explains the ideas.
 *
 *   1) CODE vs CONFIGURATION. The code is the same everywhere. What changes between your laptop and a server
 *      (database address, passwords, which port) is configuration. Keep it OUT of the code and the image, and
 *      hand it to the program when it starts, usually as environment variables ("12-factor" style).
 *   2) BUILD vs RUN. Building turns source code into something runnable (the .NET API -> published files, the
 *      React app -> plain HTML/JS/CSS files). Running starts it. A Docker image is the result of a build;
 *      a container is a running copy of an image.
 *   3) DEV vs PRODUCTION. The same program behaves differently on purpose; the table below shows the ones in
 *      this project.
 *
 * Why it matters
 *   A password committed to git or baked into an image is a leaked password. And "works on my machine" usually
 *   means the setup lived only in your head; a Dockerfile and a compose file write it down.
 *
 * Note: this page calls fetch('/api/accounts') with a RELATIVE address. The React build contains no API address.
 *   Whoever serves the page (the Vite dev server in development, nginx in the container stack) forwards /api to
 *   the API. That is why the same build works on any machine.
 */
import { useState } from 'react';

export const title = 'What deploying means';

const cell = { border: '1px solid #bbb', padding: '4px 8px', textAlign: 'left', verticalAlign: 'top' };

// Where each kind of setting lives in the container stack of FullStackBank/deploy.
export const configLocations = [
  ['SQL Server admin password', '.env file on the server (git-ignored)', 'Environment variable of the db container'],
  ['API connection string (with password)', '.env file, assembled by docker-compose.yml', 'Environment variable BTAL_SQLSERVER of the api container, set when it starts'],
  ['How to build the API', 'Dockerfile.api (committed)', 'Baked into the image: steps, base image, non-root user, port 8080'],
  ['Compiled API and React files', 'Created by the build', 'Inside the images (layers), never edited by hand'],
  ['Database files', 'Docker volume "sqldata"', 'Survive stopping the containers; removed by docker compose down -v (or docker volume rm)'],
];

// The differences this project really has (checked by running the container stack).
export const devVsProd = [
  ['Database creation + migration', 'Done by the API at startup (Development only)', 'Not done by the API; a separate db-init step runs the SQL scripts'],
  ['Database login', 'Whatever is in your BTAL_SQLSERVER variable', 'A login that can only read and write rows (cannot CREATE TABLE)'],
  ['Who serves the React app', 'Vite dev server (npm run dev)', 'nginx serving the built files from npm run build'],
  ['How /api reaches the API', 'Vite proxy to http://localhost:5080', 'nginx proxy to http://api:8080 (the compose service name)'],
  ['Address of the API', 'http://localhost:5080', 'Not published to your computer; only containers on the compose network can reach it (nginx, db-init)'],
  ['Address / HTTPS', 'Plain HTTP on localhost', 'Plain HTTP in this guide; real systems add HTTPS at nginx or a load balancer'],
];

function Table({ head, rows }) {
  return (
    <table style={{ borderCollapse: 'collapse', marginBottom: 16 }}>
      <thead>
        <tr>
          {head.map((h) => (
            <th key={h} style={cell}>{h}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.map((row) => (
          <tr key={row[0]}>
            {row.map((c, i) => (i === 0 ? <th key={i} scope="row" style={cell}>{c}</th> : <td key={i} style={cell}>{c}</td>))}
          </tr>
        ))}
      </tbody>
    </table>
  );
}

// Asks the API for the accounts through the relative address and says in plain words what came back.
export function ApiCheck() {
  const [result, setResult] = useState(null);

  async function check() {
    setResult({ kind: 'working' });
    try {
      const response = await fetch('/api/accounts');
      const text = await response.text();
      let data = null;
      try {
        data = JSON.parse(text);
      } catch {
        data = null; // not JSON: for example the web server answered with index.html
      }
      if (response.status >= 502 && response.status <= 504) {
        // A proxy (nginx) answers 502/503/504 when the server BEHIND it does not answer.
        setResult({ kind: 'gateway', status: response.status });
      } else if (response.ok && Array.isArray(data)) {
        setResult({ kind: 'ok', status: response.status, count: data.length });
      } else if (data === null) {
        setResult({ kind: 'notjson', status: response.status });
      } else {
        setResult({ kind: 'error', status: response.status });
      }
    } catch {
      setResult({ kind: 'unreachable' });
    }
  }

  return (
    <section>
      <h3>Is the API behind this page reachable?</h3>
      <button onClick={check}>Check GET /api/accounts</button>
      <div role="status" style={{ marginTop: 8 }}>
        {result === null && <p>Not checked yet.</p>}
        {result?.kind === 'working' && <p>Asking...</p>}
        {result?.kind === 'ok' && (
          <p>
            HTTP {result.status}: the API answered with a list of {result.count} account(s).
          </p>
        )}
        {result?.kind === 'error' && <p>The API answered, but with HTTP {result.status} (an error).</p>}
        {result?.kind === 'gateway' && (
          <p>
            HTTP {result.status}: the web server is running, but it got no answer from the API behind it. Is the API
            running? (dotnet run, or docker compose ps)
          </p>
        )}
        {result?.kind === 'notjson' && (
          <p>
            HTTP {result.status}, but the answer is not JSON. Is the /api forwarding set up? (See
            FullStackBank/deploy/nginx.conf.)
          </p>
        )}
        {result?.kind === 'unreachable' && (
          <p>API not reachable — start it (see FullStackBank/README.md or FullStackBank/deploy/README.md).</p>
        )}
      </div>
    </section>
  );
}

export default function Lesson() {
  return (
    <div>
      <h2>What deploying means</h2>
      <p>
        <strong>Code</strong> is the same on every machine. <strong>Configuration</strong> (passwords, addresses, ports)
        differs, so it is passed in when the program starts and is never written into the code. Secrets are never baked into the image either (harmless fixed defaults such as ASPNETCORE_ENVIRONMENT=Production are).
      </p>
      <p>
        <strong>Build</strong>: source code becomes something runnable (an image). <strong>Run</strong>: an image
        becomes a container. Full guide: <code>FullStackBank/deploy/README.md</code>.
      </p>

      <h3>Where does each setting live?</h3>
      <Table head={['Setting', 'Where you write it', 'Where it ends up']} rows={configLocations} />

      <h3>Development vs production in this project</h3>
      <Table head={['Topic', 'Development (dotnet run, npm run dev)', 'Container stack (docker compose)']} rows={devVsProd} />

      <ApiCheck />
    </div>
  );
}
