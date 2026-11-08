import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthResponse, UserDto } from '../api/auth'

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  expiresAtUtc: string | null
  user: UserDto | null
  setAuth: (auth: AuthResponse) => void
  setUser: (user: UserDto) => void
  setTokens: (accessToken: string, refreshToken: string, expiresAtUtc: string) => void
  clear: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      expiresAtUtc: null,
      user: null,
      setAuth: (auth) =>
        set({
          accessToken: auth.accessToken,
          refreshToken: auth.refreshToken,
          expiresAtUtc: auth.expiresAtUtc,
          user: auth.user,
        }),
      setUser: (user) => set({ user }),
      setTokens: (accessToken, refreshToken, expiresAtUtc) =>
        set({ accessToken, refreshToken, expiresAtUtc }),
      clear: () =>
        set({ accessToken: null, refreshToken: null, expiresAtUtc: null, user: null }),
    }),
    { name: 'opspulse-auth' },
  ),
)
