import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Activity, Globe, RefreshCw, Search, Trash2 } from 'lucide-react'
import {
  checkServiceNow,
  createService,
  deleteService,
  listServices,
  updateService,
  type ServiceDto,
  type ServiceEnvironment,
  type ServiceStatus,
} from '../../api/monitoring'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { useRealtimeInvalidation } from '../../realtime/useRealtimeInvalidation'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Field } from '../../components/ui/Field'
import { Modal } from '../../components/ui/Modal'

const statusStyles: Record<string, string> = {
  Up: 'text-healthy bg-healthy/10 border-healthy/25',
  Degraded: 'text-warning bg-warning/10 border-warning/25',
  Down: 'text-critical bg-critical/10 border-critical/25',
  Unknown: 'text-slate-400 bg-slate-500/10 border-slate-500/25',
}

function ServiceStatusBadge({ status }: { status: ServiceStatus }) {
  return (
    <span className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-full border px-2.5 py-0.5 text-xs font-medium ${statusStyles[status] ?? statusStyles.Unknown}`}>
      <span className={`h-1.5 w-1.5 rounded-full ${status === 'Up' ? 'bg-healthy' : status === 'Degraded' ? 'bg-warning' : status === 'Down' ? 'bg-critical' : 'bg-slate-500'}`} />
      {status}
    </span>
  )
}

function ServiceModal({
  open,
  onClose,
  existing,
}: {
  open: boolean
  onClose: () => void
  existing?: ServiceDto | null
}) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [url, setUrl] = useState('')
  const [description, setDescription] = useState('')
  const [environment, setEnvironment] = useState<ServiceEnvironment>('Production')
  const [interval, setIntervalSeconds] = useState(60)
  const [timeoutSeconds, setTimeoutSeconds] = useState(10)
  const [expectedStatusCodes, setExpectedStatusCodes] = useState('200-299')

  useEffect(() => {
    if (open) {
      setName(existing?.name ?? '')
      setUrl(existing?.url ?? '')
      setDescription(existing?.description ?? '')
      setEnvironment(existing?.environment ?? 'Production')
      setIntervalSeconds(existing?.checkIntervalSeconds ?? 60)
      setTimeoutSeconds(existing?.timeoutSeconds ?? 10)
      setExpectedStatusCodes(existing?.expectedStatusCodes ?? '200-299')
    }
  }, [open, existing])

  const saveMutation = useMutation({
    mutationFn: () =>
      existing
        ? updateService(existing.id, { name, url, description: description || null, environment, checkIntervalSeconds: interval, timeoutSeconds, expectedStatusCodes: expectedStatusCodes.replace(/\s+/g, '') })
        : createService({ name, url, description: description || null, environment, checkIntervalSeconds: interval, timeoutSeconds, expectedStatusCodes: expectedStatusCodes.replace(/\s+/g, '') }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] })
      toast.success(existing ? 'Service updated' : 'Service created — first check runs within seconds')
      onClose()
    },
    onError: (error) => toast.error(extractApiError(error, 'Could not save the service.')),
  })

  return (
    <Modal open={open} title={existing ? 'Edit service' : 'Add application'} onClose={onClose}>
      <form
        onSubmit={(event: FormEvent) => {
          event.preventDefault()
          saveMutation.mutate()
        }}
        className="space-y-4"
      >
        <Field id="svc-name" label="Name" value={name} onChange={(e) => setName(e.target.value)} placeholder="payments-api" required />
        <Field id="svc-url" label="URL" value={url} onChange={(e) => setUrl(e.target.value)} placeholder="https://api.example.com/health" required />
        <Field id="svc-desc" label="Description" value={description} onChange={(e) => setDescription(e.target.value)} placeholder="What does this service do?" />
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label htmlFor="svc-env" className="mb-1.5 block text-xs font-medium text-slate-400">Environment</label>
            <select
              id="svc-env"
              value={environment}
              onChange={(e) => setEnvironment(e.target.value as ServiceEnvironment)}
              className="w-full rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-200 focus:border-info/60 focus:outline-none"
            >
              <option>Development</option>
              <option>Staging</option>
              <option>Production</option>
            </select>
          </div>
          <Field id="svc-interval" label="Check interval (s)" type="number" min={10} max={3600} value={interval} onChange={(e) => setIntervalSeconds(Number(e.target.value))} required />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Field id="svc-timeout" label="Timeout (s)" type="number" min={1} max={60} value={timeoutSeconds} onChange={(e) => setTimeoutSeconds(Number(e.target.value))} required />
          <Field
            id="svc-codes"
            label="Expected status codes"
            value={expectedStatusCodes}
            onChange={(e) => setExpectedStatusCodes(e.target.value)}
            placeholder="200-299"
            pattern="^\s*\d{3}(\s*-\s*\d{3})?(\s*,\s*\d{3}(\s*-\s*\d{3})?)*\s*$"
            title='Codes or ranges separated by commas, e.g. "200", "200,204" or "200-399"'
            required
          />
        </div>
        <p className="-mt-2 text-[11px] leading-relaxed text-slate-600">
          Redirects (301/302, e.g. to a login page) are followed and the final page is judged.
          Use <span className="text-slate-400">200-399</span> to also accept a redirect that can't be followed.
        </p>
        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="secondary" onClick={onClose}>Cancel</Button>
          <Button type="submit" loading={saveMutation.isPending}>{existing ? 'Save changes' : 'Add service'}</Button>
        </div>
      </form>
    </Modal>
  )
}

export function ApplicationsPage() {
  const canManage = useAuthStore((s) => ['SuperAdmin', 'OrganizationAdmin', 'DevOpsEngineer'].includes(s.user?.role ?? ''))
  const queryClient = useQueryClient()
  const [term, setTerm] = useState('')
  const [search, setSearch] = useState('')
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<ServiceDto | null>(null)
  const [deleting, setDeleting] = useState<ServiceDto | null>(null)

  useRealtimeInvalidation({ ServiceStatusChanged: ['services'], MetricsUpdated: ['services'] })

  useEffect(() => {
    const t = setTimeout(() => setSearch(term), 300)
    return () => clearTimeout(t)
  }, [term])

  const servicesQuery = useQuery({
    queryKey: ['services', search],
    queryFn: () => listServices({ page: 1, pageSize: 50, search }),
    refetchInterval: 30_000,
    placeholderData: (p) => p,
  })

  const checkMutation = useMutation({
    mutationFn: checkServiceNow,
    onSuccess: (check) => {
      queryClient.invalidateQueries({ queryKey: ['services'] })
      toast.success(`Check completed: ${check.statusCode ?? 'error'} in ${check.responseTimeMs} ms`)
    },
    onError: (e) => toast.error(extractApiError(e, 'Manual check failed.')),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteService,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] })
      toast.success('Service deleted')
      setDeleting(null)
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not delete the service.')),
  })

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Applications</h1>
          <p className="mt-1 text-sm text-slate-500">HTTP checks, uptime and response times for registered services.</p>
        </div>
        {canManage && <Button onClick={() => { setEditing(null); setModalOpen(true) }}><Globe className="h-4 w-4" />Add application</Button>}
      </div>

      <div className="relative mt-6 max-w-md">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-600" />
        <input
          value={term}
          onChange={(e) => setTerm(e.target.value)}
          placeholder="Search applications…"
          aria-label="Search applications"
          className="w-full rounded-lg border border-navy-600 bg-navy-900 py-2 pl-9 pr-3 text-sm text-slate-200 placeholder:text-slate-600 focus:border-info/60 focus:outline-none"
        />
      </div>

      <div className="mt-4 grid grid-cols-1 gap-3 lg:grid-cols-2">
        {servicesQuery.isLoading ? (
          <Card className="animate-pulse lg:col-span-2"><div className="h-20 rounded bg-navy-700" /></Card>
        ) : (servicesQuery.data?.items.length ?? 0) === 0 ? (
          <Card className="lg:col-span-2">
            <p className="py-8 text-center text-sm text-slate-500">No applications yet. Add your first service to start monitoring.</p>
          </Card>
        ) : (
          servicesQuery.data?.items.map((service) => (
            <Card key={service.id} className="transition-colors hover:border-navy-500">
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <Link to={`/applications/${service.id}`} className="truncate text-sm font-semibold text-white hover:text-info">
                      {service.name}
                    </Link>
                    <ServiceStatusBadge status={service.currentStatus} />
                  </div>
                  <p className="mt-0.5 truncate text-xs text-slate-500">{service.url}</p>
                  <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1 text-[11px] text-slate-500">
                    <span>24h uptime <span className="font-medium text-slate-300">{service.uptimePercent24h !== null ? `${service.uptimePercent24h.toFixed(1)}%` : '—'}</span></span>
                    <span>Response <span className="font-medium text-slate-300">{service.lastResponseTimeMs !== null ? `${service.lastResponseTimeMs} ms` : '—'}</span></span>
                    <span>Env <span className="font-medium text-slate-300">{service.environment}</span></span>
                    <span>Every <span className="font-medium text-slate-300">{service.checkIntervalSeconds}s</span></span>
                  </div>
                </div>
                {canManage && (
                  <div className="flex shrink-0 gap-1">
                    <button
                      onClick={() => checkMutation.mutate(service.id)}
                      title="Check now"
                      aria-label={`Check ${service.name} now`}
                      className="rounded-lg p-2 text-slate-500 hover:bg-navy-700 hover:text-info"
                    >
                      <Activity className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => { setEditing(service); setModalOpen(true) }}
                      title="Edit"
                      aria-label={`Edit ${service.name}`}
                      className="rounded-lg p-2 text-slate-500 hover:bg-navy-700 hover:text-slate-200"
                    >
                      <RefreshCw className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => setDeleting(service)}
                      title="Delete"
                      aria-label={`Delete ${service.name}`}
                      className="rounded-lg p-2 text-slate-500 hover:bg-critical/10 hover:text-critical"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                )}
              </div>
              {service.consecutiveFailures > 0 && (
                <p className="mt-2 rounded-lg bg-critical/10 px-2.5 py-1.5 text-[11px] text-critical">
                  {service.consecutiveFailures} consecutive failure{service.consecutiveFailures > 1 ? 's' : ''}
                </p>
              )}
            </Card>
          ))
        )}
      </div>

      <ServiceModal open={modalOpen} onClose={() => setModalOpen(false)} existing={editing} />

      <Modal open={deleting !== null} title="Delete service" onClose={() => setDeleting(null)}>
        <p className="text-sm text-slate-400">
          Delete <span className="text-slate-200">{deleting?.name}</span> and its entire check history? This cannot be undone.
        </p>
        <div className="mt-5 flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setDeleting(null)}>Cancel</Button>
          <Button variant="danger" loading={deleteMutation.isPending} onClick={() => deleting && deleteMutation.mutate(deleting.id)}>Delete</Button>
        </div>
      </Modal>
    </div>
  )
}
