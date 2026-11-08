import { Link, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft } from 'lucide-react'
import { CartesianGrid, Line, LineChart, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getServer } from '../../api/monitoring'
import { Card } from '../../components/ui/Card'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useRealtimeInvalidation } from '../../realtime/useRealtimeInvalidation'

export function ServerDetailsPage() {
  const { id } = useParams<{ id: string }>()
  const serverQuery = useQuery({
    queryKey: ['servers', id],
    queryFn: () => getServer(id!),
    refetchInterval: 15_000,
    enabled: !!id,
  })

  useRealtimeInvalidation({ MetricsUpdated: ['servers'] })

  if (serverQuery.isLoading) {
    return <Card className="animate-pulse"><div className="h-64 rounded bg-navy-700" /></Card>
  }

  if (serverQuery.isError || !serverQuery.data) {
    return <Card><p className="text-sm text-slate-500">Server not found. <Link to="/servers" className="text-info hover:underline">Back to servers</Link></p></Card>
  }

  const { server, recentMetrics } = serverQuery.data
  const chartData = recentMetrics.map((metric) => ({
    time: new Date(metric.collectedAtUtc).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit', second: '2-digit' }),
    cpu: Math.round(metric.cpuUsagePercent * 10) / 10,
    memory: metric.memoryTotalMb > 0 ? Math.round((metric.memoryUsedMb * 1000 / metric.memoryTotalMb) / 10) / 10 : 0,
  }))

  return (
    <div>
      <Link to="/servers" className="inline-flex items-center gap-1.5 text-xs text-slate-500 hover:text-slate-300">
        <ArrowLeft className="h-3.5 w-3.5" /> All servers
      </Link>

      <div className="mt-3 flex flex-wrap items-center justify-between gap-3">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-xl font-semibold text-white">{server.name}</h1>
            <StatusBadge status={server.status === 'Online' ? 'Active' : 'Inactive'} />
          </div>
          <p className="mt-1 text-xs text-slate-500">
            {server.hostname} · {server.ipAddress ?? 'unknown IP'} · {server.osName} {server.osVersion} · agent {server.agentVersion ?? '—'}
          </p>
        </div>
        <div className="flex gap-6 text-xs text-slate-500">
          <div><p className="text-[10px] uppercase tracking-wide text-slate-600">CPU</p><p className="mt-0.5 text-sm font-semibold text-white">{server.lastCpuUsagePercent !== null ? `${server.lastCpuUsagePercent.toFixed(1)}%` : '—'}</p></div>
          <div><p className="text-[10px] uppercase tracking-wide text-slate-600">Memory</p><p className="mt-0.5 text-sm font-semibold text-white">{server.lastMemoryUsagePercent !== null ? `${server.lastMemoryUsagePercent.toFixed(1)}%` : '—'}</p></div>
          <div><p className="text-[10px] uppercase tracking-wide text-slate-600">Cores</p><p className="mt-0.5 text-sm font-semibold text-white">{server.cpuCores}</p></div>
          <div><p className="text-[10px] uppercase tracking-wide text-slate-600">RAM</p><p className="mt-0.5 text-sm font-semibold text-white">{(server.totalMemoryMb / 1024).toFixed(1)} GB</p></div>
          <div><p className="text-[10px] uppercase tracking-wide text-slate-600">Last heartbeat</p><p className="mt-0.5 text-sm font-semibold text-white">{server.lastHeartbeatAtUtc ? new Date(server.lastHeartbeatAtUtc).toLocaleTimeString() : '—'}</p></div>
        </div>
      </div>

      <Card className="mt-6">
        <h2 className="text-sm font-semibold text-white">Resource usage (last {chartData.length} samples)</h2>
        <div className="mt-4 h-72">
          {chartData.length === 0 ? (
            <p className="flex h-full items-center justify-center text-sm text-slate-600">No metrics yet — the agent reports every interval.</p>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={chartData} margin={{ top: 5, right: 10, left: -20, bottom: 0 }}>
                <CartesianGrid stroke="#1f2a47" strokeDasharray="3 3" />
                <XAxis dataKey="time" stroke="#64748b" fontSize={10} tickLine={false} minTickGap={40} />
                <YAxis stroke="#64748b" fontSize={11} tickLine={false} domain={[0, 100]} unit="%" />
                <Tooltip
                  contentStyle={{ background: '#121a30', border: '1px solid #2a3860', borderRadius: 8, fontSize: 12 }}
                  labelStyle={{ color: '#94a3b8' }}
                />
                <Legend wrapperStyle={{ fontSize: 12 }} />
                <Line type="monotone" dataKey="cpu" name="CPU %" stroke="#3b82f6" strokeWidth={2} dot={false} />
                <Line type="monotone" dataKey="memory" name="Memory %" stroke="#22c55e" strokeWidth={2} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          )}
        </div>
      </Card>
    </div>
  )
}
