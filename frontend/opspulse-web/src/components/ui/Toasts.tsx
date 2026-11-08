import { CheckCircle2, Info, XCircle, X, type LucideIcon } from 'lucide-react'
import { useToastStore, type ToastKind } from '../../stores/toastStore'

const icons: Record<ToastKind, LucideIcon> = {
  success: CheckCircle2,
  error: XCircle,
  info: Info,
}

const iconColors: Record<ToastKind, string> = {
  success: 'text-healthy',
  error: 'text-critical',
  info: 'text-info',
}

export function Toasts() {
  const { toasts, dismiss } = useToastStore()

  return (
    <div className="pointer-events-none fixed right-4 top-4 z-[100] flex w-80 flex-col gap-2">
      {toasts.map((toast) => {
        const Icon = icons[toast.kind]
        return (
          <div
            key={toast.id}
            className="pointer-events-auto flex items-start gap-2.5 rounded-xl border border-navy-600 bg-navy-850 p-3.5 shadow-xl"
          >
            <Icon className={`mt-0.5 h-4 w-4 shrink-0 ${iconColors[toast.kind]}`} aria-hidden="true" />
            <p className="flex-1 text-xs leading-relaxed text-slate-200">{toast.message}</p>
            <button
              onClick={() => dismiss(toast.id)}
              className="text-slate-600 hover:text-slate-300"
              aria-label="Dismiss"
            >
              <X className="h-3.5 w-3.5" />
            </button>
          </div>
        )
      })}
    </div>
  )
}
