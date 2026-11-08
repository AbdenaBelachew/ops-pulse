import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { register } from '../../api/auth'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { Button } from '../../components/ui/Button'
import { Field } from '../../components/ui/Field'

export function RegisterPage() {
  const navigate = useNavigate()
  const { accessToken, setAuth } = useAuthStore()

  const [organizationName, setOrganizationName] = useState('')
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)

  if (accessToken) {
    return <Navigate to="/" replace />
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setLoading(true)
    try {
      const auth = await register({ organizationName, fullName, email, password })
      setAuth(auth)
      toast.success(`Organization "${auth.user.organizationName}" is ready`)
      navigate('/', { replace: true })
    } catch (error) {
      toast.error(extractApiError(error, 'Registration failed. Please check your inputs.'))
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
          <h1 className="mt-4 text-xl font-semibold text-white">Create your workspace</h1>
          <p className="mt-1 text-sm text-slate-500">
            You will be the administrator of your organization
          </p>
        </div>

        <form onSubmit={handleSubmit} className="rounded-xl border border-navy-700 bg-navy-850 p-6 shadow-2xl">
          <div className="space-y-4">
            <Field
              id="register-organization"
              label="Organization name"
              placeholder="Acme Corp"
              value={organizationName}
              onChange={(event) => setOrganizationName(event.target.value)}
              minLength={3}
              required
            />
            <Field
              id="register-name"
              label="Your full name"
              placeholder="Jane Doe"
              autoComplete="name"
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
              required
            />
            <Field
              id="register-email"
              label="Work email"
              type="email"
              autoComplete="email"
              placeholder="jane@acme.io"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
            />
            <Field
              id="register-password"
              label="Password"
              type="password"
              autoComplete="new-password"
              placeholder="••••••••••"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              minLength={10}
              hint="At least 10 characters with upper, lower, digit and special character."
              required
            />
          </div>

          <Button type="submit" loading={loading} className="mt-5 w-full">
            Create organization
          </Button>
        </form>

        <p className="mt-4 text-center text-xs text-slate-600">
          Already have an account?{' '}
          <Link to="/login" className="text-info hover:underline">
            Sign in
          </Link>
        </p>
      </div>
    </div>
  )
}
