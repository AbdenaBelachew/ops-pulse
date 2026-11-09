import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Activity, ArrowLeft, Pause, Pencil, Play, Trash2 } from 'lucide-react'
import { Area, AreaChart, CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getDatabase } from '../../api/monitoring'
import { useAuthStore } from '../../stores/authStore'
import { useRealtimeInvalidation } from '../../realtime/useRealtimeInvalidation'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Modal } from '../../components/ui/Modal'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { DatabaseFormModal, Stat } from './DatabasesPage'
import { MANAGE_ROLES, OPERATE_ROLES, formatSize, healthToBadge, useDatabaseActions, utilization } from './databaseHelpers'

const tooltipStyle = {
  contentStyle: { background: '#121a30', border: '1px solid #2a3860', borderRadius: 8, fontSize: 12 },
  labelStyle: { color: '#94a3b8' },
}

function ChartCard({ title, subtitle, children, empty }: { title: string; subtitle?: string; children: React.ReactNode; empty: boolean }) {
  return (
    <Card>
      <div className="flex items-baseline justify-between gap-2">
        <h2 className="text-sm font-semibold text-white">{title}</h2>
        {subtitle && <span className="text-[11px] text-slate-600">{subtitle}</span>}
      </div>
      <div className="mt-4 h-56">
        {empty ? (
          <p className="flex h-full items-center justify-center text-sm text-slate-600">No samples yet — probes run every 60s.</p>
        ) : (
          <ResponsiveContainer width="100%" height="100%">{children as React.ReactElement}</ResponsiveContainer>
        )}
      </div>
    </Card>
  )
}

export function DatabaseDetailsPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const role = useAuthStore((s) => s.user?.role ?? '')
  const canManage = MANAGE_ROLES.includes(role)
  const canOperate = OPERATE_ROLES.includes(role)
  const [editOpen, setEditOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const actions = useDatabaseActions()

  const detailsQuery = useQuery({
    queryKey: ['databases', id],
    queryFn: () => getDatabase(id!),
    refetchInterval: 30_000,
    enabled: !!id,
  })

  useRealtimeInvalidation({ MetricsUpdated: ['databases'] })

  if (detailsQuery.isLoading) {
    return <Card className="animate-pulse"><div className="h-64 rounded bg-navy-700" /></Card>
  }

  if (detailsQuery.isError || !detailsQuery.data) {
    return <Card><p className="text-sm text-slate-500">Database not found. <Link to="/databases" className="text-info hover:underline">Back to databases</Link></p></Card>
  }

  const { connection, recentMetrics } = detailsQuery.data
  const latest = recentMetrics.at(-1)
  const util = utilization(latest)
  const reachable = recentMetrics.filter((m) => m.isReachable)
  const availability = recentMetrics.length ? Math.round((reachable.length * 1000) / recentMetrics.length) / 10 : null
  const latencies = reachable.map((m) => m.responseTimeMs ?? 0)
  const avgLatency = latencies.length ? Math.round(latencies.reduce((a, b) => a + b, 0) / latencies.length) : null
  const peakConnections = reachable.reduce((max, m) => Math.max(max, m.activeConnections ?? 0), 0)
  const firstSize = reachable.find((m) => m.databaseSizeMb != null)?.databaseSizeMb
  const sizeGrowth = firstSize != null && latest?.databaseSizeMb != null ? latest.databaseSizeMb - firstSize : null
  const errors = recentMetrics.filter((m) => !m.isReachable).reverse().slice(0, 10)

  const chartData = recentMetrics.map((m) => ({
    time: new Date(m.collectedAtUtc).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' }),
    active: m.isReachable ? m.activeConnections : null,
    max: m.isReachable ? m.maxConnections : null,
    size: m.isReachable ? m.databaseSizeMb : null,
    latency: m.isReachable ? m.responseTimeMs : null,
  }))

  return (
    <div>
      <Link to="/databases" className="inline-flex items-center gap-1.5 text-xs text-slate-500 hover:text-slate-300">
        <ArrowLeft className="h-3.5 w-3.5" /> All databases
      </Link>

      <div className="mt-3 flex flex-wrap items-start justify-between gap-3">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-xl font-semibold text-white">{connection.name}</h1>
            <StatusBadge status={connection.isActive ? healthToBadge[connection.health] ?? 'Inactive' : 'Inactive'} />
            {!connection.isActive && <span className="text-xs text-slate-500">monitoring paused</span>}
          </div>
          <p className="mt-1 text-xs text-slate-500">
            {connection.databaseType === 'SqlServer' ? 'SQL Server' : 'PostgreSQL'} · {connection.host}:{connection.port}/{connection.databaseName} · user {connection.username} ·
            last check {connection.lastCheckedAtUtc ? new Date(connection.lastCheckedAtUtc).toLocaleString() : 'pending'}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          {canOperate && (
            <Button variant="secondary" loading={actions.checkMutation.isPending} onClick={() => actions.checkMutation.mutate(connection)}>
              <Activity className="h-4 w-4" />Check now
            </Button>
          )}
          {canManage && (
            <>
              <Button variant="secondary" onClick={() => actions.toggleMutation.mutate(connection)} loading={actions.toggleMutation.isPending}>
                {connection.isActive ? <><Pause className="h-4 w-4" />Pause</> : <><Play className="h-4 w-4" />Resume</>}
              </Button>
              <Button variant="secondary" onClick={() => setEditOpen(true)}><Pencil className="h-4 w-4" />Edit</Button>
              <Button variant="danger" onClick={() => setDeleteOpen(true)}><Trash2 className="h-4 w-4" />Remove</Button>
            </>
          )}
        </div>
      </div>

      <Card className="mt-6">
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
          <Stat label="Connections" value={latest?.activeConnections != null ? `${latest.activeConnections}${latest.maxConnections ? ` / ${latest.maxConnections}` : ''}` : '—'} />
          <Stat label="Utilization" value={util !== null ? `${util}%` : '—'} tone={util !== null && util >= 80 ? 'text-warning' : undefined} />
          <Stat label="Peak connections" value={reachable.length ? String(peakConnections) : '—'} />
          <Stat label="Size" value={formatSize(latest?.databaseSizeMb)} />
          <Stat label="Avg latency" value={avgLatency !== null ? `${avgLatency} ms` : '—'} tone={avgLatency !== null && avgLatency > 2000 ? 'text-warning' : undefined} />
          <Stat label="Availability" value={availability !== null ? `${availability}%` : '—'} tone={availability !== null && availability < 99 ? 'text-warning' : 'text-healthy'} />
        </div>
        <p className="mt-3 text-[11px] text-slate-600">
          Based on the last {recentMetrics.length} probes
          {sizeGrowth !== null && sizeGrowth !== 0 && <> · size {sizeGrowth > 0 ? 'grew' : 'shrank'} by {formatSize(Math.abs(sizeGrowth))}</>}
          {latest?.maxConnections === 0 && <> · max connections is 0 (unlimited) — utilization can't be computed</>}
        </p>
      </Card>

      <div className="mt-3 grid grid-cols-1 gap-3 xl:grid-cols-2">
        <ChartCard title="Connections" subtitle="active vs. max" empty={reachable.length === 0}>
          <LineChart data={chartData} margin={{ top: 5, right: 10, left: -20, bottom: 0 }}>
            <CartesianGrid stroke="#1f2a47" strokeDasharray="3 3" />
            <XAxis dataKey="time" stroke="#64748b" fontSize={10} tickLine={false} minTickGap={40} />
            <YAxis stroke="#64748b" fontSize={11} tickLine={false} allowDecimals={false} />
            <Tooltip {...tooltipStyle} />
            <Line type="monotone" dataKey="active" name="Active" stroke="#3b82f6" strokeWidth={2} dot={false} connectNulls />
            <Line type="stepAfter" dataKey="max" name="Max" stroke="#f59e0b" strokeWidth={1.5} strokeDasharray="4 4" dot={false} connectNulls />
          </LineChart>
        </ChartCard>

        <ChartCard title="Probe latency" subtitle="Degraded above 2000 ms" empty={reachable.length === 0}>
          <LineChart data={chartData} margin={{ top: 5, right: 10, left: -20, bottom: 0 }}>
            <CartesianGrid stroke="#1f2a47" strokeDasharray="3 3" />
            <XAxis dataKey="time" stroke="#64748b" fontSize={10} tickLine={false} minTickGap={40} />
            <YAxis stroke="#64748b" fontSize={11} tickLine={false} unit=" ms" />
            <Tooltip {...tooltipStyle} />
            <Line type="monotone" dataKey="latency" name="Latency (ms)" stroke="#22c55e" strokeWidth={2} dot={false} connectNulls />
          </LineChart>
        </ChartCard>

        <ChartCard title="Database size" subtitle="MB" empty={reachable.length === 0}>
          <AreaChart data={chartData} margin={{ top: 5, right: 10, left: -10, bottom: 0 }}>
            <CartesianGrid stroke="#1f2a47" strokeDasharray="3 3" />
            <XAxis dataKey="time" stroke="#64748b" fontSize={10} tickLine={false} minTickGap={40} />
            <YAxis stroke="#64748b" fontSize={11} tickLine={false} domain={['auto', 'auto']} />
            <Tooltip {...tooltipStyle} />
            <Area type="monotone" dataKey="size" name="Size (MB)" stroke="#a855f7" fill="#a855f7" fillOpacity={0.15} strokeWidth={2} connectNulls />
          </AreaChart>
        </ChartCard>

        <Card>
          <h2 className="text-sm font-semibold text-white">Recent failures</h2>
          {errors.length === 0 ? (
            <p className="flex h-56 items-center justify-center text-sm text-slate-600">No failed probes in this window. 🎉</p>
          ) : (
            <ul className="mt-3 max-h-56 space-y-2 overflow-y-auto">
              {errors.map((m) => (
                <li key={m.collectedAtUtc} className="rounded-md bg-critical/10 px-3 py-2 text-xs">
                  <p className="text-[11px] text-slate-500">{new Date(m.collectedAtUtc).toLocaleString()}</p>
                  <p className="mt-0.5 break-words text-critical">{m.errorMessage ?? 'Unreachable'}</p>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      <DatabaseFormModal open={editOpen} existing={connection} onClose={() => setEditOpen(false)} />

      <Modal open={deleteOpen} title="Remove database connection" onClose={() => setDeleteOpen(false)}>
        <p className="text-sm text-slate-400">
          Remove <span className="text-white">{connection.name}</span> and its metric history? Stored credentials are deleted with it.
        </p>
        <div className="mt-5 flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setDeleteOpen(false)}>Cancel</Button>
          <Button
            variant="danger"
            loading={actions.deleteMutation.isPending}
            onClick={() => actions.deleteMutation.mutate(connection.id, { onSuccess: () => navigate('/databases') })}
          >
            Remove
          </Button>
        </div>
      </Modal>
    </div>
  )
}
