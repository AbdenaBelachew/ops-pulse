import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import {
  AlertTriangle,
  BellRing,
  Building2,
  Container,
  Cpu,
  Database,
  FileText,
  Globe,
  Server,
  Ship,
  Users,
  type LucideIcon,
} from 'lucide-react'
import { fetchApiHealth, fetchInfrastructureHealth } from '../api/health'
import { listUsers } from '../api/users'
import { useAuthStore } from '../stores/authStore'
import { StatusBadge } from '../components/ui/StatusBadge'
import { Card } from '../components/ui/Card'

interface ModuleTile {
  title: string
  description: string
  path: string
  milestone: number
  icon: LucideIcon
}

const modules: ModuleTile[] = [
  { title: 'Applications', description: 'HTTP checks, uptime and response times', path: '/applications', milestone: 3, icon: Globe },
  { title: 'Servers', description: 'CPU, RAM, disk and network from agents', path: '/servers', milestone: 6, icon: Server },
  { title: 'Databases', description: 'PostgreSQL and SQL Server health', path: '/databases', milestone: 7, icon: Database },
  { title: 'Kubernetes', description: 'Clusters, nodes, pods and restarts', path: '/kubernetes', milestone: 10, icon: Ship },
  { title: 'Docker', description: 'Container status and resource usage', path: '/docker', milestone: 11, icon: Container },
  { title: 'Agents', description: 'Deploy lightweight monitoring agents', path: '/agents', milestone: 6, icon: Cpu },
  { title: 'Incidents', description: 'Automatic incident creation and tracking', path: '/incidents', milestone: 8, icon: AlertTriangle },
  { title: 'Alert Rules', description: 'Thresholds, cooldowns and channels', path: '/alert-rules', milestone: 9, icon: BellRing },
  { title: 'Reports', description: 'Uptime, utilization and CSV exports', path: '/reports', milestone: 9, icon: FileText },
]

function SkeletonCard() {
  return (
    <Card className="animate-pulse">
      <div className="h-3 w-24 rounded bg-navy-700" />
      <div className="mt-3 h-6 w-32 rounded bg-navy-700" />
      <div className="mt-4 h-3 w-40 rounded bg-navy-800" />
    </Card>
  )
}

export function DashboardPage() {
  const user = useAuthStore((state) => state.user)

  const apiHealthQuery = useQuery({ queryKey: ['api-health'], queryFn: fetchApiHealth, refetchInterval: 30_000 })
  const infraHealthQuery = useQuery({
    queryKey: ['infra-health'],
    queryFn: fetchInfrastructureHealth,
    refetchInterval: 30_000,
  })
  const membersQuery = useQuery({
    queryKey: ['team-size'],
    queryFn: () => listUsers({ page: 1, pageSize: 1 }),
    staleTime: 60_000,
  })

  const infra = infraHealthQuery.data
  const postgres = infra?.entries['postgresql']
  const redis = infra?.entries['redis']
  const firstName = user?.fullName.split(/\s+/)[0] ?? 'there'

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Good to see you, {firstName}</h1>
          <p className="mt-1 text-sm text-slate-500">
            {user?.organizationName ?? 'Platform overview'} — live foundation status from the OpsPulse API.
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
        {apiHealthQuery.isLoading || infraHealthQuery.isLoading ? (
          <>
            <SkeletonCard />
            <SkeletonCard />
            <SkeletonCard />
            <SkeletonCard />
          </>
        ) : (
          <>
            <Card>
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-sm font-semibold text-white">OpsPulse API</p>
                  <p className="mt-0.5 text-xs text-slate-500">REST API v1</p>
                </div>
                <StatusBadge status="Healthy" />
              </div>
              <p className="mt-4 text-xs text-slate-500">
                Version <span className="font-medium text-slate-300">{apiHealthQuery.data?.version ?? '—'}</span>
              </p>
            </Card>

            <Card>
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-sm font-semibold text-white">PostgreSQL</p>
                  <p className="mt-0.5 text-xs text-slate-500">Primary datastore</p>
                </div>
                <StatusBadge status={postgres?.status ?? 'Unhealthy'} />
              </div>
              <p className="mt-4 text-xs text-slate-500">
                Check latency{' '}
                <span className="font-medium text-slate-300">
                  {postgres ? `${postgres.durationMs.toFixed(0)} ms` : '—'}
                </span>
              </p>
            </Card>

            <Card>
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-sm font-semibold text-white">Redis</p>
                  <p className="mt-0.5 text-xs text-slate-500">Distributed cache</p>
                </div>
                <StatusBadge status={redis?.status ?? 'Unhealthy'} />
              </div>
              <p className="mt-4 text-xs text-slate-500">
                Check latency{' '}
                <span className="font-medium text-slate-300">
                  {redis ? `${redis.durationMs.toFixed(0)} ms` : '—'}
                </span>
              </p>
            </Card>

            <Card>
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-sm font-semibold text-white">Team members</p>
                  <p className="mt-0.5 text-xs text-slate-500">
                    {user?.organizationName ?? 'All organizations'}
                  </p>
                </div>
                <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-info/15 text-info">
                  <Users className="h-4 w-4" aria-hidden="true" />
                </span>
              </div>
              <p className="mt-4 text-xs text-slate-500">
                <span className="font-medium text-slate-300">{membersQuery.data?.totalCount ?? '—'}</span>{' '}
                accounts
              </p>
            </Card>
          </>
        )}
      </div>

      <h2 className="mt-8 text-sm font-semibold text-white">Modules</h2>
      <div className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <Link
          to="/users"
          className="group rounded-xl border border-navy-600 bg-navy-800 p-4 transition-colors hover:border-info/40"
        >
          <div className="flex items-center gap-3">
            <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-info/15 text-info">
              <Users className="h-4.5 w-4.5" aria-hidden="true" />
            </span>
            <div className="min-w-0">
              <p className="text-sm font-medium text-white">User management</p>
              <p className="truncate text-xs text-slate-500">Roles, access and accounts</p>
            </div>
            <span className="ml-auto rounded border border-healthy/30 bg-healthy/10 px-1.5 text-[9px] font-medium text-healthy">
              LIVE
            </span>
          </div>
        </Link>

        {modules.map((module) => (
          <Link
            key={module.path}
            to={module.path}
            className="group rounded-xl border border-navy-700 bg-navy-850 p-4 transition-colors hover:border-navy-500"
          >
            <div className="flex items-center gap-3">
              <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-navy-800 text-slate-400 group-hover:text-slate-200">
                <module.icon className="h-4.5 w-4.5" aria-hidden="true" />
              </span>
              <div className="min-w-0">
                <p className="text-sm font-medium text-white">{module.title}</p>
                <p className="truncate text-xs text-slate-500">{module.description}</p>
              </div>
              <span className="ml-auto rounded border border-navy-600 px-1.5 text-[9px] text-slate-600">
                M{module.milestone}
              </span>
            </div>
          </Link>
        ))}
      </div>
    </div>
  )
}
