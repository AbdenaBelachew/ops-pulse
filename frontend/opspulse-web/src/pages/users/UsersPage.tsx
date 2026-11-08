import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Search, ShieldOff, UserPlus } from 'lucide-react'
import {
  createUser,
  listUsers,
  updateUser,
  type PagedResult,
} from '../../api/users'
import type { RoleName, UserDto } from '../../api/auth'
import { listOrganizations } from '../../api/organizations'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Field } from '../../components/ui/Field'
import { Modal } from '../../components/ui/Modal'
import { ConfirmDialog } from '../../components/ui/Modal'
import { StatusBadge } from '../../components/ui/StatusBadge'

const roleStyles: Record<string, string> = {
  SuperAdmin: 'bg-info/10 text-info border-info/30',
  OrganizationAdmin: 'bg-violet-500/10 text-violet-300 border-violet-500/30',
  DevOpsEngineer: 'bg-cyan-500/10 text-cyan-300 border-cyan-500/30',
  Operator: 'bg-amber-500/10 text-amber-300 border-amber-500/30',
  Viewer: 'bg-slate-500/10 text-slate-400 border-slate-500/30',
}

const roleLabels: Record<RoleName, string> = {
  SuperAdmin: 'Super Admin',
  OrganizationAdmin: 'Org Admin',
  DevOpsEngineer: 'DevOps Engineer',
  Operator: 'Operator',
  Viewer: 'Viewer',
}

const assignableRoles: RoleName[] = [
  'OrganizationAdmin',
  'DevOpsEngineer',
  'Operator',
  'Viewer',
]

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function initials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]!.toUpperCase())
    .join('')
}

function CreateUserModal({
  open,
  onClose,
  isSuperAdmin,
}: {
  open: boolean
  onClose: () => void
  isSuperAdmin: boolean
}) {
  const queryClient = useQueryClient()
  const [email, setEmail] = useState('')
  const [fullName, setFullName] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<RoleName>('DevOpsEngineer')
  const [organizationId, setOrganizationId] = useState('')

  useEffect(() => {
    if (open) {
      setEmail('')
      setFullName('')
      setPassword('')
      setRole('DevOpsEngineer')
      setOrganizationId('')
    }
  }, [open])

  const orgsQuery = useQuery({
    queryKey: ['organizations'],
    queryFn: () => listOrganizations(),
    enabled: open && isSuperAdmin,
  })

  const createMutation = useMutation({
    mutationFn: createUser,
    onSuccess: (user) => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast.success(`User ${user.email} created as ${roleLabels[user.role]}`)
      onClose()
    },
    onError: (error) => toast.error(extractApiError(error, 'Could not create the user.')),
  })

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    createMutation.mutate({
      email,
      fullName,
      password,
      role,
      organizationId: role === 'SuperAdmin' ? undefined : organizationId || undefined,
    })
  }

  const roles: RoleName[] = isSuperAdmin ? ['SuperAdmin', ...assignableRoles] : assignableRoles

  return (
    <Modal open={open} title="Create user" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <Field
          id="create-email"
          label="Email"
          type="email"
          placeholder="teammate@company.com"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
        />
        <Field
          id="create-name"
          label="Full name"
          placeholder="Jane Doe"
          value={fullName}
          onChange={(event) => setFullName(event.target.value)}
          required
        />
        <Field
          id="create-password"
          label="Temporary password"
          type="password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          minLength={10}
          hint="Min 10 chars with upper, lower, digit and special character."
          required
        />
        <div>
          <label htmlFor="create-role" className="mb-1.5 block text-xs font-medium text-slate-400">
            Role
          </label>
          <select
            id="create-role"
            value={role}
            onChange={(event) => setRole(event.target.value as RoleName)}
            className="w-full rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-200 focus:border-info/60 focus:outline-none focus:ring-2 focus:ring-info/20"
          >
            {roles.map((option) => (
              <option key={option} value={option}>
                {roleLabels[option]}
              </option>
            ))}
          </select>
        </div>
        {isSuperAdmin && role !== 'SuperAdmin' && (
          <div>
            <label htmlFor="create-organization" className="mb-1.5 block text-xs font-medium text-slate-400">
              Organization
            </label>
            <select
              id="create-organization"
              value={organizationId}
              onChange={(event) => setOrganizationId(event.target.value)}
              required
              className="w-full rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-200 focus:border-info/60 focus:outline-none focus:ring-2 focus:ring-info/20"
            >
              <option value="">Select organization…</option>
              {orgsQuery.data?.items.map((organization) => (
                <option key={organization.id} value={organization.id}>
                  {organization.name}
                </option>
              ))}
            </select>
          </div>
        )}

        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={createMutation.isPending}>
            Create user
          </Button>
        </div>
      </form>
    </Modal>
  )
}

export function UsersPage() {
  const currentUser = useAuthStore((state) => state.user)
  const queryClient = useQueryClient()

  const [page, setPage] = useState(1)
  const [term, setTerm] = useState('')
  const [search, setSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState<RoleName | ''>('')
  const [statusFilter, setStatusFilter] = useState<boolean | ''>('')
  const [createOpen, setCreateOpen] = useState(false)
  const [toggleTarget, setToggleTarget] = useState<UserDto | null>(null)

  const canManage =
    currentUser?.role === 'SuperAdmin' || currentUser?.role === 'OrganizationAdmin'

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setSearch(term)
      setPage(1)
    }, 300)
    return () => window.clearTimeout(timer)
  }, [term])

  const usersQuery = useQuery({
    queryKey: ['users', page, search, roleFilter, statusFilter],
    queryFn: () =>
      listUsers({ page, pageSize: 10, search, role: roleFilter, isActive: statusFilter }),
    placeholderData: (previous) => previous,
  })

  const toggleMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      updateUser(id, { isActive }),
    onSuccess: (updated: UserDto) => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast.success(
        `${updated.email} ${updated.isActive ? 'activated' : 'deactivated — all sessions revoked'}`,
      )
      setToggleTarget(null)
    },
    onError: (error) => {
      toast.error(extractApiError(error, 'Could not update the user.'))
      setToggleTarget(null)
    },
  })

  const data: PagedResult<UserDto> | undefined = usersQuery.data

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Users</h1>
          <p className="mt-1 text-sm text-slate-500">
            Manage accounts and roles
            {currentUser?.organizationName ? ` for ${currentUser.organizationName}` : ''}.
          </p>
        </div>
        {canManage && (
          <Button onClick={() => setCreateOpen(true)}>
            <UserPlus className="h-4 w-4" aria-hidden="true" />
            Create user
          </Button>
        )}
      </div>

      <div className="mt-6 flex flex-wrap items-center gap-3">
        <div className="relative min-w-56 flex-1">
          <Search
            className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-600"
            aria-hidden="true"
          />
          <input
            value={term}
            onChange={(event) => setTerm(event.target.value)}
            placeholder="Search by name or email…"
            aria-label="Search users"
            className="w-full rounded-lg border border-navy-600 bg-navy-900 py-2 pl-9 pr-3 text-sm text-slate-200 placeholder:text-slate-600 focus:border-info/60 focus:outline-none focus:ring-2 focus:ring-info/20"
          />
        </div>
        <select
          value={roleFilter}
          onChange={(event) => {
            setRoleFilter(event.target.value as RoleName | '')
            setPage(1)
          }}
          aria-label="Filter by role"
          className="rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-300 focus:border-info/60 focus:outline-none"
        >
          <option value="">All roles</option>
          {(Object.keys(roleLabels) as RoleName[]).map((option) => (
            <option key={option} value={option}>
              {roleLabels[option]}
            </option>
          ))}
        </select>
        <select
          value={String(statusFilter)}
          onChange={(event) => {
            setStatusFilter(
              event.target.value === '' ? '' : event.target.value === 'true',
            )
            setPage(1)
          }}
          aria-label="Filter by status"
          className="rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-300 focus:border-info/60 focus:outline-none"
        >
          <option value="">All statuses</option>
          <option value="true">Active</option>
          <option value="false">Inactive</option>
        </select>
      </div>

      <Card className="mt-4 overflow-hidden p-0">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-navy-700 text-xs text-slate-500">
                <th scope="col" className="px-5 py-3 font-medium">User</th>
                <th scope="col" className="px-5 py-3 font-medium">Role</th>
                <th scope="col" className="px-5 py-3 font-medium">Status</th>
                <th scope="col" className="px-5 py-3 font-medium">Created</th>
                {canManage && <th scope="col" className="px-5 py-3 font-medium"><span className="sr-only">Actions</span></th>}
              </tr>
            </thead>
            <tbody>
              {usersQuery.isLoading ? (
                <tr>
                  <td colSpan={5} className="px-5 py-10 text-center text-slate-600">Loading users…</td>
                </tr>
              ) : (data?.items.length ?? 0) === 0 ? (
                <tr>
                  <td colSpan={5} className="px-5 py-10 text-center text-slate-600">
                    No users match the current filters.
                  </td>
                </tr>
              ) : (
                data?.items.map((user) => (
                  <tr key={user.id} className="border-b border-navy-800 last:border-0 hover:bg-navy-800/40">
                    <td className="px-5 py-3.5">
                      <div className="flex items-center gap-3">
                        <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-navy-700 text-[11px] font-semibold text-slate-300">
                          {initials(user.fullName)}
                        </span>
                        <div className="min-w-0">
                          <p className="truncate font-medium text-slate-200">
                            {user.fullName}
                            {user.id === currentUser?.id && (
                              <span className="ml-2 rounded bg-navy-700 px-1.5 py-0.5 text-[10px] text-slate-400">you</span>
                            )}
                          </p>
                          <p className="truncate text-xs text-slate-500">{user.email}</p>
                        </div>
                      </div>
                    </td>
                    <td className="px-5 py-3.5">
                      <span className={`inline-block rounded-full border px-2.5 py-0.5 text-xs font-medium ${roleStyles[user.role] ?? roleStyles.Viewer}`}>
                        {roleLabels[user.role] ?? user.role}
                      </span>
                    </td>
                    <td className="px-5 py-3.5">
                      <StatusBadge status={user.isActive ? 'Active' : 'Inactive'} />
                    </td>
                    <td className="px-5 py-3.5 text-xs text-slate-500">{formatDate(user.createdAtUtc)}</td>
                    {canManage && (
                      <td className="px-5 py-3.5">
                        <div className="flex justify-end">
                          <button
                            onClick={() => setToggleTarget(user)}
                            disabled={user.id === currentUser?.id}
                            title={
                              user.id === currentUser?.id
                                ? 'You cannot deactivate your own account'
                                : user.isActive
                                  ? 'Deactivate user'
                                  : 'Activate user'
                            }
                            className="rounded-lg p-2 text-slate-500 transition-colors hover:bg-navy-700 hover:text-slate-200 disabled:cursor-not-allowed disabled:opacity-30"
                          >
                            <ShieldOff className="h-4 w-4" aria-hidden="true" />
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {data && data.totalCount > 0 && (
          <div className="flex items-center justify-between border-t border-navy-700 px-5 py-3 text-xs text-slate-500">
            <span>
              Page {data.page} of {Math.max(1, data.totalPages)} · {data.totalCount} users
            </span>
            <div className="flex gap-2">
              <Button variant="secondary" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                Previous
              </Button>
              <Button
                variant="secondary"
                disabled={page >= data.totalPages}
                onClick={() => setPage((p) => p + 1)}
              >
                Next
              </Button>
            </div>
          </div>
        )}
      </Card>

      <CreateUserModal open={createOpen} onClose={() => setCreateOpen(false)} isSuperAdmin={currentUser?.role === 'SuperAdmin'} />

      <ConfirmDialog
        open={toggleTarget !== null}
        title={toggleTarget?.isActive ? 'Deactivate user' : 'Activate user'}
        message={
          toggleTarget?.isActive
            ? `${toggleTarget?.email} will be signed out everywhere and unable to sign in until reactivated.`
            : `${toggleTarget?.email} will regain access with their current role.`
        }
        confirmLabel={toggleTarget?.isActive ? 'Deactivate' : 'Activate'}
        danger={toggleTarget?.isActive}
        loading={toggleMutation.isPending}
        onConfirm={() => {
          if (toggleTarget) {
            toggleMutation.mutate({ id: toggleTarget.id, isActive: !toggleTarget.isActive })
          }
        }}
        onCancel={() => setToggleTarget(null)}
      />
    </div>
  )
}
