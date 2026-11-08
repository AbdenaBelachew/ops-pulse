import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Database, Trash2 } from 'lucide-react'
import { createDatabase, deleteDatabase, listDatabases, type DatabaseType } from '../../api/monitoring'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { useRealtimeInvalidation } from '../../realtime/useRealtimeInvalidation'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Field } from '../../components/ui/Field'
import { Modal } from '../../components/ui/Modal'
import { StatusBadge } from '../../components/ui/StatusBadge'

const healthToBadge: Record<string, string> = {
  Healthy: 'Healthy',
  Degraded: 'Degraded',
  Unreachable: 'Unhealthy',
  Unknown: 'Inactive',
}

function AddDatabaseModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [type, setType] = useState<DatabaseType>('PostgreSQL')
  const [host, setHost] = useState('localhost')
  const [port, setPort] = useState(5432)
  const [databaseName, setDatabaseName] = useState('')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')

  const createMutation = useMutation({
    mutationFn: () => createDatabase({ name, databaseType: type, host, port, databaseName, username, password }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['databases'] })
      toast.success('Database registered — credentials stored encrypted, first check within 60s')
      onClose()
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not register the database.')),
  })

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    createMutation.mutate()
  }

  return (
    <Modal open={open} title="Register database" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <Field id="db-name" label="Name" value={name} onChange={(e) => setName(e.target.value)} placeholder="Main database" required />
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label htmlFor="db-type" className="mb-1.5 block text-xs font-medium text-slate-400">Type</label>
            <select
              id="db-type"
              value={type}
              onChange={(e) => {
                const next = e.target.value as DatabaseType
                setType(next)
                setPort(next === 'PostgreSQL' ? 5432 : 1433)
              }}
              className="w-full rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-200 focus:border-info/60 focus:outline-none"
            >
              <option value="PostgreSQL">PostgreSQL</option>
              <option value="SqlServer">SQL Server</option>
            </select>
          </div>
          <Field id="db-port" label="Port" type="number" min={1} max={65535} value={port} onChange={(e) => setPort(Number(e.target.value))} required />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Field id="db-host" label="Host" value={host} onChange={(e) => setHost(e.target.value)} required />
          <Field id="db-name2" label="Database" value={databaseName} onChange={(e) => setDatabaseName(e.target.value)} required />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Field id="db-user" label="Username" value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="off" required />
          <Field id="db-pass" label="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" hint="Encrypted with AES-GCM before storage." required />
        </div>
        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="secondary" onClick={onClose}>Cancel</Button>
          <Button type="submit" loading={createMutation.isPending}>Register</Button>
        </div>
      </form>
    </Modal>
  )
}

export function DatabasesPage() {
  const canManage = useAuthStore((s) => ['SuperAdmin', 'OrganizationAdmin', 'DevOpsEngineer'].includes(s.user?.role ?? ''))
  const queryClient = useQueryClient()
  const [addOpen, setAddOpen] = useState(false)
  const [deleting, setDeleting] = useState<string | null>(null)

  useRealtimeInvalidation({ MetricsUpdated: ['databases'] })

  const databasesQuery = useQuery({
    queryKey: ['databases'],
    queryFn: () => listDatabases({ page: 1, pageSize: 50 }),
    refetchInterval: 60_000,
    placeholderData: (p) => p,
  })

  const deleteMutation = useMutation({
    mutationFn: deleteDatabase,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['databases'] })
      toast.success('Database connection removed')
      setDeleting(null)
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not remove the connection.')),
  })

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Databases</h1>
          <p className="mt-1 text-sm text-slate-500">PostgreSQL and SQL Server availability, connections and size.</p>
        </div>
        {canManage && <Button onClick={() => setAddOpen(true)}><Database className="h-4 w-4" />Register database</Button>}
      </div>

      <div className="mt-6 grid grid-cols-1 gap-3 lg:grid-cols-2">
        {databasesQuery.isLoading ? (
          <Card className="animate-pulse lg:col-span-2"><div className="h-20 rounded bg-navy-700" /></Card>
        ) : (databasesQuery.data?.items.length ?? 0) === 0 ? (
          <Card className="lg:col-span-2">
            <p className="py-8 text-center text-sm text-slate-500">No databases registered yet.</p>
          </Card>
        ) : (
          databasesQuery.data?.items.map((database) => (
            <Card key={database.id}>
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <p className="truncate text-sm font-semibold text-white">{database.name}</p>
                    <StatusBadge status={healthToBadge[database.health] ?? 'Inactive'} />
                  </div>
                  <p className="mt-0.5 truncate text-xs text-slate-500">
                    {database.databaseType} · {database.host}:{database.port}/{database.databaseName}
                  </p>
                  <p className="mt-3 text-[11px] text-slate-600">
                    Last check: {database.lastCheckedAtUtc ? new Date(database.lastCheckedAtUtc).toLocaleTimeString() : 'pending…'}
                  </p>
                </div>
                {canManage && (
                  <button
                    onClick={() => setDeleting(database.id)}
                    aria-label={`Remove ${database.name}`}
                    className="rounded-lg p-2 text-slate-500 hover:bg-critical/10 hover:text-critical"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                )}
              </div>
            </Card>
          ))
        )}
      </div>

      <AddDatabaseModal open={addOpen} onClose={() => setAddOpen(false)} />

      <Modal open={deleting !== null} title="Remove database connection" onClose={() => setDeleting(null)}>
        <p className="text-sm text-slate-400">Remove this connection and its metric history? Stored credentials are deleted with it.</p>
        <div className="mt-5 flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setDeleting(null)}>Cancel</Button>
          <Button variant="danger" loading={deleteMutation.isPending} onClick={() => deleting && deleteMutation.mutate(deleting)}>Remove</Button>
        </div>
      </Modal>
    </div>
  )
}
