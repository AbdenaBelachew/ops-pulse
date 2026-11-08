import { Link, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft } from 'lucide-react'
import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getService } from '../../api/monitoring'
import { Card } from '../../components/ui/Card'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useRealtimeInvalidation } from '../../realtime/useRealtimeInvalidation'

function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
}

export function ApplicationDetailsPage() {
  const { id } = useParams<{ id: string }>()
  const serviceQuery = useQuery({
    queryKey: ['services', id],
    queryFn: () => getService(id!),
    refetchInterval: 15_000,
    enabled: !!id,
  })

  useRealtimeInvalidation({ ServiceStatusChanged: ['services'] })

  if (serviceQuery.isLoading) {
    return <Card className="animate-pulse"><div className="h-64 rounded bg-navy-700" /></Card>
  }

  if (serviceQuery.isError || !serviceQuery.data) {
    return <Card><p className="text-sm text-slate-500">Service not found. <Link to="/applications" className="text-info hover:underline">Back to applications</Link></p></Card>
  }

  const service = serviceQuery.data
  const chartData = [...service.recentChecks].reverse().map((check) => ({
    time: formatTime(check.checkedAtUtc),
    ms: check.responseTimeMs,
    success: check.isSuccess,
  }))

  return (
    <div>
      <Link to="/applications" className="inline-flex items-center gap-1.5 text-xs text-slate-500 hover:text-slate-300">
        <ArrowLeft className="h-3.5 w-3.5" /> All applications
      </Link>

      <div className="mt-3 flex flex-wrap items-center justify-between gap-3">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-xl font-semibold text-white">{service.name}</h1>
            <StatusBadge status={service.currentStatus} />
          </div>
          <p className="mt-1 break-all text-xs text-slate-500">{service.url}</p>
        </div>
        <div className="flex gap-6 text-xs text-slate-500">
          <div><p className="text-[10px] uppercase tracking-wide text-slate-600">24h uptime</p><p className="mt-0.5 text-sm font-semibold text-white">{service.uptimePercent24h !== null ? `${service.uptimePercent24h.toFixed(2)}%` : '—'}</p></div>
          <div><p className="text-[10px] uppercase tracking-wide text-slate-600">7d uptime</p><p className="mt-0.5 text-sm font-semibold text-white">{service.uptimePercent7d !== null ? `${service.uptimePercent7d.toFixed(2)}%` : '—'}</p></div>
          <div><p className="text-[10px] uppercase tracking-wide text-slate-600">Avg response</p><p className="mt-0.5 text-sm font-semibold text-white">{service.avgResponseMs24h !== null ? `${service.avgResponseMs24h.toFixed(0)} ms` : '—'}</p></div>
          <div><p className="text-[10px] uppercase tracking-wide text-slate-600">Interval</p><p className="mt-0.5 text-sm font-semibold text-white">{service.checkIntervalSeconds}s</p></div>
        </div>
      </div>

      <Card className="mt-6">
        <h2 className="text-sm font-semibold text-white">Response time (last {chartData.length} checks)</h2>
        <div className="mt-4 h-64">
          {chartData.length === 0 ? (
            <p className="flex h-full items-center justify-center text-sm text-slate-600">No checks recorded yet.</p>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={chartData} margin={{ top: 5, right: 10, left: -20, bottom: 0 }}>
                <CartesianGrid stroke="#1f2a47" strokeDasharray="3 3" />
                <XAxis dataKey="time" stroke="#64748b" fontSize={11} tickLine={false} />
                <YAxis stroke="#64748b" fontSize={11} tickLine={false} unit=" ms" />
                <Tooltip
                  contentStyle={{ background: '#121a30', border: '1px solid #2a3860', borderRadius: 8, fontSize: 12 }}
                  labelStyle={{ color: '#94a3b8' }}
                />
                <Line type="monotone" dataKey="ms" stroke="#3b82f6" strokeWidth={2} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          )}
        </div>
      </Card>

      <Card className="mt-4 overflow-hidden p-0">
        <div className="border-b border-navy-700 px-5 py-3"><h2 className="text-sm font-semibold text-white">Check history</h2></div>
        <div className="max-h-96 overflow-y-auto">
          <table className="w-full text-left text-sm">
            <thead className="sticky top-0 bg-navy-850">
              <tr className="text-xs text-slate-500">
                <th scope="col" className="px-5 py-2.5 font-medium">Time</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Result</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Status code</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Response</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Error</th>
              </tr>
            </thead>
            <tbody>
              {service.recentChecks.map((check, index) => (
                <tr key={index} className="border-t border-navy-800">
                  <td className="px-5 py-2 text-xs text-slate-500">{new Date(check.checkedAtUtc).toLocaleString()}</td>
                  <td className="px-5 py-2"><StatusBadge status={check.isSuccess ? 'Healthy' : 'Unhealthy'} /></td>
                  <td className="px-5 py-2 text-xs text-slate-400">{check.statusCode ?? '—'}</td>
                  <td className="px-5 py-2 text-xs text-slate-300">{check.responseTimeMs} ms</td>
                  <td className="max-w-xs truncate px-5 py-2 text-xs text-critical/80">{check.errorMessage ?? ''}</td>
                </tr>
              ))}
              {service.recentChecks.length === 0 && (
                <tr><td colSpan={5} className="px-5 py-8 text-center text-sm text-slate-600">No checks recorded yet — the worker checks every {service.checkIntervalSeconds}s.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  )
}
