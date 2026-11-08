import axios from 'axios'
import { useAuthStore } from '../stores/authStore'
import type { AuthResponse } from './auth'

/** Client for the versioned OpsPulse REST API, with auth + refresh handling. */
export const apiClient = axios.create({
  baseURL: '/api/v1',
  timeout: 10_000,
})

/** Client for infrastructure endpoints outside /api/v1 (e.g. /health). */
export const infraClient = axios.create({
  baseURL: '/',
  timeout: 10_000,
})

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

let refreshPromise: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const { refreshToken, setTokens, setUser, clear } = useAuthStore.getState()
  if (!refreshToken) {
    return null
  }

  try {
    // Bare axios call: bypasses interceptors so a failing refresh never loops.
    const { data } = await axios.post<AuthResponse>('/api/v1/auth/refresh', { refreshToken })
    setTokens(data.accessToken, data.refreshToken, data.expiresAtUtc)
    setUser(data.user)
    return data.accessToken
  } catch {
    clear()
    window.location.href = '/login'
    return null
  }
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config as
      | (typeof error.config & { _retry?: boolean })
      | undefined
    const url: string = original?.url ?? ''

    if (
      error.response?.status === 401 &&
      original &&
      !original._retry &&
      !url.includes('/auth/')
    ) {
      original._retry = true
      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null
      })
      const newToken = await refreshPromise
      if (newToken) {
        original.headers.Authorization = `Bearer ${newToken}`
        return apiClient(original)
      }
    }

    return Promise.reject(error)
  },
)

/** Extracts the API's problem-details message, with a fallback. */
export function extractApiError(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { detail?: string; title?: string } | undefined
    return data?.detail ?? data?.title ?? fallback
  }
  return fallback
}
