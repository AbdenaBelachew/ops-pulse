import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Copy, KeyRound, Plus, Trash2 } from 'lucide-react'
import { createApiKey, listApiKeys, revokeApiKey } from '../../api/operations'
import { listServers } from '../../api/monitoring'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Field } from '../../components/ui/Field'
import { Modal } from '../../components/ui/Modal'
import { StatusBadge } from '../../components/ui/StatusBadge'

export function AgentsPage() {
  const canManage = useAuthStore((s) => ['SuperAdmin', 'OrganizationAdmin', 'DevOpsEngineer'].includes(s.user?.role ?? ''))
  const queryClient = useQueryClient()
  const [addOpen, setAddOpen] = useState(false)
  const [keyName, setKeyName] = useState('')
  const [createdKey, setCreatedKey] = useState<string | null>(null)
  const [revoking, setRevoking] = useState<string | null>(null)

  const keysQuery = useQuery({ queryKey: ['api-keys'], queryFn: listApiKeys })
  const serversQuery = useQuery({
    queryKey: ['servers'],
    queryFn: () => listServers({ page: 1, pageSize: 50 }),
    refetchInterval: 30_000,
  })

  const createMutation = useMutation({
    mutationFn: () => createApiKey(keyName),
    onSuccess: (created) => {
      queryClient.invalidateQueries({ queryKey: ['api-keys'] })
      setCreatedKey(created.key)
      setAddOpen(false)
      setKeyName('')
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not create the API key.')),
  })

  const revokeMutation = useMutation({
    mutationFn: revokeApiKey,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['api-keys'] })
      toast.success('API key revoked — agents using it will lose access')
      setRevoking(null)
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not revoke the key.')),
  })

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Monitoring Agents</h1>
          <p className="mt-1 text-sm text-slate-500">Deploy lightweight agents that report CPU, RAM, disk and network metrics.</p>
        </div>
        {canManage && <Button onClick={() => setAddOpen(true)}><Plus className="h-4 w-4" />Create API key</Button>}
      </div>

      <Card className="mt-6">
        <h2 className="text-sm font-semibold text-white">Deploy an agent</h2>
        <ol className="mt-3 space-y-2 text-xs leading-relaxed text-slate-500">
          <li><span className="font-medium text-slate-400">1.</span> Create an API key below (keys are shown once and stored hashed).</li>
          <li><span className="font-medium text-slate-400">2.</span> Run the agent on the target machine:</li>
        </ol>
        <pre className="mt-2 overflow-x-auto rounded-lg border border-navy-600 bg-navy-950 p-3 text-[11px] leading-relaxed text-slate-300">
{`cd backend/OpsPulse.Agent
dotnet run --environment Development \\
  --OpsPulse:ApiUrl=http://localhost:5000 \\
  --OpsPulse:ApiKey=<your-api-key> \\
  --OpsPulse:IntervalSeconds=30`}
        </pre>
        <p className="mt-2 text-[11px] text-slate-600">The agent heartbeats, registers its host automatically and streams metrics every 30 seconds.</p>
      </Card>

      <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card className="overflow-hidden p-0">
          <div className="border-b border-navy-700 px-5 py-3"><h2 className="text-sm font-semibold text-white">Connected servers</h2></div>
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="text-xs text-slate-500">
                <th scope="col" className="px-5 py-2.5 font-medium">Host</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Agent</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {serversQuery.data?.items.map((server) => (
                <tr key={server.id} className="border-t border-navy-800">
                  <td className="px-5 py-2.5">
                    <p className="font-medium text-slate-200">{server.name}</p>
                    <p className="text-[10px] text-slate-600">{server.ipAddress ?? server.hostname}</p>
                  </td>
                  <td className="px-5 py-2.5 text-xs text-slate-500">{server.agentVersion ?? '—'}</td>
                  <td className="px-5 py-2.5"><StatusBadge status={server.status === 'Online' ? 'Active' : 'Inactive'} /></td>
                </tr>
              ))}
              {(serversQuery.data?.items.length ?? 0) === 0 && (
                <tr><td colSpan={3} className="px-5 py-8 text-center text-xs text-slate-600">No agents connected yet.</td></tr>
              )}
            </tbody>
          </table>
        </Card>

        <Card className="overflow-hidden p-0">
          <div className="border-b border-navy-700 px-5 py-3"><h2 className="text-sm font-semibold text-white">API keys</h2></div>
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="text-xs text-slate-500">
                <th scope="col" className="px-5 py-2.5 font-medium">Name</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Prefix</th>
                <th scope="col" className="px-5 py-2.5 font-medium">Status</th>
                {canManage && <th scope="col" className="px-5 py-2.5"><span className="sr-only">Actions</span></th>}
              </tr>
            </thead>
            <tbody>
              {keysQuery.data?.map((key) => (
                <tr key={key.id} className="border-t border-navy-800">
                  <td className="px-5 py-2.5 font-medium text-slate-200">{key.name}</td>
                  <td className="px-5 py-2.5 font-mono text-xs text-slate-500">{key.keyPrefix}…</td>
                  <td className="px-5 py-2.5"><StatusBadge status={key.isActive ? 'Active' : 'Inactive'} /></td>
                  {canManage && (
                    <td className="px-5 py-2.5 text-right">
                      {key.isActive && (
                        <button onClick={() => setRevoking(key.id)} aria-label={`Revoke ${key.name}`} className="rounded-lg p-1.5 text-slate-500 hover:bg-critical/10 hover:text-critical">
                          <Trash2 className="h-3.5 w-3.5" />
                        </button>
                      )}
                    </td>
                  )}
                </tr>
              ))}
              {(keysQuery.data?.length ?? 0) === 0 && (
                <tr><td colSpan={4} className="px-5 py-8 text-center text-xs text-slate-600">No API keys yet.</td></tr>
              )}
            </tbody>
          </table>
        </Card>
      </div>

      <Modal open={addOpen} title="Create API key" onClose={() => setAddOpen(false)}>
        <form
          onSubmit={(e) => {
            e.preventDefault()
            createMutation.mutate()
          }}
          className="space-y-4"
        >
          <Field id="key-name" label="Key name" value={keyName} onChange={(e) => setKeyName(e.target.value)} placeholder="prod-agent-1" required />
          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" variant="secondary" onClick={() => setAddOpen(false)}>Cancel</Button>
            <Button type="submit" loading={createMutation.isPending}><KeyRound className="h-4 w-4" />Create key</Button>
          </div>
        </form>
      </Modal>

      <Modal open={createdKey !== null} title="API key created" onClose={() => setCreatedKey(null)}>
        <p className="text-xs leading-relaxed text-slate-500">
          Copy this key now — it is shown <span className="text-warning">only once</span> (only its SHA-256 hash is stored).
        </p>
        <div className="mt-3 flex items-center gap-2 rounded-lg border border-navy-600 bg-navy-950 p-3">
          <code className="flex-1 break-all font-mono text-xs text-healthy">{createdKey}</code>
          <button
            onClick={() => {
              void navigator.clipboard.writeText(createdKey ?? '')
              toast.success('Key copied to clipboard')
            }}
            aria-label="Copy key"
            className="rounded-lg p-2 text-slate-400 hover:bg-navy-700 hover:text-slate-200"
          >
            <Copy className="h-4 w-4" />
          </button>
        </div>
        <div className="mt-4 flex justify-end">
          <Button onClick={() => setCreatedKey(null)}>I've stored it safely</Button>
        </div>
      </Modal>

      <Modal open={revoking !== null} title="Revoke API key" onClose={() => setRevoking(null)}>
        <p className="text-sm text-slate-400">Revoke this key? Connected agents will immediately lose access.</p>
        <div className="mt-5 flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setRevoking(null)}>Cancel</Button>
          <Button variant="danger" loading={revokeMutation.isPending} onClick={() => revoking && revokeMutation.mutate(revoking)}>Revoke</Button>
        </div>
      </Modal>
    </div>
  )
}
