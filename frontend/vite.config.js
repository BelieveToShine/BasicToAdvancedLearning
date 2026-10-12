import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// The dev server proxies /api to the Web API from Phase 4/6 so the browser sees ONE origin (no CORS
// setup needed while learning). Set API_URL if your API listens elsewhere.
export default defineConfig({
  plugins: [react()],
  server: { proxy: { '/api': process.env.API_URL ?? 'http://localhost:5080' } },
  test: { environment: 'jsdom', setupFiles: ['./src/test-setup.js'], globals: true },
});
