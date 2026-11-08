import { useEffect, useState } from 'react'
import { Link, Outlet, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Bell, LogOut, Menu } from 'lucide-react'
import { Sidebar } from './Sidebar'
import { Toasts } from '../ui/Toasts'
import { useAuthStore } from '../../stores/authStore'
import { fetchMe, logout } from '../../api/auth'
import { getUnreadCount } from '../../api/operations'
import { toast } from '../../stores/toastStore'

function initials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]!.toUpperCase())
    .join('')
}

export function AppShell() {
  const navigate = useNavigate()
  const { user, accessToken, setUser, clear } = useAuthStore()
  const [mobileNavOpen, setMobileNavOpen] = useState(false)

  const unreadQuery = useQuery({
    queryKey: ['unread-count'],
    queryFn: getUnreadCount,
    refetchInterval: 30_000,
  })

  // Bootstrap the profile after a full page reload (token persisted, user may be stale).
  useEffect(() => {
    if (accessToken && !user) {
      fetchMe()
        .then(setUser)
        .catch(() => {
          // A failed refresh cycle in the API client redirects to /login already.
        })
    }
  }, [accessToken, user, setUser])

  const handleLogout = async () => {
    const { refreshToken } = useAuthStore.getState()
    try {
      if (refreshToken) {
        await logout(refreshToken)
      }
    } catch {
      // Token may already be invalid — clearing locally is what matters.
    }
    clear()
    toast.info('Signed out')
    navigate('/login')
  }

  return (
    <div className="flex h-screen overflow-hidden">
      <div className="hidden lg:block">
        <Sidebar />
      </div>

      {mobileNavOpen && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div
            className="absolute inset-0 bg-navy-950/70 backdrop-blur-sm"
            onClick={() => setMobileNavOpen(false)}
            aria-hidden="true"
          />
          <div className="relative h-full">
            <Sidebar />
          </div>
        </div>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-14 shrink-0 items-center justify-between border-b border-navy-700 bg-navy-900 px-4 lg:px-6">
          <button
            className="rounded-lg p-2 text-slate-400 hover:bg-navy-800 lg:hidden"
            onClick={() => setMobileNavOpen(true)}
            aria-label="Open navigation"
          >
            <Menu className="h-5 w-5" />
          </button>
          <div className="hidden lg:block" />

          <div className="flex items-center gap-2">
            <Link
              to="/notifications"
              className="relative rounded-lg p-2 text-slate-400 hover:bg-navy-800 hover:text-slate-200"
              aria-label={`Notifications${unreadQuery.data ? ` (${unreadQuery.data} unread)` : ''}`}
            >
              <Bell className="h-4 w-4" />
              {(unreadQuery.data ?? 0) > 0 && (
                <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-critical px-1 text-[9px] font-bold text-white">
                  {unreadQuery.data! > 9 ? '9+' : unreadQuery.data}
                </span>
              )}
            </Link>
            {user && (
              <div className="flex items-center gap-2.5">
                <div className="text-right leading-tight">
                  <p className="text-xs font-medium text-slate-200">{user.fullName}</p>
                  <p className="text-[10px] text-slate-500">{user.role}</p>
                </div>
                <span className="flex h-8 w-8 items-center justify-center rounded-full bg-info/15 text-xs font-semibold text-info">
                  {initials(user.fullName)}
                </span>
              </div>
            )}
            <button
              onClick={handleLogout}
              className="rounded-lg p-2 text-slate-500 hover:bg-navy-800 hover:text-slate-200"
              aria-label="Sign out"
              title="Sign out"
            >
              <LogOut className="h-4 w-4" />
            </button>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>

      <Toasts />
    </div>
  )
}
