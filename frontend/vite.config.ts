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
      },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
  },
})
