import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api': 'http://localhost:7146',
      '/health': 'http://localhost:7146',
    },
  },
  test: {
    environment: 'jsdom',
  },
})
