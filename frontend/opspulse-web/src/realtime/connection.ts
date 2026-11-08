import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '../stores/authStore'

export type MonitoringEvent =
  | 'ServiceStatusChanged'
  | 'MetricsUpdated'
  | 'IncidentCreated'
  | 'IncidentUpdated'
  | 'IncidentResolved'
  | 'AgentConnected'
  | 'AgentDisconnected'
  | 'AlertTriggered'

let connection: signalR.HubConnection | null = null
let started = false

/**
 * Singleton SignalR connection to the monitoring hub with automatic reconnect.
 * The access token is attached via the accessTokenFactory (query string for
 * WebSockets — handled server-side by the JWT bearer events).
 */
export function getMonitoringConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/monitoring', {
        accessTokenFactory: () => useAuthStore.getState().accessToken ?? '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.onreconnected(() => {
      started = true
    })
  }

  return connection
}

export async function ensureMonitoringConnected(): Promise<void> {
  const conn = getMonitoringConnection()
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    try {
      await conn.start()
      started = true
    } catch {
      // Retried automatically on next use / reconnect policy.
    }
  }
}

export function isMonitoringConnected(): boolean {
  return started && connection?.state === signalR.HubConnectionState.Connected
}

/** Subscribes to a monitoring event; returns an unsubscribe function. */
export function onMonitoringEvent(event: MonitoringEvent, handler: (...args: unknown[]) => void): () => void {
  const conn = getMonitoringConnection()
  conn.on(event, handler as never)
  void ensureMonitoringConnected()

  return () => {
    conn.off(event, handler as never)
  }
}
