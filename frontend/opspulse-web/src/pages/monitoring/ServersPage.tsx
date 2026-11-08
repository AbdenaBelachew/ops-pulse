import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Search, Trash2 } from 'lucide-react'
import { deleteServer, listServers } from '../../api/monitoring'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { useRealtimeInvalidation } from '../../realtime/useRealtimeInvalidation'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Modal } from '../../components/ui/Modal'
import { StatusBadge } from '../../components/ui/StatusBadge'

function usageBar(percent: number | null): string {
  if (percent === null) return 'text-slate-600'
  if (percent >= 85) return 'text-critical'
  if (percent >= 65) return 'text-warning'
  return 'text-healthy'
}

export function ServersPage() {
  const canManage = useAuthStore((s) => ['SuperAdmin', 'OrganizationAdmin', 'DevOpsEngineer'].includes(s.user?.role ?? ''))
  const queryClient = useQueryClient()
  const [term, setTerm] = useState('')
  const [search, setSearch] = useState('')
  const [deleting, setDeleting] = useState<string | null>(null)

  useRealtimeInvalidation({ MetricsUpdated: ['servers'], AgentConnected: ['servers'], AgentDisconnected: ['servers'] })

  useEffect(() => {
    const timer = window.setTimeout(() => setSearch(term), 300)
    return () => window.clearTimeout(timer)
  }, [term])

  const serversQuery = useQuery({
    queryKey: ['servers', search],
    queryFn: () => listServers({ page: 1, pageSize: 50, search }),
    refetchInterval: 30_000,
    placeholderData: (p) => p,
  })

  const deleteMutation = useMutation({
    mutationFn: deleteServer,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['servers'] })
      toast.success('Server removed')
      setDeleting(null)
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not remove the server.')),
  })

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Servers</h1>
          <p className="mt-1 text-sm text-slate-500">Machines reporting through OpsPulse monitoring agents.</p>
        </div>
      </div>

      <div className="relative mt-6 max-w-md">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-600" />
        <input
          value={term}
          onChange={(e) => setTerm(e.target.value)}
          placeholder="Search by name or hostname…"
          aria-label="Search servers"
          className="w-full rounded-lg border border-navy-600 bg-navy-900 py-2 pl-9 pr-3 text-sm text-slate-200 placeholder:text-slate-600 focus:border-info/60 focus:outline-none"
        />
      </div>

      <Card className="mt-4 overflow-hidden p-0">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-navy-700 text-xs text-slate-500">
                <th scope="col" className="px-5 py-3 font-medium">Server</th>
                <th scope="col" className="px-5 py-3 font-medium">Status</th>
                <th scope="col" className="px-5 py-3 font-medium">CPU</th>
                <th scope="col" className="px-5 py-3 font-medium">Memory</th>
                <th scope="col" className="px-5 py-3 font-medium">Last heartbeat</th>
                {canManage && <th scope="col" className="px-5 py-3"><span className="sr-only">Actions</span></th>}
              </tr>
            </thead>
            <tbody>
              {serversQuery.isLoading ? (
                <tr><td colSpan={6} className="px-5 py-10 text-center text-slate-600">Loading servers…</td></tr>
              ) : (serversQuery.data?.items.length ?? 0) === 0 ? (
                <tr>
                  <td colSpan={6} className="px-5 py-10 text-center text-sm text-slate-500">
                    No servers yet. Create an API key on the Monitoring Agents page and deploy the agent.
                  </td>
                </tr>
              ) : (
                serversQuery.data?.items.map((server) => (
                  <tr key={server.id} className="border-b border-navy-800 last:border-0 hover:bg-navy-800/40">
                    <td className="px-5 py-3.5">
                      <Link to={`/servers/${server.id}`} className="font-medium text-slate-200 hover:text-info">{server.name}</Link>
                      <p className="text-xs text-slate-500">{server.hostname} · {server.osName} · {server.cpuCores} cores · {(server.totalMemoryMb / 1024).toFixed(0)} GB</p>
                    </td>
                    <td className="px-5 py-3.5"><StatusBadge status={server.status === 'Online' ? 'Active' : 'Inactive'} /></td>
                    <td className={`px-5 py-3.5 text-xs font-medium ${usageBar(server.lastCpuUsagePercent ?? null)}`}>
                      {server.lastCpuUsagePercent !== null ? `${server.lastCpuUsagePercent.toFixed(1)}%` : '—'}
                    </td>
                    <td className={`px-5 py-3.5 text-xs font-medium ${usageBar(server.lastMemoryUsagePercent ?? null)}`}>
                      {server.lastMemoryUsagePercent !== null ? `${server.lastMemoryUsagePercent.toFixed(1)}%` : '—'}
                    </td>
                    <td className="px-5 py-3.5 text-xs text-slate-500">
                      {server.lastHeartbeatAtUtc ? new Date(server.lastHeartbeatAtUtc).toLocaleTimeString() : '—'}
                    </td>
                    {canManage && (
                      <td className="px-5 py-3.5 text-right">
                        <button
                          onClick={() => setDeleting(server.id)}
                          aria-label={`Remove ${server.name}`}
                          className="rounded-lg p-2 text-slate-500 hover:bg-critical/10 hover:text-critical"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </td>
                    )}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </Card>

      <Modal open={deleting !== null} title="Remove server" onClose={() => setDeleting(null)}>
        <p className="text-sm text-slate-400">Remove this server and its metric history? The agent will re-register on its next heartbeat.</p>
        <div className="mt-5 flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setDeleting(null)}>Cancel</Button>
          <Button variant="danger" loading={deleteMutation.isPending} onClick={() => deleting && deleteMutation.mutate(deleting)}>Remove</Button>
        </div>
      </Modal>
    </div>
  )
}
