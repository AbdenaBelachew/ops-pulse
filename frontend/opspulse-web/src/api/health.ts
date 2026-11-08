import { apiClient, infraClient } from './client'

export type HealthStatus = 'Healthy' | 'Degraded' | 'Unhealthy'

export interface ApiHealth {
  service: string
  version: string
  environment: string
  timestampUtc: string
}

export interface HealthCheckEntry {
  status: HealthStatus | string
  description: string | null
  durationMs: number
}

export interface InfrastructureHealth {
  status: string
  totalDurationMs: number
  entries: Record<string, HealthCheckEntry>
}

export async function fetchApiHealth(): Promise<ApiHealth> {
  const { data } = await apiClient.get<ApiHealth>('/health')
  return data
}

export async function fetchInfrastructureHealth(): Promise<InfrastructureHealth> {
  const { data } = await infraClient.get<InfrastructureHealth>('/health')
  return data
}
