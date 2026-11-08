import type { LucideIcon } from 'lucide-react'

interface ComingSoonProps {
  title: string
  description: string
  milestone: number
  icon: LucideIcon
}

export function ComingSoon({ title, description, milestone, icon: Icon }: ComingSoonProps) {
  return (
    <div className="mx-auto mt-10 max-w-lg text-center">
      <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl border border-navy-600 bg-navy-850 text-info">
        <Icon className="h-6 w-6" aria-hidden="true" />
      </div>
      <h1 className="mt-5 text-lg font-semibold text-white">{title}</h1>
      <p className="mx-auto mt-2 max-w-md text-sm leading-relaxed text-slate-500">{description}</p>
      <span className="mt-4 inline-block rounded-full border border-info/30 bg-info/10 px-3 py-1 text-xs font-medium text-info">
        Scheduled for Milestone {milestone}
      </span>
    </div>
  )
}
