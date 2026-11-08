import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { RefreshCw, Ship, Trash2 } from 'lucide-react'
import { createCluster, deleteCluster, getCluster, listClusters, syncCluster } from '../../api/monitoring'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Field } from '../../components/ui/Field'
import { Modal } from '../../components/ui/Modal'
import { StatusBadge } from '../../components/ui/StatusBadge'

export function KubernetesPage() {
  const canManage = useAuthStore((s) => ['SuperAdmin', 'OrganizationAdmin', 'DevOpsEngineer'].includes(s.user?.role ?? ''))
  const queryClient = useQueryClient()
  const [addOpen, setAddOpen] = useState(false)
  const [name, setName] = useState('')
  const [kubeConfig, setKubeConfig] = useState('')
  const [selectedClusterId, setSelectedClusterId] = useState<string | null>(null)
  const [deleting, setDeleting] = useState<string | null>(null)

  const clustersQuery = useQuery({ queryKey: ['kubernetes-clusters'], queryFn: listClusters, refetchInterval: 60_000 })

  const selectedQuery = useQuery({
    queryKey: ['kubernetes-clusters', selectedClusterId],
    queryFn: () => getCluster(selectedClusterId!),
    enabled: !!selectedClusterId,
    refetchInterval: 60_000,
  })

  const createMutation = useMutation({
    mutationFn: () => createCluster({ name, kubeConfigYaml: kubeConfig || null }),
    onSuccess: (cluster) => {
      queryClient.invalidateQueries({ queryKey: ['kubernetes-clusters'] })
      toast.success(cluster.isReachable
        ? `Connected — ${cluster.nodeCount} nodes, ${cluster.podCount} pods`
        : 'Cluster saved but unreachable (check kubeconfig / RBAC)')
      setAddOpen(false)
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not add the cluster.')),
  })

  const syncMutation = useMutation({
    mutationFn: syncCluster,
    onSuccess: (cluster) => {
      queryClient.invalidateQueries({ queryKey: ['kubernetes-clusters'] })
      toast.success(cluster.isReachable ? `Synced — ${cluster.nodeCount} nodes, ${cluster.podCount} pods` : `Unreachable: ${cluster.lastError?.slice(0, 80)}`)
    },
    onError: (e) => toast.error(extractApiError(e, 'Sync failed.')),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteCluster,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['kubernetes-clusters'] })
      toast.success('Cluster removed')
      setDeleting(null)
      setSelectedClusterId(null)
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not remove the cluster.')),
  })

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Kubernetes</h1>
          <p className="mt-1 text-sm text-slate-500">Cluster health, nodes and pods via least-privilege API access.</p>
        </div>
        {canManage && <Button onClick={() => setAddOpen(true)}><Ship className="h-4 w-4" />Add cluster</Button>}
      </div>

      <div className="mt-6 grid grid-cols-1 gap-3 lg:grid-cols-3">
        {clustersQuery.isLoading ? (
          <Card className="animate-pulse lg:col-span-3"><div className="h-16 rounded bg-navy-700" /></Card>
        ) : (clustersQuery.data?.length ?? 0) === 0 ? (
          <Card className="lg:col-span-3">
            <p className="py-8 text-center text-sm text-slate-500">
              No clusters added. Add one with a stored kubeconfig, or connect using the machine's local kubeconfig.
            </p>
          </Card>
        ) : (
          clustersQuery.data?.map((cluster) => (
            <Card key={cluster.id} className={`cursor-pointer transition-colors ${selectedClusterId === cluster.id ? 'border-info/50' : 'hover:border-navy-500'}`}>
              <button onClick={() => setSelectedClusterId(cluster.id)} className="w-full text-left">
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-semibold text-white">{cluster.name}</p>
                    <p className="mt-0.5 truncate text-[11px] text-slate-500">{cluster.kubernetesVersion ?? 'version unknown'}</p>
                  </div>
                  <StatusBadge status={cluster.isReachable ? 'Healthy' : 'Unhealthy'} />
                </div>
                <div className="mt-3 flex gap-4 text-[11px] text-slate-500">
                  <span>Nodes <span className="font-medium text-slate-300">{cluster.nodeCount}</span></span>
                  <span>Pods <span className="font-medium text-slate-300">{cluster.podCount}</span></span>
                  <span className={cluster.failedPodCount > 0 ? 'text-critical' : ''}>
                    Failed <span className="font-medium">{cluster.failedPodCount}</span>
                  </span>
                </div>
              </button>
              {canManage && (
                <div className="mt-3 flex justify-end gap-1">
                  <button onClick={() => syncMutation.mutate(cluster.id)} aria-label={`Sync ${cluster.name}`} className="rounded-lg p-1.5 text-slate-500 hover:bg-navy-700 hover:text-info">
                    <RefreshCw className="h-3.5 w-3.5" />
                  </button>
                  <button onClick={() => setDeleting(cluster.id)} aria-label={`Remove ${cluster.name}`} className="rounded-lg p-1.5 text-slate-500 hover:bg-critical/10 hover:text-critical">
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                </div>
              )}
            </Card>
          ))
        )}
      </div>

      {selectedClusterId && selectedQuery.data && (
        <>
          <Card className="mt-4 overflow-hidden p-0">
            <div className="border-b border-navy-700 px-5 py-3"><h2 className="text-sm font-semibold text-white">Nodes ({selectedQuery.data.nodes.length})</h2></div>
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="text-xs text-slate-500">
                  <th scope="col" className="px-5 py-2.5 font-medium">Node</th>
                  <th scope="col" className="px-5 py-2.5 font-medium">Status</th>
                  <th scope="col" className="px-5 py-2.5 font-medium">Roles</th>
                  <th scope="col" className="px-5 py-2.5 font-medium">CPU</th>
                  <th scope="col" className="px-5 py-2.5 font-medium">Memory</th>
                </tr>
              </thead>
              <tbody>
                {selectedQuery.data.nodes.map((node) => (
                  <tr key={node.id} className="border-t border-navy-800">
                    <td className="px-5 py-2.5 font-medium text-slate-200">{node.name}</td>
                    <td className="px-5 py-2.5"><StatusBadge status={node.status === 'Ready' ? 'Healthy' : 'Unhealthy'} /></td>
                    <td className="px-5 py-2.5 text-xs text-slate-500">{node.roles.join(', ') || 'worker'}</td>
                    <td className="px-5 py-2.5 text-xs text-slate-300">{node.cpuCapacityCores ?? '—'} cores</td>
                    <td className="px-5 py-2.5 text-xs text-slate-300">{node.memoryCapacityMb ? `${(node.memoryCapacityMb / 1024).toFixed(1)} GB` : '—'}</td>
                  </tr>
                ))}
                {selectedQuery.data.nodes.length === 0 && (
                  <tr><td colSpan={5} className="px-5 py-8 text-center text-sm text-slate-600">No nodes synced{selectedQuery.data.cluster.isReachable ? ' yet' : ' — cluster unreachable'}.</td></tr>
                )}
              </tbody>
            </table>
          </Card>

          <Card className="mt-4 overflow-hidden p-0">
            <div className="border-b border-navy-700 px-5 py-3"><h2 className="text-sm font-semibold text-white">Pods ({selectedQuery.data.pods.length})</h2></div>
            <div className="max-h-96 overflow-auto">
              <table className="w-full text-left text-sm">
                <thead className="sticky top-0 bg-navy-850">
                  <tr className="text-xs text-slate-500">
                    <th scope="col" className="px-5 py-2.5 font-medium">Pod</th>
                    <th scope="col" className="px-5 py-2.5 font-medium">Namespace</th>
                    <th scope="col" className="px-5 py-2.5 font-medium">Status</th>
                    <th scope="col" className="px-5 py-2.5 font-medium">Restarts</th>
                    <th scope="col" className="px-5 py-2.5 font-medium">Node</th>
                  </tr>
                </thead>
                <tbody>
                  {selectedQuery.data.pods.slice(0, 100).map((pod) => (
                    <tr key={pod.id} className="border-t border-navy-800 hover:bg-navy-800/40">
                      <td className="px-5 py-2 text-xs font-medium text-slate-200">{pod.name}</td>
                      <td className="px-5 py-2 text-xs text-slate-500">{pod.namespace}</td>
                      <td className="px-5 py-2"><StatusBadge status={pod.isFailed ? 'Unhealthy' : pod.status === 'Running' ? 'Healthy' : 'Inactive'} /></td>
                      <td className={`px-5 py-2 text-xs ${pod.restartCount > 5 ? 'font-medium text-warning' : 'text-slate-300'}`}>{pod.restartCount}</td>
                      <td className="px-5 py-2 text-xs text-slate-500">{pod.nodeName ?? '—'}</td>
                    </tr>
                  ))}
                  {selectedQuery.data.pods.length === 0 && (
                    <tr><td colSpan={5} className="px-5 py-8 text-center text-sm text-slate-600">No pods synced.</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </Card>
        </>
      )}

      <Modal open={addOpen} title="Add Kubernetes cluster" onClose={() => setAddOpen(false)}>
        <form
          onSubmit={(e: FormEvent) => {
            e.preventDefault()
            createMutation.mutate()
          }}
          className="space-y-4"
        >
          <Field id="k8s-name" label="Cluster name" value={name} onChange={(e) => setName(e.target.value)} required />
          <div>
            <label htmlFor="k8s-config" className="mb-1.5 block text-xs font-medium text-slate-400">Kubeconfig YAML (optional)</label>
            <textarea
              id="k8s-config"
              value={kubeConfig}
              onChange={(e) => setKubeConfig(e.target.value)}
              rows={5}
              placeholder="Leave empty to use this machine's default kubeconfig (~/.kube/config). Stored encrypted."
              className="w-full resize-none rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 font-mono text-xs text-slate-200 placeholder:text-slate-600 focus:border-info/60 focus:outline-none"
            />
          </div>
          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" variant="secondary" onClick={() => setAddOpen(false)}>Cancel</Button>
            <Button type="submit" loading={createMutation.isPending}>Connect</Button>
          </div>
        </form>
      </Modal>

      <Modal open={deleting !== null} title="Remove cluster" onClose={() => setDeleting(null)}>
        <p className="text-sm text-slate-400">Remove this cluster and its synced nodes/pods?</p>
        <div className="mt-5 flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setDeleting(null)}>Cancel</Button>
          <Button variant="danger" loading={deleteMutation.isPending} onClick={() => deleting && deleteMutation.mutate(deleting)}>Remove</Button>
        </div>
      </Modal>
    </div>
  )
}
