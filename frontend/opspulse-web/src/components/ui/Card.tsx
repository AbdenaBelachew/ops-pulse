import type { ReactNode } from 'react'

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <div className={`rounded-xl border border-navy-700 bg-navy-850 p-5 ${className}`}>
      {children}
    </div>
  )
}
