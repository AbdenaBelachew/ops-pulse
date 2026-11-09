import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Activity, Database, Pause, Pencil, Play, Trash2 } from 'lucide-react'
import {
  checkDatabase,
  createDatabase,
  getDatabase,
  listDatabases,
  updateDatabase,
  type DatabaseConnectionDto,
  type DatabaseType,
} from '../../api/monitoring'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { useRealtimeInvalidation } from '../../realtime/useRealtimeInvalidation'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Field } from '../../components/ui/Field'
import { Modal } from '../../components/ui/Modal'
import { StatusBadge } from '../../components/ui/StatusBadge'
import {
  MANAGE_ROLES,
  OPERATE_ROLES,
  announceCheck,
  formatSize,
  healthToBadge,
  useDatabaseActions,
  utilization,
} from './databaseHelpers'

/** Register (no `existing`) or edit (with `existing`) a database connection. */
export function DatabaseFormModal({
  open,
  existing,
  onClose,
}: {
  open: boolean
  existing?: DatabaseConnectionDto | null
  onClose: () => void
}) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [type, setType] = useState<DatabaseType>('PostgreSQL')
  const [host, setHost] = useState('localhost')
  const [port, setPort] = useState(5432)
  const [databaseName, setDatabaseName] = useState('')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')

  useEffect(() => {
    if (open) {
      setName(existing?.name ?? '')
      setType(existing?.databaseType ?? 'PostgreSQL')
      setHost(existing?.host ?? 'localhost')
      setPort(existing?.port ?? 5432)
      setDatabaseName(existing?.databaseName ?? '')
      setUsername(existing?.username ?? '')
      setPassword('')
    }
  }, [open, existing])

  const saveMutation = useMutation({
    mutationFn: () =>
      existing
        ? updateDatabase(existing.id, {
            name,
            host,
            port,
            databaseName,
            username,
            // Empty = keep the stored password.
            ...(password ? { password } : {}),
          })
        : createDatabase({ name, databaseType: type, host, port, databaseName, username, password }),
    onSuccess: async (saved) => {
      queryClient.invalidateQueries({ queryKey: ['databases'] })
      onClose()
      if (existing) {
        toast.success('Connection updated — testing it now…')
        // Validate the new settings straight away instead of waiting up to 60s.
        try {
          announceCheck(saved.name, await checkDatabase(saved.id))
          queryClient.invalidateQueries({ queryKey: ['databases'] })
        } catch (error) {
          toast.error(extractApiError(error, 'Connection test failed.'))
        }
      } else {
        toast.success('Database registered — credentials stored encrypted, first check within 60s')
      }
    },
    onError: (e) => toast.error(extractApiError(e, existing ? 'Could not update the connection.' : 'Could not register the database.')),
  })

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    saveMutation.mutate()
  }

  return (
    <Modal open={open} title={existing ? `Edit ${existing.name}` : 'Register database'} onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <Field id="db-name" label="Name" value={name} onChange={(e) => setName(e.target.value)} placeholder="Main database" required />
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label htmlFor="db-type" className="mb-1.5 block text-xs font-medium text-slate-400">Type</label>
            <select
              id="db-type"
              value={type}
              disabled={!!existing}
              onChange={(e) => {
                const next = e.target.value as DatabaseType
                setType(next)
                setPort(next === 'PostgreSQL' ? 5432 : 1433)
              }}
              className="w-full rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-200 focus:border-info/60 focus:outline-none disabled:opacity-60"
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
          <Field
            id="db-pass"
            label={existing ? 'New password' : 'Password'}
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="new-password"
            placeholder={existing ? 'Leave empty to keep current' : undefined}
            hint="Encrypted with AES-GCM before storage."
            required={!existing}
          />
        </div>
        <p className="text-[11px] leading-relaxed text-slate-600">
          Use a read-only login. PostgreSQL needs access to <span className="text-slate-400">pg_stat_activity</span>;
          SQL Server needs <span className="text-slate-400">VIEW SERVER STATE</span>.
        </p>
        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="secondary" onClick={onClose}>Cancel</Button>
          <Button type="submit" loading={saveMutation.isPending}>{existing ? 'Save & test' : 'Register'}</Button>
        </div>
      </form>
    </Modal>
  )
}

const iconButton = 'rounded-lg p-2 text-slate-500 hover:bg-navy-700 hover:text-slate-200 disabled:opacity-40'

function DatabaseCard({
  database,
  canManage,
  canOperate,
  onEdit,
  onDelete,
  actions,
}: {
  database: DatabaseConnectionDto
  canManage: boolean
  canOperate: boolean
  onEdit: () => void
  onDelete: () => void
  actions: ReturnType<typeof useDatabaseActions>
}) {
  // Latest sample for the inline stats (the details endpoint returns recent history).
  const detailsQuery = useQuery({
    queryKey: ['databases', database.id],
    queryFn: () => getDatabase(database.id),
    refetchInterval: 60_000,
  })
  const latest = detailsQuery.data?.recentMetrics.at(-1)
  const util = utilization(latest)
  const checking = actions.checkMutation.isPending && actions.checkMutation.variables?.id === database.id

  return (
    <Card className={database.isActive ? '' : 'opacity-70'}>
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <Link to={`/databases/${database.id}`} className="truncate text-sm font-semibold text-white hover:text-info hover:underline">
              {database.name}
            </Link>
            <StatusBadge status={database.isActive ? healthToBadge[database.health] ?? 'Inactive' : 'Inactive'} />
            {!database.isActive && <span className="text-[11px] text-slate-500">paused</span>}
          </div>
          <p className="mt-0.5 truncate text-xs text-slate-500">
            {database.databaseType === 'SqlServer' ? 'SQL Server' : 'PostgreSQL'} · {database.host}:{database.port}/{database.databaseName} · {database.username}
          </p>
        </div>
        <div className="flex shrink-0 items-center">
          {canOperate && (
            <button onClick={() => actions.checkMutation.mutate(database)} disabled={checking} aria-label={`Check ${database.name} now`} title="Check now / test connection" className={iconButton}>
              <Activity className={`h-4 w-4 ${checking ? 'animate-pulse text-info' : ''}`} />
            </button>
          )}
          {canManage && (
            <>
              <button onClick={() => actions.toggleMutation.mutate(database)} aria-label={database.isActive ? `Pause ${database.name}` : `Resume ${database.name}`} title={database.isActive ? 'Pause monitoring' : 'Resume monitoring'} className={iconButton}>
                {database.isActive ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
              </button>
              <button onClick={onEdit} aria-label={`Edit ${database.name}`} title="Edit connection" className={iconButton}>
                <Pencil className="h-4 w-4" />
              </button>
              <button onClick={onDelete} aria-label={`Remove ${database.name}`} title="Remove" className="rounded-lg p-2 text-slate-500 hover:bg-critical/10 hover:text-critical">
                <Trash2 className="h-4 w-4" />
              </button>
            </>
          )}
        </div>
      </div>

      <div className="mt-4 grid grid-cols-4 gap-3 text-xs">
        <Stat label="Connections" value={latest?.activeConnections != null ? `${latest.activeConnections}${latest.maxConnections ? ` / ${latest.maxConnections}` : ''}` : '—'} />
        <Stat label="Utilization" value={util !== null ? `${util}%` : '—'} tone={util !== null && util >= 80 ? 'text-warning' : undefined} />
        <Stat label="Size" value={formatSize(latest?.databaseSizeMb)} />
        <Stat label="Latency" value={latest?.responseTimeMs != null ? `${latest.responseTimeMs} ms` : '—'} tone={latest?.responseTimeMs && latest.responseTimeMs > 2000 ? 'text-warning' : undefined} />
      </div>

      {latest && !latest.isReachable && latest.errorMessage && (
        <p className="mt-3 truncate rounded-md bg-critical/10 px-2 py-1 text-[11px] text-critical" title={latest.errorMessage}>{latest.errorMessage}</p>
      )}
      <p className="mt-3 text-[11px] text-slate-600">
        Last check: {database.lastCheckedAtUtc ? new Date(database.lastCheckedAtUtc).toLocaleString() : 'pending…'}
      </p>
    </Card>
  )
}

export function Stat({ label, value, tone }: { label: string; value: string; tone?: string }) {
  return (
    <div>
      <p className="text-[10px] uppercase tracking-wide text-slate-600">{label}</p>
      <p className={`mt-0.5 text-sm font-semibold ${tone ?? 'text-white'}`}>{value}</p>
    </div>
  )
}

export function DatabasesPage() {
  const role = useAuthStore((s) => s.user?.role ?? '')
  const canManage = MANAGE_ROLES.includes(role)
  const canOperate = OPERATE_ROLES.includes(role)
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<DatabaseConnectionDto | null>(null)
  const [deleting, setDeleting] = useState<DatabaseConnectionDto | null>(null)
  const [search, setSearch] = useState('')
  const actions = useDatabaseActions()

  useRealtimeInvalidation({ MetricsUpdated: ['databases'] })

  const databasesQuery = useQuery({
    queryKey: ['databases', 'list', search],
    queryFn: () => listDatabases({ page: 1, pageSize: 100, search }),
    refetchInterval: 60_000,
    placeholderData: (p) => p,
  })

  const items = databasesQuery.data?.items ?? []
  const counts = {
    total: items.length,
    healthy: items.filter((d) => d.isActive && d.health === 'Healthy').length,
    degraded: items.filter((d) => d.isActive && d.health === 'Degraded').length,
    down: items.filter((d) => d.isActive && d.health === 'Unreachable').length,
    paused: items.filter((d) => !d.isActive).length,
  }

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Databases</h1>
          <p className="mt-1 text-sm text-slate-500">PostgreSQL and SQL Server availability, connections and size — probed every 60s.</p>
        </div>
        <div className="flex items-center gap-2">
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search name or host…"
            aria-label="Search databases"
            className="w-48 rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-200 placeholder:text-slate-600 focus:border-info/60 focus:outline-none"
          />
          {canManage && (
            <Button onClick={() => { setEditing(null); setFormOpen(true) }}>
              <Database className="h-4 w-4" />Register database
            </Button>
          )}
        </div>
      </div>

      {counts.total > 0 && (
        <div className="mt-4 flex flex-wrap gap-4 text-xs text-slate-500">
          <span><span className="font-semibold text-white">{counts.total}</span> total</span>
          <span><span className="font-semibold text-healthy">{counts.healthy}</span> healthy</span>
          <span><span className="font-semibold text-warning">{counts.degraded}</span> degraded</span>
          <span><span className="font-semibold text-critical">{counts.down}</span> unreachable</span>
          <span><span className="font-semibold text-slate-300">{counts.paused}</span> paused</span>
        </div>
      )}

      <div className="mt-4 grid grid-cols-1 gap-3 lg:grid-cols-2">
        {databasesQuery.isLoading ? (
          <Card className="animate-pulse lg:col-span-2"><div className="h-20 rounded bg-navy-700" /></Card>
        ) : items.length === 0 ? (
          <Card className="lg:col-span-2">
            <p className="py-8 text-center text-sm text-slate-500">{search ? 'No databases match your search.' : 'No databases registered yet.'}</p>
          </Card>
        ) : (
          items.map((database) => (
            <DatabaseCard
              key={database.id}
              database={database}
              canManage={canManage}
              canOperate={canOperate}
              actions={actions}
              onEdit={() => { setEditing(database); setFormOpen(true) }}
              onDelete={() => setDeleting(database)}
            />
          ))
        )}
      </div>

      <DatabaseFormModal open={formOpen} existing={editing} onClose={() => setFormOpen(false)} />

      <Modal open={deleting !== null} title="Remove database connection" onClose={() => setDeleting(null)}>
        <p className="text-sm text-slate-400">
          Remove <span className="text-white">{deleting?.name}</span> and its metric history? Stored credentials are deleted with it.
          To stop monitoring temporarily, use <span className="text-white">Pause</span> instead.
        </p>
        <div className="mt-5 flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setDeleting(null)}>Cancel</Button>
          <Button
            variant="danger"
            loading={actions.deleteMutation.isPending}
            onClick={() => deleting && actions.deleteMutation.mutate(deleting.id, { onSuccess: () => setDeleting(null) })}
          >
            Remove
          </Button>
        </div>
      </Modal>
    </div>
  )
}
