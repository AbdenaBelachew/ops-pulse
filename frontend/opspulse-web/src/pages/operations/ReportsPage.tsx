import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Download } from 'lucide-react'
import { getIncidentSummary, getServerUtilization, getUptimeReport } from '../../api/operations'
import { useAuthStore } from '../../stores/authStore'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { StatusBadge } from '../../components/ui/StatusBadge'

const ranges = [
  { label: '24 hours', hours: 24 },
  { label: '7 days', hours: 168 },
  { label: '30 days', hours: 720 },
]

export function ReportsPage() {
  const [hours, setHours] = useState(24)
  const canExport = useAuthStore((s) => s.user !== null)

  const uptimeQuery = useQuery({ queryKey: ['uptime-report', hours], queryFn: () => getUptimeReport(hours) })
  const incidentsQuery = useQuery({ queryKey: ['incident-summary', hours], queryFn: () => getIncidentSummary(hours) })
  const serversQuery = useQuery({ queryKey: ['server-utilization', hours], queryFn: () => getServerUtilization(hours) })

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Reports & Analytics</h1>
          <p className="mt-1 text-sm text-slate-500">Uptime, incident and utilization summaries with CSV export.</p>
        </div>
        <div className="flex items-center gap-2">
          {ranges.map((range) => (
            <button
              key={range.hours}
              onClick={() => setHours(range.hours)}
              className={`rounded-lg border px-3 py-1.5 text-xs font-medium transition-colors ${
                hours === range.hours ? 'border-info/50 bg-info/10 text-info' : 'border-navy-600 bg-navy-800 text-slate-400 hover:text-slate-200'
              }`}
            >
              {range.label}
            </button>
          ))}
          {canExport && (
            <a href={`/api/v1/reports/uptime/export?hours=${hours}`} className="ml-2 inline-flex">
              <Button variant="secondary"><Download className="h-4 w-4" />CSV</Button>
            </a>
          )}
        </div>
      </div>

      <div className="mt-6 grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Card>
          <p className="text-xs text-slate-500">Open incidents</p>
          <p className="mt-1 text-2xl font-semibold text-white">{incidentsQuery.data?.open ?? '—'}</p>
        </Card>
        <Card>
          <p className="text-xs text-slate-500">Acknowledged</p>
          <p className="mt-1 text-2xl font-semibold text-white">{incidentsQuery.data?.acknowledged ?? '—'}</p>
        </Card>
        <Card>
          <p className="text-xs text-slate-500">Resolved</p>
          <p className="mt-1 text-2xl font-semibold text-white">{incidentsQuery.data?.resolved ?? '—'}</p>
        </Card>
        <Card>
          <p className="text-xs text-slate-500">Avg resolution</p>
          <p className="mt-1 text-2xl font-semibold text-white">
            {incidentsQuery.data?.avgResolutionMinutes !== null && incidentsQuery.data?.avgResolutionMinutes !== undefined
              ? `${incidentsQuery.data.avgResolutionMinutes} min`
              : '—'}
          </p>
        </Card>
      </div>

      <Card className="mt-4 overflow-hidden p-0">
        <div className="border-b border-navy-700 px-5 py-3"><h2 className="text-sm font-semibold text-white">Service uptime</h2></div>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="text-xs text-slate-500">
                <th scope="col" className="px-5 py-2.5 font-medium">Service</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Environment</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Status</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Uptime</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Avg response</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Checks</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Failed</th>
              </tr>
            </thead>
            <tbody>
              {uptimeQuery.data?.map((row) => (
                <tr key={row.serviceId} className="border-t border-navy-800">
                  <td className="px-5 py-2.5 font-medium text-slate-200">{row.serviceName}</td>
                  <td className="px-5 py-2.5 text-xs text-slate-500">{row.environment}</td>
                  <td className="px-5 py-2.5"><StatusBadge status={row.currentStatus === 'Up' ? 'Healthy' : row.currentStatus === 'Degraded' ? 'Degraded' : row.currentStatus === 'Down' ? 'Unhealthy' : 'Inactive'} /></td>
                  <td className={`px-5 py-2.5 text-xs font-medium ${row.uptimePercent === null ? 'text-slate-600' : row.uptimePercent >= 99 ? 'text-healthy' : row.uptimePercent >= 95 ? 'text-warning' : 'text-critical'}`}>
                    {row.uptimePercent !== null ? `${row.uptimePercent.toFixed(2)}%` : '—'}
                  </td>
                  <td className="px-5 py-2.5 text-xs text-slate-300">{row.avgResponseMs !== null ? `${row.avgResponseMs.toFixed(0)} ms` : '—'}</td>
                  <td className="px-5 py-2.5 text-xs text-slate-500">{row.checkCount}</td>
                  <td className={`px-5 py-2.5 text-xs ${row.downCount > 0 ? 'text-critical' : 'text-slate-500'}`}>{row.downCount}</td>
                </tr>
              ))}
              {(uptimeQuery.data?.length ?? 0) === 0 && (
                <tr><td colSpan={7} className="px-5 py-8 text-center text-sm text-slate-600">No services monitored yet.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>

      <Card className="mt-4 overflow-hidden p-0">
        <div className="border-b border-navy-700 px-5 py-3"><h2 className="text-sm font-semibold text-white">Server utilization</h2></div>
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="text-xs text-slate-500">
              <th scope="col" className="px-5 py-2.5 font-medium">Server</th>
              <th scope="col" className="px-5 py-2.5 font-medium">Status</th>
              <th scope="col" className="px-5 py-2.5 font-medium">Avg CPU</th>
              <th scope="col" className="px-5 py-2.5 font-medium">Peak CPU</th>
              <th scope="col" className="px-5 py-2.5 font-medium">Avg memory</th>
              <th scope="col" className="px-5 py-2.5 font-medium">Peak memory</th>
            </tr>
          </thead>
          <tbody>
            {serversQuery.data?.map((row) => (
              <tr key={row.serverId} className="border-t border-navy-800">
                <td className="px-5 py-2.5 font-medium text-slate-200">{row.serverName}</td>
                <td className="px-5 py-2.5"><StatusBadge status={row.status === 'Online' ? 'Active' : 'Inactive'} /></td>
                <td className="px-5 py-2.5 text-xs text-slate-300">{row.avgCpuPercent !== null ? `${row.avgCpuPercent}%` : '—'}</td>
                <td className="px-5 py-2.5 text-xs text-slate-300">{row.peakCpuPercent !== null ? `${row.peakCpuPercent}%` : '—'}</td>
                <td className="px-5 py-2.5 text-xs text-slate-300">{row.avgMemoryPercent !== null ? `${row.avgMemoryPercent}%` : '—'}</td>
                <td className="px-5 py-2.5 text-xs text-slate-300">{row.peakMemoryPercent !== null ? `${row.peakMemoryPercent}%` : '—'}</td>
              </tr>
            ))}
            {(serversQuery.data?.length ?? 0) === 0 && (
              <tr><td colSpan={6} className="px-5 py-8 text-center text-sm text-slate-600">No server metrics in this window.</td></tr>
            )}
          </tbody>
        </table>
      </Card>
    </div>
  )
}
