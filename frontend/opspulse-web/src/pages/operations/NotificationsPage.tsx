import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCheck } from 'lucide-react'
import { listNotifications, markAllNotificationsRead, markNotificationRead } from '../../api/operations'
import { extractApiError } from '../../api/client'
import { toast } from '../../stores/toastStore'
import { useRealtimeInvalidation } from '../../realtime/useRealtimeInvalidation'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'

export function NotificationsPage() {
  const queryClient = useQueryClient()
  const [unreadOnly, setUnreadOnly] = useState(false)

  useRealtimeInvalidation({ AlertTriggered: ['notifications'], IncidentCreated: ['notifications'] })

  const notificationsQuery = useQuery({
    queryKey: ['notifications', unreadOnly],
    queryFn: () => listNotifications({ page: 1, pageSize: 50, unreadOnly }),
    refetchInterval: 60_000,
  })

  const markReadMutation = useMutation({
    mutationFn: markNotificationRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
      queryClient.invalidateQueries({ queryKey: ['unread-count'] })
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not mark as read.')),
  })

  const markAllMutation = useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
      queryClient.invalidateQueries({ queryKey: ['unread-count'] })
      toast.success('All notifications marked as read')
    },
    onError: (e) => toast.error(extractApiError(e, 'Could not mark all as read.')),
  })

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">Notifications</h1>
          <p className="mt-1 text-sm text-slate-500">Alerts and incident activity delivered to you.</p>
        </div>
        <div className="flex items-center gap-2">
          <label className="flex cursor-pointer items-center gap-2 text-xs text-slate-400">
            <input
              type="checkbox"
              checked={unreadOnly}
              onChange={(e) => setUnreadOnly(e.target.checked)}
              className="h-4 w-4 accent-blue-500"
            />
            Unread only
          </label>
          <Button variant="secondary" loading={markAllMutation.isPending} onClick={() => markAllMutation.mutate()}>
            <CheckCheck className="h-4 w-4" />Mark all read
          </Button>
        </div>
      </div>

      <div className="mt-6 space-y-2">
        {notificationsQuery.isLoading ? (
          <Card className="animate-pulse"><div className="h-14 rounded bg-navy-700" /></Card>
        ) : (notificationsQuery.data?.items.length ?? 0) === 0 ? (
          <Card><p className="py-8 text-center text-sm text-slate-500">No notifications{unreadOnly ? ' unread' : ''}.</p></Card>
        ) : (
          notificationsQuery.data?.items.map((notification) => (
            <button
              key={notification.id}
              onClick={() => !notification.isRead && markReadMutation.mutate(notification.id)}
              className={`block w-full rounded-xl border p-4 text-left transition-colors ${
                notification.isRead
                  ? 'border-navy-800 bg-navy-900/60 opacity-70 hover:opacity-100'
                  : 'border-info/30 bg-navy-850 hover:border-info/50'
              }`}
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className={`text-sm ${notification.isRead ? 'text-slate-400' : 'font-semibold text-white'}`}>{notification.title}</p>
                  <p className="mt-0.5 line-clamp-2 text-xs text-slate-500">{notification.message}</p>
                </div>
                <div className="shrink-0 text-right">
                  {!notification.isRead && <span className="mb-1 block h-2 w-2 rounded-full bg-info" aria-hidden="true" />}
                  <p className="text-[10px] text-slate-600">{new Date(notification.createdAtUtc).toLocaleString()}</p>
                </div>
              </div>
            </button>
          ))
        )}
      </div>
    </div>
  )
}
