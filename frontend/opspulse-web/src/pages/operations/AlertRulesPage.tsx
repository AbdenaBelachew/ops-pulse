import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { BellRing, Plus, Trash2 } from 'lucide-react'
import {
  createAlertChannel,
  createAlertRule,
  deleteAlertChannel,
  deleteAlertRule,
  listAlertChannels,
  listAlertRules,
  updateAlertRule,
  type AlertMetric,
  type AlertSeverity,
  type ChannelType,
} from '../../api/operations'
import { listServices } from '../../api/monitoring'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Field } from '../../components/ui/Field'
import { Modal } from '../../components/ui/Modal'

const metricLabels: Record<AlertMetric, string> = {
  ServiceDown: 'Service unavailable',
  ResponseTime: 'Response time (ms)',
  CpuUsage: 'CPU usage (%)',
  MemoryUsage: 'Memory usage (%)',
  DiskUsage: 'Disk usage (%)',
  DatabaseConnectionUtilization: 'DB connection utilization (%)',
  AgentHeartbeat: 'Agent heartbeat missing',
}

const severityStyles: Record<AlertSeverity, string> = {
  Critical: 'text-critical bg-critical/10 border-critical/25',
  Warning: 'text-warning bg-warning/10 border-warning/25',
  Informational: 'text-info bg-info/10 border-info/25',
}

function CreateRuleModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const servicesQuery = useQuery({ queryKey: ['services'], queryFn: () => listServices({ pageSize: 100 }), enabled: open })

  const [name, setName] = useState('')
  const [metric, setMetric] = useState<AlertMetric>('CpuUsage')
  const [threshold, setThreshold] = useState(85)
  const [durationMinutes, setDurationMinutes] = useState(0)
  const [cooldownMinutes, setCooldownMinutes] = useState(15)
  const [severity, setSeverity] = useState<AlertSeverity>('Warning')
  const [targetServiceId, setTargetServiceId] = useState('')

  const createMutation = useMutation({
    mutationFn: () =>
      createAlertRule({
        name,
        metric,
        comparator: 'GreaterThan',
        threshold,
        durationMinutes,
        cooldownMinutes,
        severity,
        targetServiceId: targetServiceId || null,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['alert-rules'] })
      toast.success('Alert rule created — evaluated on every check')
      onClose()
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not create the rule.')),
  })

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    createMutation.mutate()
  }

  const isHealthMetric = metric === 'ServiceDown' || metric === 'AgentHeartbeat'

  return (
    <Modal open={open} title="Create alert rule" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <Field id="rule-name" label="Rule name" value={name} onChange={(e) => setName(e.target.value)} placeholder="High CPU" required />
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label htmlFor="rule-metric" className="mb-1.5 block text-xs font-medium text-slate-400">Metric</label>
            <select
              id="rule-metric"
              value={metric}
              onChange={(e) => setMetric(e.target.value as AlertMetric)}
              className="w-full rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-200 focus:outline-none"
            >
              {(Object.keys(metricLabels) as AlertMetric[]).map((option) => (
                <option key={option} value={option}>{metricLabels[option]}</option>
              ))}
            </select>
          </div>
          <Field
            id="rule-threshold"
            label={isHealthMetric ? 'Threshold (unused)' : 'Threshold'}
            type="number"
            step="0.1"
            value={threshold}
            onChange={(e) => setThreshold(Number(e.target.value))}
            disabled={isHealthMetric}
            required={!isHealthMetric}
          />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Field id="rule-duration" label="Sustained for (min)" type="number" min={0} max={1440} value={durationMinutes} onChange={(e) => setDurationMinutes(Number(e.target.value))} hint="0 = trigger immediately" />
          <Field id="rule-cooldown" label="Cooldown (min)" type="number" min={0} max={1440} value={cooldownMinutes} onChange={(e) => setCooldownMinutes(Number(e.target.value))} />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label htmlFor="rule-severity" className="mb-1.5 block text-xs font-medium text-slate-400">Severity</label>
            <select
              id="rule-severity"
              value={severity}
              onChange={(e) => setSeverity(e.target.value as AlertSeverity)}
              className="w-full rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-200 focus:outline-none"
            >
              <option value="Informational">Informational</option>
              <option value="Warning">Warning</option>
              <option value="Critical">Critical</option>
            </select>
          </div>
          <div>
            <label htmlFor="rule-target" className="mb-1.5 block text-xs font-medium text-slate-400">Scope</label>
            <select
              id="rule-target"
              value={targetServiceId}
              onChange={(e) => setTargetServiceId(e.target.value)}
              className="w-full rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-200 focus:outline-none"
            >
              <option value="">All services</option>
              {servicesQuery.data?.items.map((service) => (
                <option key={service.id} value={service.id}>{service.name}</option>
              ))}
            </select>
          </div>
        </div>
        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="secondary" onClick={onClose}>Cancel</Button>
          <Button type="submit" loading={createMutation.isPending}><BellRing className="h-4 w-4" />Create rule</Button>
        </div>
      </form>
    </Modal>
  )
}

export function AlertRulesPage() {
  const canManage = useAuthStore((s) => ['SuperAdmin', 'OrganizationAdmin', 'DevOpsEngineer'].includes(s.user?.role ?? ''))
  const queryClient = useQueryClient()
  const [createOpen, setCreateOpen] = useState(false)
  const [channelOpen, setChannelOpen] = useState(false)
  const [channelName, setChannelName] = useState('')
  const [channelType, setChannelType] = useState<ChannelType>('Email')
  const [channelTarget, setChannelTarget] = useState('')

  const rulesQuery = useQuery({ queryKey: ['alert-rules'], queryFn: listAlertRules })
  const channelsQuery = useQuery({ queryKey: ['alert-channels'], queryFn: listAlertChannels })

  const deleteRuleMutation = useMutation({
    mutationFn: deleteAlertRule,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['alert-rules'] })
      toast.success('Rule deleted')
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not delete the rule.')),
  })

  const toggleMutation = useMutation({
    mutationFn: ({ id, isEnabled }: { id: string; isEnabled: boolean }) => updateAlertRule(id, { isEnabled }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['alert-rules'] }),
    onError: (e) => toast.error(extractApiError(e, 'Could not update the rule.')),
  })

  const deleteChannelMutation = useMutation({
    mutationFn: deleteAlertChannel,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['alert-channels'] })
      toast.success('Channel deleted')
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not delete the channel.')),
  })

  const createChannelMutation = useMutation({
    mutationFn: () => createAlertChannel({ name: channelName, type: channelType, target: channelType === 'InApp' ? null : channelTarget }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['alert-channels'] })
      toast.success('Notification channel added')
      setChannelOpen(false)
      setChannelName('')
      setChannelTarget('')
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not add the channel.')),
  })

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Alert Rules</h1>
          <p className="mt-1 text-sm text-slate-500">Thresholds, sustained windows, cooldowns and notification channels.</p>
        </div>
        <div className="flex gap-2">
          {canManage && <Button variant="secondary" onClick={() => setChannelOpen(true)}>Add channel</Button>}
          {canManage && <Button onClick={() => setCreateOpen(true)}><Plus className="h-4 w-4" />Create rule</Button>}
        </div>
      </div>

      <Card className="mt-6 overflow-hidden p-0">
        <div className="border-b border-navy-700 px-5 py-3"><h2 className="text-sm font-semibold text-white">Rules ({rulesQuery.data?.length ?? 0})</h2></div>
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="text-xs text-slate-500">
              <th scope="col" className="px-5 py-2.5 font-medium">Rule</th>
              <th scope="col" className="px-5 py-2.5 font-medium">Condition</th>
              <th scope="col" className="px-5 py-2.5 font-medium">Severity</th>
              <th scope="col" className="px-5 py-2.5 font-medium">Scope</th>
              <th scope="col" className="px-5 py-2.5 font-medium">Enabled</th>
              {canManage && <th scope="col" className="px-5 py-2.5"><span className="sr-only">Actions</span></th>}
            </tr>
          </thead>
          <tbody>
            {rulesQuery.data?.map((rule) => (
              <tr key={rule.id} className="border-t border-navy-800">
                <td className="px-5 py-3 font-medium text-slate-200">{rule.name}</td>
                <td className="px-5 py-3 text-xs text-slate-400">
                  {rule.metric === 'ServiceDown' || rule.metric === 'AgentHeartbeat'
                    ? metricLabels[rule.metric]
                    : `${metricLabels[rule.metric]} ${rule.comparator === 'GreaterThan' ? '>' : '<'} ${rule.threshold}`}
                  <span className="ml-2 text-slate-600">sustained {rule.durationMinutes}m · cooldown {rule.cooldownMinutes}m</span>
                </td>
                <td className="px-5 py-3">
                  <span className={`inline-block rounded-full border px-2.5 py-0.5 text-xs font-medium ${severityStyles[rule.severity]}`}>{rule.severity}</span>
                </td>
                <td className="px-5 py-3 text-xs text-slate-500">{rule.targetServiceName ?? 'All services'}</td>
                <td className="px-5 py-3">
                  {canManage ? (
                    <label className="inline-flex cursor-pointer items-center">
                      <input
                        type="checkbox"
                        checked={rule.isEnabled}
                        onChange={(e) => toggleMutation.mutate({ id: rule.id, isEnabled: e.target.checked })}
                        className="h-4 w-4 accent-blue-500"
                        aria-label={`Toggle ${rule.name}`}
                      />
                    </label>
                  ) : (
                    <span className={`text-xs ${rule.isEnabled ? 'text-healthy' : 'text-slate-600'}`}>{rule.isEnabled ? 'on' : 'off'}</span>
                  )}
                </td>
                {canManage && (
                  <td className="px-5 py-3 text-right">
                    <button onClick={() => deleteRuleMutation.mutate(rule.id)} aria-label={`Delete ${rule.name}`} className="rounded-lg p-1.5 text-slate-500 hover:bg-critical/10 hover:text-critical">
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </td>
                )}
              </tr>
            ))}
            {(rulesQuery.data?.length ?? 0) === 0 && (
              <tr><td colSpan={6} className="px-5 py-8 text-center text-sm text-slate-600">No alert rules yet — create one to start raising incidents.</td></tr>
            )}
          </tbody>
        </table>
      </Card>

      <Card className="mt-4 overflow-hidden p-0">
        <div className="border-b border-navy-700 px-5 py-3"><h2 className="text-sm font-semibold text-white">Notification channels ({channelsQuery.data?.length ?? 0})</h2></div>
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="text-xs text-slate-500">
              <th scope="col" className="px-5 py-2.5 font-medium">Channel</th>
              <th scope="col" className="px-5 py-2.5 font-medium">Type</th>
              <th scope="col" className="px-5 py-2.5 font-medium">Target</th>
              {canManage && <th scope="col" className="px-5 py-2.5"><span className="sr-only">Actions</span></th>}
            </tr>
          </thead>
          <tbody>
            {channelsQuery.data?.map((channel) => (
              <tr key={channel.id} className="border-t border-navy-800">
                <td className="px-5 py-2.5 font-medium text-slate-200">{channel.name}</td>
                <td className="px-5 py-2.5 text-xs text-slate-500">{channel.type}</td>
                <td className="px-5 py-2.5 text-xs text-slate-500">{channel.target ?? 'in-app'}</td>
                {canManage && (
                  <td className="px-5 py-2.5 text-right">
                    <button onClick={() => deleteChannelMutation.mutate(channel.id)} aria-label={`Delete ${channel.name}`} className="rounded-lg p-1.5 text-slate-500 hover:bg-critical/10 hover:text-critical">
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </td>
                )}
              </tr>
            ))}
            {(channelsQuery.data?.length ?? 0) === 0 && (
              <tr><td colSpan={4} className="px-5 py-8 text-center text-sm text-slate-600">In-app notifications are always on. Add email or webhook channels here.</td></tr>
            )}
          </tbody>
        </table>
      </Card>

      <CreateRuleModal open={createOpen} onClose={() => setCreateOpen(false)} />

      <Modal open={channelOpen} title="Add notification channel" onClose={() => setChannelOpen(false)}>
        <form
          onSubmit={(e: FormEvent) => {
            e.preventDefault()
            createChannelMutation.mutate()
          }}
          className="space-y-4"
        >
          <Field id="channel-name" label="Channel name" value={channelName} onChange={(e) => setChannelName(e.target.value)} placeholder="On-call email" required />
          <div>
            <label htmlFor="channel-type" className="mb-1.5 block text-xs font-medium text-slate-400">Type</label>
            <select
              id="channel-type"
              value={channelType}
              onChange={(e) => setChannelType(e.target.value as ChannelType)}
              className="w-full rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-200 focus:outline-none"
            >
              <option value="Email">Email (SMTP must be configured)</option>
              <option value="Webhook">Webhook</option>
            </select>
          </div>
          {channelType !== 'InApp' && (
            <Field
              id="channel-target"
              label={channelType === 'Email' ? 'Email address' : 'Webhook URL'}
              value={channelTarget}
              onChange={(e) => setChannelTarget(e.target.value)}
              required
            />
          )}
          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" variant="secondary" onClick={() => setChannelOpen(false)}>Cancel</Button>
            <Button type="submit" loading={createChannelMutation.isPending}>Add channel</Button>
          </div>
        </form>
      </Modal>
    </div>
  )
}
