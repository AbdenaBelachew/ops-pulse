import axios from 'axios'

/**
 * Client for the versioned OpsPulse REST API. The Vite dev server proxies
 * /api and /hubs to the backend, so no CORS handling is needed locally.
 */
export const apiClient = axios.create({
  baseURL: '/api/v1',
  timeout: 10_000,
  headers: { 'Content-Type': 'application/json' },
})

/** Client for infrastructure endpoints that live outside /api/v1 (e.g. /health). */
export const infraClient = axios.create({
  baseURL: '/',
  timeout: 10_000,
})
