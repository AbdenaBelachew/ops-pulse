import { useQuery } from '@tanstack/react-query'
import { fetchApiHealth, fetchInfrastructureHealth } from '../api/health'

const statusStyles: Record<string, string> = {
  Healthy: 'text-healthy bg-healthy/10 border-healthy/25',
  Degraded: 'text-warning bg-warning/10 border-warning/25',
  Unhealthy: 'text-critical bg-critical/10 border-critical/25',
}

const statusDots: Record<string, string> = {
  Healthy: 'bg-healthy',
  Degraded: 'bg-warning',
  Unhealthy: 'bg-critical',
}

function statusStyle(status: string): string {
  return statusStyles[status] ?? 'text-slate-400 bg-slate-500/10 border-slate-500/25'
}

function statusDot(status: string): string {
  return statusDots[status] ?? 'bg-slate-500'
}

function StatusBadge({ status }: { status: string }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium ${statusStyle(status)}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${statusDot(status)}`} aria-hidden="true" />
      {status}
    </span>
  )
}

function CardSkeleton() {
  return (
    <div className="animate-pulse rounded-xl border border-navy-700 bg-navy-850 p-5">
      <div className="h-3 w-24 rounded bg-navy-700" />
      <div className="mt-3 h-6 w-32 rounded bg-navy-700" />
      <div className="mt-4 h-3 w-40 rounded bg-navy-800" />
    </div>
  )
}

interface DependencyCardProps {
  name: string
  description: string
  status?: string
  latencyMs?: number
}

function DependencyCard({ name, description, status, latencyMs }: DependencyCardProps) {
  const known = status !== undefined
  return (
    <div className="rounded-xl border border-navy-700 bg-navy-850 p-5">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm font-semibold text-white">{name}</p>
          <p className="mt-0.5 text-xs text-slate-500">{description}</p>
        </div>
        {known ? <StatusBadge status={status} /> : <StatusBadge status="Unhealthy" />}
      </div>
      {known && latencyMs !== undefined && (
        <p className="mt-4 text-xs text-slate-500">
          Check latency <span className="font-medium text-slate-300">{latencyMs.toFixed(0)} ms</span>
        </p>
      )}
    </div>
  )
}

function OverviewPage() {
  const apiHealthQuery = useQuery({ queryKey: ['api-health'], queryFn: fetchApiHealth })
  const infraHealthQuery = useQuery({
    queryKey: ['infra-health'],
    queryFn: fetchInfrastructureHealth,
    refetchInterval: 30_000,
  })

  const infra = infraHealthQuery.data
  const overallStatus: string = infra?.status ?? 'Loading'
  const postgres = infra?.entries['postgresql']
  const redis = infra?.entries['redis']

  return (
    <div>
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-white">Overview</h1>
          <p className="mt-1 text-sm text-slate-500">
            Platform foundation status, reported live by the OpsPulse API.
          </p>
        </div>
        <StatusBadge status={overallStatus} />
      </div>

      <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {apiHealthQuery.isLoading ? (
          <CardSkeleton />
        ) : apiHealthQuery.isError ? (
          <DependencyCard
            name="OpsPulse API"
            description="API metadata endpoint"
            status="Unhealthy"
          />
        ) : (
          <div className="rounded-xl border border-navy-700 bg-navy-850 p-5">
            <p className="text-sm font-semibold text-white">OpsPulse API</p>
            <p className="mt-0.5 text-xs text-slate-500">REST API v1</p>
            <dl className="mt-4 space-y-1 text-xs">
              <div className="flex justify-between">
                <dt className="text-slate-500">Version</dt>
                <dd className="font-medium text-slate-300">{apiHealthQuery.data?.version}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500">Environment</dt>
                <dd className="font-medium text-slate-300">{apiHealthQuery.data?.environment}</dd>
              </div>
            </dl>
          </div>
        )}

        {infraHealthQuery.isLoading ? (
          <>
            <CardSkeleton />
            <CardSkeleton />
            <CardSkeleton />
          </>
        ) : (
          <>
            <DependencyCard
              name="PostgreSQL"
              description="Primary datastore (EF Core)"
              status={postgres?.status}
              latencyMs={postgres?.durationMs}
            />
            <DependencyCard
              name="Redis"
              description="Distributed cache"
              status={redis?.status}
              latencyMs={redis?.durationMs}
            />
            <div className="rounded-xl border border-navy-700 bg-navy-850 p-5">
              <p className="text-sm font-semibold text-white">Health sweep</p>
              <p className="mt-0.5 text-xs text-slate-500">All registered checks</p>
              <p className="mt-4 text-xs text-slate-500">
                Completed in{' '}
                <span className="font-medium text-slate-300">
                  {infra ? infra.totalDurationMs.toFixed(0) : '—'} ms
                </span>
              </p>
            </div>
          </>
        )}
      </div>

      <div className="mt-8 rounded-xl border border-navy-700 bg-navy-850 p-5">
        <h2 className="text-sm font-semibold text-white">Coming next</h2>
        <p className="mt-2 text-xs leading-relaxed text-slate-500">
          The sidebar navigation, application monitoring engine, real-time dashboard (SignalR),
          server/database/Kubernetes/Docker monitoring, incidents and alerting modules are
          delivered in Milestones 2-12. This page verifies the foundation: the API boots,
          migrations applied and both backing services are reachable.
        </p>
      </div>
    </div>
  )
}

export default OverviewPage
