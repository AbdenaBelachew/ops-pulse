import { useMutation, useQueryClient } from '@tanstack/react-query'
import { checkDatabase, deleteDatabase, updateDatabase, type DatabaseConnectionDto, type DatabaseMetricDto } from '../../api/monitoring'
import { extractApiError } from '../../api/client'
import { toast } from '../../stores/toastStore'

export const healthToBadge: Record<string, string> = {
  Healthy: 'Healthy',
  Degraded: 'Degraded',
  Unreachable: 'Unhealthy',
  Unknown: 'Inactive',
}

export const MANAGE_ROLES = ['SuperAdmin', 'OrganizationAdmin', 'DevOpsEngineer']
export const OPERATE_ROLES = [...MANAGE_ROLES, 'Operator']

export function formatSize(mb: number | null | undefined) {
  if (mb === null || mb === undefined) return '—'
  return mb >= 1024 ? `${(mb / 1024).toFixed(1)} GB` : `${mb} MB`
}

export function utilization(metric: DatabaseMetricDto | undefined) {
  if (!metric?.activeConnections || !metric.maxConnections) return null
  return Math.round((metric.activeConnections * 1000) / metric.maxConnections) / 10
}

/** Toast describing the outcome of a "check now". */
export function announceCheck(name: string, metric: DatabaseMetricDto) {
  if (metric.isReachable) {
    toast.success(`${name}: reachable in ${metric.responseTimeMs ?? '?'} ms · ${metric.activeConnections ?? '?'} connections · ${formatSize(metric.databaseSizeMb)}`)
  } else {
    toast.error(`${name}: unreachable — ${metric.errorMessage ?? 'unknown error'}`)
  }
}

/** Check-now / pause-resume / remove actions shared by the list and the details page. */
export function useDatabaseActions() {
  const queryClient = useQueryClient()
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['databases'] })

  const checkMutation = useMutation({
    mutationFn: (database: DatabaseConnectionDto) => checkDatabase(database.id),
    onSuccess: (metric, database) => {
      announceCheck(database.name, metric)
      invalidate()
    },
    onError: (e) => toast.error(extractApiError(e, 'Check failed.')),
  })

  const toggleMutation = useMutation({
    mutationFn: (database: DatabaseConnectionDto) => updateDatabase(database.id, { isActive: !database.isActive }),
    onSuccess: (saved) => {
      toast.success(saved.isActive ? `${saved.name}: monitoring resumed` : `${saved.name}: monitoring paused`)
      invalidate()
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not change monitoring state.')),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteDatabase,
    onSuccess: () => {
      toast.success('Database connection removed')
      invalidate()
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not remove the connection.')),
  })

  return { checkMutation, toggleMutation, deleteMutation }
}
