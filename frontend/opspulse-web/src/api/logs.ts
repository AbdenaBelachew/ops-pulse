import { apiClient } from './client'
import type { Paged } from './monitoring'

export interface PlatformLogDto {
  id: string
  timestampUtc: string
  level: string
  message: string
  exception: string | null
  source: string | null
  process: string
}

export async function listLogs(params: { page?: number; pageSize?: number; search?: string; level?: string; source?: string } = {}): Promise<Paged<PlatformLogDto>> {
  const clean = Object.fromEntries(Object.entries(params).filter(([, v]) => v !== '' && v !== undefined))
  const { data } = await apiClient.get<Paged<PlatformLogDto>>('/logs', { params: clean })
  return data
}
