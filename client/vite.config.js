import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// The /api proxy makes the browser see same-origin requests in dev:
// no preflight, no CORS, no mixed content. Target must match the API's
// pinned port in server/FitBit.Api/Properties/launchSettings.json.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5099',
        changeOrigin: true,
      },
    },
  },
});
