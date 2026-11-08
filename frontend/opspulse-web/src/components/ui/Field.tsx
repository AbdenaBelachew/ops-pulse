import type { InputHTMLAttributes } from 'react'

interface FieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  hint?: string
}

export function Field({ label, hint, id, className = '', ...props }: FieldProps) {
  return (
    <div className={className}>
      <label htmlFor={id} className="mb-1.5 block text-xs font-medium text-slate-400">
        {label}
      </label>
      <input
        id={id}
        className="w-full rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-200 placeholder:text-slate-600 focus:border-info/60 focus:outline-none focus:ring-2 focus:ring-info/20"
        {...props}
      />
      {hint && <p className="mt-1 text-[11px] leading-relaxed text-slate-600">{hint}</p>}
    </div>
  )
}
