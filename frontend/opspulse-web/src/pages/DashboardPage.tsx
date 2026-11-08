import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import {
  AlertTriangle,
  ArrowRight,
  BellRing,
  Building2,
  CheckCircle2,
  Database,
  Server,
  Users,
  XCircle,
} from 'lucide-react'
import { listServices, fetchInfrastructureHealth, listServers, listDatabases } from '../api/monitoring'
import { listIncidents, getUnreadCount, type IncidentDto } from '../api/operations'
import { useAuthStore } from '../stores/authStore'
import { useRealtimeInvalidation } from '../realtime/useRealtimeInvalidation'
import { StatusBadge } from '../components/ui/StatusBadge'
import { Card } from '../components/ui/Card'

const severityDot: Record<string, string> = {
  Critical: 'bg-critical',
  Warning: 'bg-warning',
  Informational: 'bg-info',
}

function KpiCard({
  label,
  value,
  detail,
  icon: Icon,
  tone = 'info',
  to,
}: {
  label: string
  value: string
  detail?: string
  icon: typeof Server
  tone?: 'info' | 'healthy' | 'warning' | 'critical'
  to?: string
}) {
  const tones = {
    info: 'bg-info/15 text-info',
    healthy: 'bg-healthy/15 text-healthy',
    warning: 'bg-warning/15 text-warning',
    critical: 'bg-critical/15 text-critical',
  }

  const content = (
    <Card className="h-full transition-colors hover:border-navy-500">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-xs text-slate-500">{label}</p>
          <p className="mt-1 text-2xl font-semibold text-white">{value}</p>
          {detail && <p className="mt-0.5 text-[11px] text-slate-600">{detail}</p>}
        </div>
        <span className={`flex h-9 w-9 items-center justify-center rounded-lg ${tones[tone]}`}>
          <Icon className="h-4.5 w-4.5" aria-hidden="true" />
        </span>
      </div>
    </Card>
  )

  return to ? <Link to={to} className="block">{content}</Link> : content
}

export function DashboardPage() {
  const user = useAuthStore((state) => state.user)
  const queryClient = useQueryClient()

  useRealtimeInvalidation({
    ServiceStatusChanged: ['dashboard-services', 'infra-health', 'incidents'],
    MetricsUpdated: ['dashboard-servers'],
    IncidentCreated: ['dashboard-incidents', 'unread-count'],
    IncidentResolved: ['dashboard-incidents'],
    AlertTriggered: ['unread-count'],
    AgentConnected: ['dashboard-servers'],
    AgentDisconnected: ['dashboard-servers'],
  })

  const servicesQuery = useQuery({ queryKey: ['dashboard-services'], queryFn: () => listServices({ pageSize: 100 }), refetchInterval: 30_000 })
  const serversQuery = useQuery({ queryKey: ['dashboard-servers'], queryFn: () => listServers({ pageSize: 100 }), refetchInterval: 30_000 })
  const databasesQuery = useQuery({ queryKey: ['dashboard-databases'], queryFn: () => listDatabases({ pageSize: 100 }), refetchInterval: 60_000 })
  const infraQuery = useQuery({ queryKey: ['infra-health'], queryFn: fetchInfrastructureHealth, refetchInterval: 30_000 })
  const incidentsQuery = useQuery({
    queryKey: ['dashboard-incidents'],
    queryFn: () => listIncidents({ page: 1, pageSize: 6 }),
    refetchInterval: 30_000,
  })
  const unreadQuery = useQuery({ queryKey: ['unread-count'], queryFn: getUnreadCount, refetchInterval: 30_000 })
  void queryClient

  const services = servicesQuery.data?.items ?? []
  const up = services.filter((s) => s.currentStatus === 'Up').length
  const down = services.filter((s) => s.currentStatus === 'Down').length
  const degraded = services.filter((s) => s.currentStatus === 'Degraded').length
  const onlineServers = (serversQuery.data?.items ?? []).filter((s) => s.status === 'Online').length
  const healthyDatabases = (databasesQuery.data?.items ?? []).filter((d) => d.health === 'Healthy').length
  const responseTimes = services.map((s) => s.avgResponseMs24h).filter((v): v is number => v !== null)
  const avgResponse = responseTimes.length > 0 ? Math.round(responseTimes.reduce((a, b) => a + b, 0) / responseTimes.length) : null
  const openIncidents = incidentsQuery.data?.items.filter((i) => i.status !== 'Resolved') ?? []

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Good to see you, {user?.fullName.split(/\s+/)[0] ?? 'there'}</h1>
          <p className="mt-1 text-sm text-slate-500">
            {user?.organizationName ?? 'Platform overview'} — live status streamed over SignalR.
          </p>
        </div>
        {user?.organizationId && (
          <Link
            to="/settings/organization"
            className="inline-flex items-center gap-2 rounded-lg border border-navy-600 bg-navy-800 px-3 py-2 text-xs font-medium text-slate-300 hover:bg-navy-700"
          >
            <Building2 className="h-3.5 w-3.5" aria-hidden="true" />
            Organization settings
          </Link>
        )}
      </div>

      <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <KpiCard
          label="Applications"
          value={`${up + degraded + down}/${services.length}`}
          detail={`${up} up · ${degraded} degraded · ${down} down`}
          icon={CheckCircle2}
          tone={down > 0 ? 'critical' : degraded > 0 ? 'warning' : 'healthy'}
          to="/applications"
        />
        <KpiCard
          label="Servers online"
          value={`${onlineServers}/${serversQuery.data?.totalCount ?? 0}`}
          detail="agents reporting"
          icon={Server}
          tone={onlineServers > 0 ? 'healthy' : 'info'}
          to="/servers"
        />
        <KpiCard
          label="Databases healthy"
          value={`${healthyDatabases}/${databasesQuery.data?.totalCount ?? 0}`}
          detail="registered connections"
          icon={Database}
          tone="info"
          to="/databases"
        />
        <KpiCard
          label="Open incidents"
          value={`${openIncidents.length}`}
          detail={unreadQuery.data ? `${unreadQuery.data} unread notifications` : undefined}
          icon={openIncidents.some((i) => i.severity === 'Critical') ? XCircle : AlertTriangle}
          tone={openIncidents.length > 0 ? 'warning' : 'healthy'}
          to="/incidents"
        />
      </div>

      <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card>
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-semibold text-white">Recent incidents</h2>
            <Link to="/incidents" className="inline-flex items-center gap-1 text-xs text-info hover:underline">
              View all <ArrowRight className="h-3 w-3" />
            </Link>
          </div>
          <div className="mt-3 space-y-2">
            {(incidentsQuery.data?.items.length ?? 0) === 0 ? (
              <p className="py-6 text-center text-xs text-slate-600">No incidents recorded.</p>
            ) : (
              incidentsQuery.data?.items.slice(0, 5).map((incident: IncidentDto) => (
                <Link
                  key={incident.id}
                  to="/incidents"
                  className="flex items-center gap-2.5 rounded-lg border border-navy-700 bg-navy-900/60 px-3 py-2 hover:border-navy-500"
                >
                  <span className={`h-2 w-2 shrink-0 rounded-full ${severityDot[incident.severity] ?? 'bg-slate-500'}`} aria-hidden="true" />
                  <span className="min-w-0 flex-1 truncate text-xs text-slate-300">{incident.title}</span>
                  <StatusBadge status={incident.status === 'Resolved' ? 'Healthy' : incident.status === 'Acknowledged' ? 'Degraded' : 'Unhealthy'} />
                </Link>
              ))
            )}
          </div>
        </Card>

        <Card>
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-semibold text-white">Platform</h2>
            <span className="text-[11px] text-slate-600">
              API {infraQuery.data?.status.toLowerCase() ?? '…'}
            </span>
          </div>
          <dl className="mt-3 space-y-2 text-xs">
            <div className="flex items-center justify-between rounded-lg border border-navy-700 bg-navy-900/60 px-3 py-2">
              <dt className="text-slate-500">Average response (24h)</dt>
              <dd className="font-medium text-slate-300">{avgResponse !== null ? `${avgResponse} ms` : '—'}</dd>
            </div>
            <div className="flex items-center justify-between rounded-lg border border-navy-700 bg-navy-900/60 px-3 py-2">
              <dt className="text-slate-500">Team members</dt>
              <dd className="font-medium text-slate-300">
                <Link to="/users" className="inline-flex items-center gap-1 hover:text-info">
                  <Users className="h-3 w-3" /> manage
                </Link>
              </dd>
            </div>
            <div className="flex items-center justify-between rounded-lg border border-navy-700 bg-navy-900/60 px-3 py-2">
              <dt className="text-slate-500">Unread notifications</dt>
              <dd className="font-medium text-slate-300">
                <Link to="/notifications" className="inline-flex items-center gap-1 hover:text-info">
                  <BellRing className="h-3 w-3" /> {unreadQuery.data ?? 0}
                </Link>
              </dd>
            </div>
          </dl>
        </Card>
      </div>
    </div>
  )
}
