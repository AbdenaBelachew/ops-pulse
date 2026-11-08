import { useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { onMonitoringEvent, type MonitoringEvent } from './connection'

/**
 * Invalidates TanStack Query caches when real-time monitoring events arrive,
 * so open pages reflect the live state pushed by the worker through SignalR.
 */
export function useRealtimeInvalidation(mapping: Partial<Record<MonitoringEvent, string[]>>) {
  const queryClient = useQueryClient()

  useEffect(() => {
    const unsubscribers = Object.entries(mapping).map(([event, keys]) =>
      onMonitoringEvent(event as MonitoringEvent, () => {
        for (const key of keys ?? []) {
          void queryClient.invalidateQueries({ queryKey: [key] })
        }
      }),
    )

    return () => unsubscribers.forEach((unsubscribe) => unsubscribe())
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])
}
