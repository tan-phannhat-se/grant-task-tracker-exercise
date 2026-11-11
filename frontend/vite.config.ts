import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

// The dev server forwards /api/* to the ASP.NET Core API, so the browser
// talks to one origin and the API needs no CORS setup.
const apiUrl = 'http://localhost:5080'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: apiUrl,
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
        // When the API is down, answer 502 like a real proxy (Vite would send a 500),
        // so the screen can tell the officer the server cannot be reached.
        configure: (proxy) => {
          proxy.on('error', (_error, _request, response) => {
            if ('writeHead' in response && !response.headersSent) {
              response.writeHead(502)
              response.end()
            }
          })
        },
      },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
  },
})
