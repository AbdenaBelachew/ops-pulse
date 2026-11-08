import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Boxes, Container, RefreshCw, Trash2 } from 'lucide-react'
import {
  createDockerHost,
  deleteDockerHost,
  getDockerHost,
  listDockerHosts,
  syncDockerHost,
} from '../../api/monitoring'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Field } from '../../components/ui/Field'
import { Modal } from '../../components/ui/Modal'
import { StatusBadge } from '../../components/ui/StatusBadge'

export function DockerPage() {
  const canManage = useAuthStore((s) => ['SuperAdmin', 'OrganizationAdmin', 'DevOpsEngineer'].includes(s.user?.role ?? ''))
  const queryClient = useQueryClient()
  const [addOpen, setAddOpen] = useState(false)
  const [name, setName] = useState('Local Docker')
  const [endpoint, setEndpoint] = useState('npipe://./pipe/docker_engine')
  const [selectedHostId, setSelectedHostId] = useState<string | null>(null)
  const [deleting, setDeleting] = useState<string | null>(null)

  const hostsQuery = useQuery({ queryKey: ['docker-hosts'], queryFn: listDockerHosts, refetchInterval: 30_000 })

  const selectedHostQuery = useQuery({
    queryKey: ['docker-hosts', selectedHostId],
    queryFn: () => getDockerHost(selectedHostId!),
    enabled: !!selectedHostId,
    refetchInterval: 30_000,
  })

  const createMutation = useMutation({
    mutationFn: () => createDockerHost({ name, endpoint }),
    onSuccess: (host) => {
      queryClient.invalidateQueries({ queryKey: ['docker-hosts'] })
      toast.success(host.isReachable ? `Connected — ${host.containerCount} containers found` : 'Host saved but unreachable')
      setAddOpen(false)
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not add the Docker host.')),
  })

  const syncMutation = useMutation({
    mutationFn: syncDockerHost,
    onSuccess: (host) => {
      queryClient.invalidateQueries({ queryKey: ['docker-hosts'] })
      toast.success(host.isReachable ? `Synced — ${host.containerCount} containers` : `Unreachable: ${host.lastError?.slice(0, 80)}`)
    },
    onError: (e) => toast.error(extractApiError(e, 'Sync failed.')),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteDockerHost,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['docker-hosts'] })
      toast.success('Docker host removed')
      setDeleting(null)
      setSelectedHostId(null)
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not remove the host.')),
  })

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Docker</h1>
          <p className="mt-1 text-sm text-slate-500">Container status, CPU and memory across Docker hosts.</p>
        </div>
        {canManage && <Button onClick={() => setAddOpen(true)}><Container className="h-4 w-4" />Add Docker host</Button>}
      </div>

      <div className="mt-6 grid grid-cols-1 gap-3 lg:grid-cols-3">
        {hostsQuery.isLoading ? (
          <Card className="animate-pulse lg:col-span-3"><div className="h-16 rounded bg-navy-700" /></Card>
        ) : (hostsQuery.data?.length ?? 0) === 0 ? (
          <Card className="lg:col-span-3"><p className="py-8 text-center text-sm text-slate-500">No Docker hosts added yet.</p></Card>
        ) : (
          hostsQuery.data?.map((host) => (
            <Card
              key={host.id}
              className={`cursor-pointer transition-colors ${selectedHostId === host.id ? 'border-info/50' : 'hover:border-navy-500'}`}
            >
              <button onClick={() => setSelectedHostId(host.id)} className="w-full text-left">
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-semibold text-white">{host.name}</p>
                    <p className="mt-0.5 truncate text-[11px] text-slate-500">{host.endpoint}</p>
                  </div>
                  <StatusBadge status={host.isReachable ? 'Healthy' : 'Unhealthy'} />
                </div>
                <p className="mt-3 text-[11px] text-slate-500">
                  <span className="font-medium text-slate-300">{host.runningCount}</span> running / {host.containerCount} containers
                </p>
              </button>
              {canManage && (
                <div className="mt-3 flex justify-end gap-1">
                  <button onClick={() => syncMutation.mutate(host.id)} aria-label={`Sync ${host.name}`} className="rounded-lg p-1.5 text-slate-500 hover:bg-navy-700 hover:text-info">
                    <RefreshCw className="h-3.5 w-3.5" />
                  </button>
                  <button onClick={() => setDeleting(host.id)} aria-label={`Remove ${host.name}`} className="rounded-lg p-1.5 text-slate-500 hover:bg-critical/10 hover:text-critical">
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                </div>
              )}
            </Card>
          ))
        )}
      </div>

      {selectedHostId && (
        <Card className="mt-4 overflow-hidden p-0">
          <div className="border-b border-navy-700 px-5 py-3">
            <h2 className="text-sm font-semibold text-white">
              Containers — {selectedHostQuery.data?.host.name}
              {selectedHostQuery.data && (
                <span className="ml-2 text-xs font-normal text-slate-500">{selectedHostQuery.data.containers.length} total</span>
              )}
            </h2>
          </div>
          <div className="max-h-[480px] overflow-auto">
            <table className="w-full text-left text-sm">
              <thead className="sticky top-0 bg-navy-850">
                <tr className="text-xs text-slate-500">
                  <th scope="col" className="px-5 py-2.5 font-medium">Container</th>
                  <th scope="col" className="px-5 py-2.5 font-medium">Image</th>
                  <th scope="col" className="px-5 py-2.5 font-medium">State</th>
                  <th scope="col" className="px-5 py-2.5 font-medium">CPU</th>
                  <th scope="col" className="px-5 py-2.5 font-medium">Memory</th>
                </tr>
              </thead>
              <tbody>
                {selectedHostQuery.data?.containers.map((container) => (
                  <tr key={container.id} className="border-t border-navy-800 hover:bg-navy-800/40">
                    <td className="px-5 py-2.5">
                      <p className="font-medium text-slate-200">{container.name}</p>
                      <p className="text-[10px] text-slate-600">{container.containerId.slice(0, 12)}</p>
                    </td>
                    <td className="max-w-48 truncate px-5 py-2.5 text-xs text-slate-500">{container.image}</td>
                    <td className="px-5 py-2.5">
                      <StatusBadge status={container.state === 'running' ? 'Healthy' : 'Inactive'} />
                    </td>
                    <td className="px-5 py-2.5 text-xs text-slate-300">{container.cpuUsagePercent !== null ? `${container.cpuUsagePercent.toFixed(1)}%` : '—'}</td>
                    <td className="px-5 py-2.5 text-xs text-slate-300">
                      {container.memoryUsedMb !== null
                        ? `${container.memoryUsedMb} MB${container.memoryLimitMb ? ` / ${container.memoryLimitMb} MB` : ''}`
                        : '—'}
                    </td>
                  </tr>
                ))}
                {selectedHostQuery.data?.containers.length === 0 && (
                  <tr><td colSpan={5} className="px-5 py-8 text-center text-sm text-slate-600">No containers on this host.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      <Modal open={addOpen} title="Add Docker host" onClose={() => setAddOpen(false)}>
        <form
          onSubmit={(e) => {
            e.preventDefault()
            createMutation.mutate()
          }}
          className="space-y-4"
        >
          <Field id="docker-name" label="Name" value={name} onChange={(e) => setName(e.target.value)} required />
          <Field
            id="docker-endpoint"
            label="Engine endpoint"
            value={endpoint}
            onChange={(e) => setEndpoint(e.target.value)}
            hint="npipe://./pipe/docker_engine (Windows) · unix:///var/run/docker.sock (Linux) · tcp://host:2375"
            required
          />
          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" variant="secondary" onClick={() => setAddOpen(false)}>Cancel</Button>
            <Button type="submit" loading={createMutation.isPending}><Boxes className="h-4 w-4" />Connect</Button>
          </div>
        </form>
      </Modal>

      <Modal open={deleting !== null} title="Remove Docker host" onClose={() => setDeleting(null)}>
        <p className="text-sm text-slate-400">Remove this host and its container inventory?</p>
        <div className="mt-5 flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setDeleting(null)}>Cancel</Button>
          <Button variant="danger" loading={deleteMutation.isPending} onClick={() => deleting && deleteMutation.mutate(deleting)}>Remove</Button>
        </div>
      </Modal>
    </div>
  )
}
