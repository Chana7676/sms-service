import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// The UI runs on :5173 and forwards API calls to the API (default :5000), so no CORS setup is needed.
const api = process.env.API_URL ?? 'http://localhost:5000';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/messages': api,
      '/employees': api,
    },
  },
});
