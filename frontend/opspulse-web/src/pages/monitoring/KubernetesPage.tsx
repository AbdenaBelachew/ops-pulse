import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { FileText, Pencil, RefreshCw, RotateCcw, Ship, Trash2 } from 'lucide-react'
import { createCluster, deleteCluster, listClusters, syncCluster } from '../../api/monitoring'
import {
  getClusterDetails,
  getPodLogs,
  restartPod,
  scaleDeployment,
} from '../../api/kubernetes'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Field } from '../../components/ui/Field'
import { Modal } from '../../components/ui/Modal'
import { StatusBadge } from '../../components/ui/StatusBadge'

function PodLogsModal({
  clusterId,
  target,
  onClose,
}: {
  clusterId: string
  target: { namespace: string; pod: string } | null
  onClose: () => void
}) {
  const [lines, setLines] = useState(200)
  const [previous, setPrevious] = useState(false)

  const logsQuery = useQuery({
    queryKey: ['pod-logs', clusterId, target?.namespace, target?.pod, lines, previous],
    queryFn: () => getPodLogs(clusterId, target!.namespace, target!.pod, lines, previous),
    enabled: !!target,
    refetchInterval: 5000,
  })

  return (
    <Modal
      open={target !== null}
      title={target ? `Pod logs — ${target.namespace}/${target.pod}` : 'Pod logs'}
      onClose={onClose}
    >
      {target && (
        <div>
          <div className="mb-3 flex flex-wrap items-center gap-2 text-xs">
            <select
              value={lines}
              onChange={(e) => setLines(Number(e.target.value))}
              aria-label="Number of lines"
              className="rounded-lg border border-navy-600 bg-navy-900 px-2 py-1.5 text-slate-300 focus:outline-none"
            >
              {[100, 200, 500, 1000].map((option) => (
                <option key={option} value={option}>{option} lines</option>
              ))}
            </select>
            <label className="flex cursor-pointer items-center gap-1.5 text-slate-400">
              <input type="checkbox" checked={previous} onChange={(e) => setPrevious(e.target.checked)} className="h-3.5 w-3.5 accent-blue-500" />
              Previous container
            </label>
            <span className="ml-auto inline-flex items-center gap-1.5 text-slate-600">
              <span className="h-1.5 w-1.5 rounded-full bg-healthy" /> auto-refresh 5s
            </span>
          </div>
          <pre className="max-h-[55vh] overflow-auto rounded-lg border border-navy-600 bg-navy-950 p-3 text-[11px] leading-relaxed text-slate-300">
            {logsQuery.isLoading ? 'Loading logs…' : logsQuery.data?.logs?.trim() || '(no log output)'}
          </pre>
        </div>
      )}
    </Modal>
  )
}

function ScaleModal({
  clusterId,
  target,
  onClose,
}: {
  clusterId: string
  target: { namespace: string; deployment: string; current: number } | null
  onClose: () => void
}) {
  const queryClient = useQueryClient()
  const [replicas, setReplicas] = useState(1)

  const scaleMutation = useMutation({
    mutationFn: () => scaleDeployment(clusterId, target!.namespace, target!.deployment, replicas),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['kubernetes-clusters'] })
      toast.success(`${target?.deployment} scaled to ${result.replicasDesired} replicas`)
      onClose()
    },
    onError: (e) => toast.error(extractApiError(e, 'Scale operation failed.')),
  })

  return (
    <Modal open={target !== null} title={`Scale ${target?.deployment ?? ''}`} onClose={onClose}>
      {target && (
        <form
          onSubmit={(e: FormEvent) => {
            e.preventDefault()
            scaleMutation.mutate()
          }}
        >
          <p className="text-xs text-slate-500">
            Namespace <span className="text-slate-300">{target.namespace}</span> · current replicas{' '}
            <span className="text-slate-300">{target.current}</span>. Scaling to 0 suspends the workload.
          </p>
          <Field
            id="scale-replicas"
            label="Replicas"
            type="number"
            min={0}
            max={100}
            value={replicas}
            onChange={(e) => setReplicas(Number(e.target.value))}
            className="mt-4"
            required
          />
          <div className="mt-5 flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={onClose}>Cancel</Button>
            <Button type="submit" loading={scaleMutation.isPending}>Apply scale</Button>
          </div>
        </form>
      )}
    </Modal>
  )
}

export function KubernetesPage() {
  const canManage = useAuthStore((s) => ['SuperAdmin', 'OrganizationAdmin', 'DevOpsEngineer'].includes(s.user?.role ?? ''))
  const queryClient = useQueryClient()
  const [addOpen, setAddOpen] = useState(false)
  const [name, setName] = useState('')
  const [kubeConfig, setKubeConfig] = useState('')
  const [selectedClusterId, setSelectedClusterId] = useState<string | null>(null)
  const [deleting, setDeleting] = useState<string | null>(null)
  const [logsTarget, setLogsTarget] = useState<{ namespace: string; pod: string } | null>(null)
  const [restartTarget, setRestartTarget] = useState<{ namespace: string; pod: string } | null>(null)
  const [scaleTarget, setScaleTarget] = useState<{ namespace: string; deployment: string; current: number } | null>(null)

  const clustersQuery = useQuery({ queryKey: ['kubernetes-clusters'], queryFn: listClusters, refetchInterval: 60_000 })

  const selectedQuery = useQuery({
    queryKey: ['kubernetes-clusters', selectedClusterId],
    queryFn: () => getClusterDetails(selectedClusterId!),
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

  const restartMutation = useMutation({
    mutationFn: () => restartPod(selectedClusterId!, restartTarget!.namespace, restartTarget!.pod),
    onSuccess: () => {
      toast.success('Pod deleted — its controller is recreating it')
      setRestartTarget(null)
      setTimeout(() => { void selectedQuery.refetch() }, 3000)
    },
    onError: (e) => {
      toast.error(extractApiError(e, 'Restart failed — check RBAC permissions (pods delete).'))
      setRestartTarget(null)
    },
  })

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Kubernetes</h1>
          <p className="mt-1 text-sm text-slate-500">Cluster health, nodes, deployments and pods — with logs, restarts and scaling.</p>
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
            <div className="border-b border-navy-700 px-5 py-3"><h2 className="text-sm font-semibold text-white">Deployments ({selectedQuery.data.deployments.length})</h2></div>
            <div className="max-h-80 overflow-auto">
              <table className="w-full text-left text-sm">
                <thead className="sticky top-0 bg-navy-850">
                  <tr className="text-xs text-slate-500">
                    <th scope="col" className="px-5 py-2.5 font-medium">Deployment</th>
                    <th scope="col" className="px-5 py-2.5 font-medium">Namespace</th>
                    <th scope="col" className="px-5 py-2.5 font-medium">Replicas</th>
                    <th scope="col" className="px-5 py-2.5 font-medium">Image</th>
                    {canManage && <th scope="col" className="px-5 py-2.5"><span className="sr-only">Actions</span></th>}
                  </tr>
                </thead>
                <tbody>
                  {selectedQuery.data.deployments.map((deployment) => (
                    <tr key={deployment.id} className="border-t border-navy-800 hover:bg-navy-800/40">
                      <td className="px-5 py-2 text-xs font-medium text-slate-200">{deployment.name}</td>
                      <td className="px-5 py-2 text-xs text-slate-500">{deployment.namespace}</td>
                      <td className="px-5 py-2">
                        <span className={`text-xs font-semibold ${deployment.replicasReady >= deployment.replicasDesired ? 'text-healthy' : 'text-warning'}`}>
                          {deployment.replicasReady}/{deployment.replicasDesired}
                        </span>
                      </td>
                      <td className="max-w-56 truncate px-5 py-2 text-[11px] text-slate-600">{deployment.image ?? '—'}</td>
                      {canManage && (
                        <td className="px-5 py-2 text-right">
                          <button
                            onClick={() => setScaleTarget({ namespace: deployment.namespace, deployment: deployment.name, current: deployment.replicasDesired })}
                            aria-label={`Scale ${deployment.name}`}
                            title="Scale deployment"
                            className="rounded-lg p-1.5 text-slate-500 hover:bg-navy-700 hover:text-info"
                          >
                            <Pencil className="h-3.5 w-3.5" />
                          </button>
                        </td>
                      )}
                    </tr>
                  ))}
                  {selectedQuery.data.deployments.length === 0 && (
                    <tr><td colSpan={5} className="px-5 py-6 text-center text-xs text-slate-600">No deployments synced.</td></tr>
                  )}
                </tbody>
              </table>
            </div>
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
                    {canManage && <th scope="col" className="px-5 py-2.5"><span className="sr-only">Actions</span></th>}
                  </tr>
                </thead>
                <tbody>
                  {selectedQuery.data.pods.slice(0, 100).map((pod) => (
                    <tr key={pod.id} className="border-t border-navy-800 hover:bg-navy-800/40">
                      <td className="px-5 py-2 text-xs font-medium text-slate-200">{pod.name}</td>
                      <td className="px-5 py-2 text-xs text-slate-500">{pod.namespace}</td>
                      <td className="px-5 py-2"><StatusBadge status={pod.isFailed ? 'Unhealthy' : pod.status === 'Running' ? 'Healthy' : 'Inactive'} /></td>
                      <td className={`px-5 py-2 text-xs ${pod.restartCount > 5 ? 'font-medium text-warning' : 'text-slate-300'}`}>{pod.restartCount}</td>
                      {canManage && (
                        <td className="px-5 py-2">
                          <div className="flex justify-end gap-1">
                            <button
                              onClick={() => setLogsTarget({ namespace: pod.namespace, pod: pod.name })}
                              aria-label={`View logs of ${pod.name}`}
                              title="View pod logs"
                              className="rounded-lg p-1.5 text-slate-500 hover:bg-navy-700 hover:text-info"
                            >
                              <FileText className="h-3.5 w-3.5" />
                            </button>
                            <button
                              onClick={() => setRestartTarget({ namespace: pod.namespace, pod: pod.name })}
                              aria-label={`Restart ${pod.name}`}
                              title="Restart pod (delete — controller recreates)"
                              className="rounded-lg p-1.5 text-slate-500 hover:bg-warning/10 hover:text-warning"
                            >
                              <RotateCcw className="h-3.5 w-3.5" />
                            </button>
                          </div>
                        </td>
                      )}
                    </tr>
                  ))}
                  {selectedQuery.data.pods.length === 0 && (
                    <tr><td colSpan={5} className="px-5 py-8 text-center text-sm text-slate-600">No pods synced.</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </Card>

          <PodLogsModal clusterId={selectedClusterId} target={logsTarget} onClose={() => setLogsTarget(null)} />
          <ScaleModal clusterId={selectedClusterId} target={scaleTarget} onClose={() => setScaleTarget(null)} />

          <Modal open={restartTarget !== null} title="Restart pod" onClose={() => setRestartTarget(null)}>
            <p className="text-sm leading-relaxed text-slate-400">
              Delete pod <span className="font-mono text-slate-200">{restartTarget?.namespace}/{restartTarget?.pod}</span>?
              Its controller (Deployment/StatefulSet) will recreate it immediately. Standalone pods will disappear.
            </p>
            <div className="mt-5 flex justify-end gap-2">
              <Button variant="secondary" onClick={() => setRestartTarget(null)}>Cancel</Button>
              <Button variant="danger" loading={restartMutation.isPending} onClick={() => restartMutation.mutate()}>Restart pod</Button>
            </div>
          </Modal>
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
