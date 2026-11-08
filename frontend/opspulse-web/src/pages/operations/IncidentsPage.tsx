import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, MessageSquare, Search } from 'lucide-react'
import {
  acknowledgeIncident,
  commentIncident,
  getIncident,
  listIncidents,
  resolveIncident,
  type IncidentSeverity,
  type IncidentStatus,
} from '../../api/operations'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { useRealtimeInvalidation } from '../../realtime/useRealtimeInvalidation'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Field } from '../../components/ui/Field'
import { Modal } from '../../components/ui/Modal'

const severityBadge: Record<IncidentSeverity, string> = {
  Critical: 'text-critical bg-critical/10 border-critical/25',
  Warning: 'text-warning bg-warning/10 border-warning/25',
  Informational: 'text-info bg-info/10 border-info/25',
}

const statusBadge: Record<IncidentStatus, string> = {
  Open: 'text-critical bg-critical/10 border-critical/25',
  Acknowledged: 'text-warning bg-warning/10 border-warning/25',
  Resolved: 'text-healthy bg-healthy/10 border-healthy/25',
}

function IncidentDetailModal({ incidentId, onClose }: { incidentId: string | null; onClose: () => void }) {
  const queryClient = useQueryClient()
  const canOperate = useAuthStore((s) => ['SuperAdmin', 'OrganizationAdmin', 'DevOpsEngineer', 'Operator'].includes(s.user?.role ?? ''))
  const [comment, setComment] = useState('')

  const incidentQuery = useQuery({
    queryKey: ['incidents', incidentId],
    queryFn: () => getIncident(incidentId!),
    enabled: !!incidentId,
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['incidents'] })
    queryClient.invalidateQueries({ queryKey: ['incident-summary'] })
  }

  const ackMutation = useMutation({
    mutationFn: () => acknowledgeIncident(incidentId!),
    onSuccess: () => { invalidate(); toast.success('Incident acknowledged') },
    onError: (e) => toast.error(extractApiError(e, 'Could not acknowledge.')),
  })

  const resolveMutation = useMutation({
    mutationFn: () => resolveIncident(incidentId!),
    onSuccess: () => { invalidate(); toast.success('Incident resolved') },
    onError: (e) => toast.error(extractApiError(e, 'Could not resolve.')),
  })

  const commentMutation = useMutation({
    mutationFn: () => commentIncident(incidentId!, comment),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['incidents', incidentId] })
      setComment('')
      toast.success('Comment added')
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not add comment.')),
  })

  const incident = incidentQuery.data?.incident
  const events = incidentQuery.data?.events ?? []

  return (
    <Modal open={incidentId !== null} title="Incident" onClose={onClose}>
      {incident ? (
        <div>
          <div className="flex items-start justify-between gap-2">
            <h3 className="text-sm font-semibold text-white">{incident.title}</h3>
            <div className="flex shrink-0 gap-1.5">
              <span className={`inline-block rounded-full border px-2 py-0.5 text-[10px] font-medium ${severityBadge[incident.severity]}`}>{incident.severity}</span>
              <span className={`inline-block rounded-full border px-2 py-0.5 text-[10px] font-medium ${statusBadge[incident.status]}`}>{incident.status}</span>
            </div>
          </div>
          <p className="mt-1 text-xs leading-relaxed text-slate-500">{incident.description}</p>
          <p className="mt-2 text-[11px] text-slate-600">
            Source: {incident.sourceName ?? incident.sourceType} · Started {new Date(incident.startedAtUtc).toLocaleString()} · Open {incident.durationMinutes} min
          </p>

          <div className="mt-4 max-h-56 space-y-2 overflow-y-auto border-l border-navy-600 pl-3">
            {events.map((event) => (
              <div key={event.id}>
                <p className="text-[11px] leading-relaxed text-slate-400">{event.message}</p>
                <p className="text-[10px] text-slate-600">{event.actorName ?? 'system'} · {new Date(event.occurredAtUtc).toLocaleTimeString()}</p>
              </div>
            ))}
          </div>

          {canOperate && incident.status !== 'Resolved' && (
            <>
              <div className="mt-4">
                <Field
                  id="incident-comment"
                  label="Add comment"
                  value={comment}
                  onChange={(e) => setComment(e.target.value)}
                  placeholder="What did you observe?"
                />
              </div>
              <div className="mt-4 flex justify-between">
                <Button variant="ghost" loading={commentMutation.isPending} onClick={() => comment && commentMutation.mutate()}>
                  <MessageSquare className="h-4 w-4" />Comment
                </Button>
                <div className="flex gap-2">
                  {incident.status === 'Open' && (
                    <Button variant="secondary" loading={ackMutation.isPending} onClick={() => ackMutation.mutate()}>
                      <Check className="h-4 w-4" />Acknowledge
                    </Button>
                  )}
                  <Button loading={resolveMutation.isPending} onClick={() => resolveMutation.mutate()}>Resolve</Button>
                </div>
              </div>
            </>
          )}
        </div>
      ) : (
        <p className="py-8 text-center text-sm text-slate-600">Loading incident…</p>
      )}
    </Modal>
  )
}

export function IncidentsPage() {
  const [term, setTerm] = useState('')
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<IncidentStatus | ''>('')
  const [severityFilter, setSeverityFilter] = useState<IncidentSeverity | ''>('')
  const [selectedId, setSelectedId] = useState<string | null>(null)

  useRealtimeInvalidation({ IncidentCreated: ['incidents'], IncidentUpdated: ['incidents'], IncidentResolved: ['incidents'], AlertTriggered: ['incidents'] })

  useEffect(() => {
    const timer = window.setTimeout(() => setSearch(term), 300)
    return () => window.clearTimeout(timer)
  }, [term])

  const incidentsQuery = useQuery({
    queryKey: ['incidents', search, statusFilter, severityFilter],
    queryFn: () => listIncidents({ page: 1, pageSize: 50, search, status: statusFilter, severity: severityFilter }),
    refetchInterval: 30_000,
    placeholderData: (p) => p,
  })

  return (
    <div>
      <div>
        <h1 className="text-xl font-semibold text-white">Incidents</h1>
        <p className="mt-1 text-sm text-slate-500">Automatically raised by alert rules; acknowledge, investigate and resolve.</p>
      </div>

      <div className="mt-6 flex flex-wrap items-center gap-3">
        <div className="relative min-w-56 flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-600" />
          <input
            value={term}
            onChange={(e) => setTerm(e.target.value)}
            placeholder="Search incidents…"
            aria-label="Search incidents"
            className="w-full rounded-lg border border-navy-600 bg-navy-900 py-2 pl-9 pr-3 text-sm text-slate-200 placeholder:text-slate-600 focus:border-info/60 focus:outline-none"
          />
        </div>
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value as IncidentStatus | '')}
          aria-label="Filter by status"
          className="rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-300 focus:outline-none"
        >
          <option value="">All statuses</option>
          <option value="Open">Open</option>
          <option value="Acknowledged">Acknowledged</option>
          <option value="Resolved">Resolved</option>
        </select>
        <select
          value={severityFilter}
          onChange={(e) => setSeverityFilter(e.target.value as IncidentSeverity | '')}
          aria-label="Filter by severity"
          className="rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-300 focus:outline-none"
        >
          <option value="">All severities</option>
          <option value="Critical">Critical</option>
          <option value="Warning">Warning</option>
          <option value="Informational">Informational</option>
        </select>
      </div>

      <Card className="mt-4 overflow-hidden p-0">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-navy-700 text-xs text-slate-500">
                <th scope="col" className="px-5 py-3 font-medium">Incident</th>
                <th scope="col" className="px-5 py-3 font-medium">Severity</th>
                <th scope="col" className="px-5 py-3 font-medium">Status</th>
                <th scope="col" className="px-5 py-3 font-medium">Source</th>
                <th scope="col" className="px-5 py-3 font-medium">Started</th>
              </tr>
            </thead>
            <tbody>
              {incidentsQuery.isLoading ? (
                <tr><td colSpan={5} className="px-5 py-10 text-center text-slate-600">Loading incidents…</td></tr>
              ) : (incidentsQuery.data?.items.length ?? 0) === 0 ? (
                <tr><td colSpan={5} className="px-5 py-10 text-center text-sm text-slate-500">No incidents — everything is calm. 🎉</td></tr>
              ) : (
                incidentsQuery.data?.items.map((incident) => (
                  <tr
                    key={incident.id}
                    onClick={() => setSelectedId(incident.id)}
                    className="cursor-pointer border-b border-navy-800 last:border-0 hover:bg-navy-800/40"
                  >
                    <td className="px-5 py-3.5">
                      <p className="font-medium text-slate-200">{incident.title}</p>
                      <p className="max-w-md truncate text-xs text-slate-500">{incident.description}</p>
                    </td>
                    <td className="px-5 py-3.5">
                      <span className={`inline-block rounded-full border px-2.5 py-0.5 text-xs font-medium ${severityBadge[incident.severity]}`}>{incident.severity}</span>
                    </td>
                    <td className="px-5 py-3.5">
                      <span className={`inline-block rounded-full border px-2.5 py-0.5 text-xs font-medium ${statusBadge[incident.status]}`}>{incident.status}</span>
                    </td>
                    <td className="px-5 py-3.5 text-xs text-slate-500">{incident.sourceName ?? incident.sourceType}</td>
                    <td className="px-5 py-3.5 text-xs text-slate-500">{new Date(incident.startedAtUtc).toLocaleString()}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </Card>

      <IncidentDetailModal incidentId={selectedId} onClose={() => setSelectedId(null)} />
    </div>
  )
}
