const statusStyles: Record<string, string> = {
  Healthy: 'text-healthy bg-healthy/10 border-healthy/25',
  Active: 'text-healthy bg-healthy/10 border-healthy/25',
  Degraded: 'text-warning bg-warning/10 border-warning/25',
  Unhealthy: 'text-critical bg-critical/10 border-critical/25',
  Inactive: 'text-slate-400 bg-slate-500/10 border-slate-500/25',
}

const statusDots: Record<string, string> = {
  Healthy: 'bg-healthy',
  Active: 'bg-healthy',
  Degraded: 'bg-warning',
  Unhealthy: 'bg-critical',
  Inactive: 'bg-slate-500',
}

export function StatusBadge({ status }: { status: string }) {
  const style = statusStyles[status] ?? 'text-slate-400 bg-slate-500/10 border-slate-500/25'
  const dot = statusDots[status] ?? 'bg-slate-500'

  return (
    <span
      className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-full border px-2.5 py-0.5 text-xs font-medium ${style}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${dot}`} aria-hidden="true" />
      {status}
    </span>
  )
}
