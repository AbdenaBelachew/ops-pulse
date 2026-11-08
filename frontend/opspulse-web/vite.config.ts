import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      // Infrastructure probes live at the API root (used by Kubernetes later).
      '/health': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      // SignalR hub proxy arrives with Milestone 5; wired here so the
      // frontend config never needs to change.
      '/hubs': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
