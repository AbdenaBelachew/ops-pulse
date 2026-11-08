import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { login } from '../../api/auth'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { Button } from '../../components/ui/Button'
import { Field } from '../../components/ui/Field'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { accessToken, setAuth } = useAuthStore()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)

  if (accessToken) {
    return <Navigate to="/" replace />
  }

  const from = (location.state as { from?: string } | null)?.from ?? '/'

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setLoading(true)
    try {
      const auth = await login({ email, password })
      setAuth(auth)
      toast.success(`Welcome back, ${auth.user.fullName}`)
      navigate(from, { replace: true })
    } catch (error) {
      toast.error(extractApiError(error, 'Unable to sign in. Please try again.'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <span className="mx-auto flex h-12 w-12 items-center justify-center rounded-xl bg-info/15 text-info">
            <svg
              className="h-6 w-6"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              <path d="M22 12h-2.48a2 2 0 0 0-1.93 1.46l-2.35 8.36a.25.25 0 0 1-.48 0L9.24 2.18a.25.25 0 0 0-.48 0l-2.35 8.36A2 2 0 0 1 4.49 12H2" />
            </svg>
          </span>
          <h1 className="mt-4 text-xl font-semibold text-white">OpsPulse</h1>
          <p className="mt-1 text-sm text-slate-500">Enterprise infrastructure monitoring</p>
        </div>

        <form onSubmit={handleSubmit} className="rounded-xl border border-navy-700 bg-navy-850 p-6 shadow-2xl">
          <h2 className="text-sm font-semibold text-white">Sign in</h2>
          <p className="mt-0.5 text-xs text-slate-500">Access your monitoring workspace</p>

          <div className="mt-5 space-y-4">
            <Field
              id="login-email"
              label="Email"
              type="email"
              autoComplete="email"
              placeholder="you@company.com"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
            />
            <Field
              id="login-password"
              label="Password"
              type="password"
              autoComplete="current-password"
              placeholder="••••••••••"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
            />
          </div>

          <Button type="submit" loading={loading} className="mt-5 w-full">
            Sign in
          </Button>
        </form>

        <p className="mt-4 text-center text-xs text-slate-600">
          Starting out?{' '}
          <Link to="/register" className="text-info hover:underline">
            Create your organization
          </Link>
        </p>

        <div className="mt-6 rounded-lg border border-navy-700 bg-navy-900 px-3.5 py-2.5 text-center text-[11px] leading-relaxed text-slate-600">
          Development seed account:{' '}
          <span className="text-slate-400">admin@opspulse.dev</span> /{' '}
          <span className="text-slate-400">Admin#12345</span>
        </div>
      </div>
    </div>
  )
}
