import { useQuery } from '@tanstack/react-query'
import { Server } from 'lucide-react'
import { fetchApiHealth, fetchInfrastructureHealth } from '../../api/health'
import { Card } from '../../components/ui/Card'
import { StatusBadge } from '../../components/ui/StatusBadge'

export function SystemSettingsPage() {
  const apiQuery = useQuery({ queryKey: ['api-health'], queryFn: fetchApiHealth, refetchInterval: 30_000 })
  const infraQuery = useQuery({ queryKey: ['infra-health'], queryFn: fetchInfrastructureHealth, refetchInterval: 30_000 })

  const entries = Object.entries(infraQuery.data?.entries ?? [])

  return (
    <div className="max-w-2xl">
      <h1 className="text-xl font-semibold text-white">System settings</h1>
      <p className="mt-1 text-sm text-slate-500">
        Runtime information reported by the API. Deployment configuration is supplied
        through environment variables and secret stores — never through the UI.
      </p>

      <Card className="mt-6">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-info/15 text-info">
              <Server className="h-4.5 w-4.5" aria-hidden="true" />
            </span>
            <div>
              <p className="text-sm font-semibold text-white">{apiQuery.data?.service ?? 'OpsPulse API'}</p>
              <p className="text-xs text-slate-500">
                Version {apiQuery.data?.version ?? '—'} · {apiQuery.data?.environment ?? '—'}
              </p>
            </div>
          </div>
          {apiQuery.data && (
            <span className="text-[11px] text-slate-600">
              {new Date(apiQuery.data.timestampUtc).toLocaleTimeString()}
            </span>
          )}
        </div>
      </Card>

      <h2 className="mt-6 text-sm font-semibold text-white">Dependency health</h2>
      <div className="mt-3 space-y-3">
        {infraQuery.isLoading ? (
          <Card className="animate-pulse">
            <div className="h-4 w-40 rounded bg-navy-700" />
          </Card>
        ) : entries.length === 0 ? (
          <Card>
            <p className="text-sm text-slate-500">Health checks are unreachable.</p>
          </Card>
        ) : (
          entries.map(([key, entry]) => (
            <Card key={key}>
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium capitalize text-slate-200">{key}</p>
                  <p className="text-xs text-slate-500">
                    Check completed in {entry.durationMs.toFixed(0)} ms
                  </p>
                </div>
                <StatusBadge status={entry.status} />
              </div>
            </Card>
          ))
        )}
      </div>

      <Card className="mt-6">
        <p className="text-xs leading-relaxed text-slate-500">
          <span className="font-medium text-slate-400">Platform security notes</span> — access
          tokens expire after 15 minutes and rotate automatically; refresh tokens are stored
          hashed and reused tokens revoke the whole chain. Authentication endpoints are rate
          limited per client IP. All configuration changes are scoped by organization.
        </p>
      </Card>
    </div>
  )
}
