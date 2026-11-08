import { apiClient } from './client'
import type { Paged } from './monitoring'

const clean = (params: Record<string, unknown>) =>
  Object.fromEntries(Object.entries(params).filter(([, v]) => v !== '' && v !== undefined && v !== null))

// ---- Alert rules ----
export type AlertMetric = 'ServiceDown' | 'ResponseTime' | 'CpuUsage' | 'MemoryUsage' | 'DiskUsage' | 'DatabaseConnectionUtilization' | 'AgentHeartbeat'
export type AlertComparator = 'GreaterThan' | 'LessThan'
export type AlertSeverity = 'Informational' | 'Warning' | 'Critical'
export type ChannelType = 'InApp' | 'Email' | 'Webhook'

export interface AlertRuleDto {
  id: string
  name: string
  metric: AlertMetric
  comparator: AlertComparator
  threshold: number
  durationMinutes: number
  cooldownMinutes: number
  severity: AlertSeverity
  targetServiceId: string | null
  targetServiceName: string | null
  isEnabled: boolean
  createdAtUtc: string
}

export interface AlertChannelDto {
  id: string
  name: string
  type: ChannelType
  target: string | null
  isEnabled: boolean
  createdAtUtc: string
}

export async function listAlertRules(): Promise<AlertRuleDto[]> {
  const { data } = await apiClient.get<AlertRuleDto[]>('/alert-rules')
  return data
}

export async function createAlertRule(input: {
  name: string
  metric: AlertMetric
  comparator: AlertComparator
  threshold: number
  durationMinutes: number
  cooldownMinutes: number
  severity: AlertSeverity
  targetServiceId?: string | null
}): Promise<AlertRuleDto> {
  const { data } = await apiClient.post<AlertRuleDto>('/alert-rules', input)
  return data
}

export async function updateAlertRule(id: string, input: Partial<Parameters<typeof createAlertRule>[0]> & { isEnabled?: boolean }): Promise<AlertRuleDto> {
  const { data } = await apiClient.put<AlertRuleDto>(`/alert-rules/${id}`, input)
  return data
}

export async function deleteAlertRule(id: string): Promise<void> {
  await apiClient.delete(`/alert-rules/${id}`)
}

export async function listAlertChannels(): Promise<AlertChannelDto[]> {
  const { data } = await apiClient.get<AlertChannelDto[]>('/alert-rules/channels')
  return data
}

export async function createAlertChannel(input: { name: string; type: ChannelType; target?: string | null }): Promise<AlertChannelDto> {
  const { data } = await apiClient.post<AlertChannelDto>('/alert-rules/channels', input)
  return data
}

export async function deleteAlertChannel(id: string): Promise<void> {
  await apiClient.delete(`/alert-rules/channels/${id}`)
}

// ---- Incidents ----
export type IncidentStatus = 'Open' | 'Acknowledged' | 'Resolved'
export type IncidentSeverity = 'Informational' | 'Warning' | 'Critical'

export interface IncidentDto {
  id: string
  title: string
  description: string | null
  severity: IncidentSeverity
  status: IncidentStatus
  sourceType: string
  sourceName: string | null
  startedAtUtc: string
  acknowledgedAtUtc: string | null
  resolvedAtUtc: string | null
  lastEventAtUtc: string
  durationMinutes: number | null
}

export interface IncidentEventDto {
  id: string
  type: string
  message: string
  actorName: string | null
  occurredAtUtc: string
}

export async function listIncidents(params: { page?: number; pageSize?: number; search?: string; status?: IncidentStatus | ''; severity?: IncidentSeverity | '' } = {}): Promise<Paged<IncidentDto>> {
  const { data } = await apiClient.get<Paged<IncidentDto>>('/incidents', { params: clean(params) })
  return data
}

export async function getIncident(id: string): Promise<{ incident: IncidentDto; events: IncidentEventDto[] }> {
  const { data } = await apiClient.get(`/incidents/${id}`)
  return data
}

export async function acknowledgeIncident(id: string): Promise<IncidentDto> {
  const { data } = await apiClient.post<IncidentDto>(`/incidents/${id}/acknowledge`)
  return data
}

export async function resolveIncident(id: string): Promise<IncidentDto> {
  const { data } = await apiClient.post<IncidentDto>(`/incidents/${id}/resolve`)
  return data
}

export async function commentIncident(id: string, message: string): Promise<IncidentDto> {
  const { data } = await apiClient.post<IncidentDto>(`/incidents/${id}/comments`, { message })
  return data
}

// ---- Notifications ----
export interface NotificationDto {
  id: string
  title: string
  message: string
  type: string
  channel: string
  isRead: boolean
  createdAtUtc: string
  readAtUtc: string | null
}

export async function listNotifications(params: { page?: number; pageSize?: number; unreadOnly?: boolean } = {}): Promise<Paged<NotificationDto>> {
  const { data } = await apiClient.get<Paged<NotificationDto>>('/notifications', { params: clean(params) })
  return data
}

export async function getUnreadCount(): Promise<number> {
  const { data } = await apiClient.get<{ count: number }>('/notifications/unread-count')
  return data.count
}

export async function markNotificationRead(id: string): Promise<void> {
  await apiClient.post(`/notifications/${id}/read`)
}

export async function markAllNotificationsRead(): Promise<void> {
  await apiClient.post('/notifications/read-all')
}

// ---- Reports ----
export interface UptimeReportRow {
  serviceId: string
  serviceName: string
  environment: string
  currentStatus: string
  uptimePercent: number | null
  avgResponseMs: number | null
  checkCount: number
  downCount: number
}

export interface IncidentSummaryRow {
  open: number
  acknowledged: number
  resolved: number
  informational: number
  warning: number
  critical: number
  avgResolutionMinutes: number | null
}

export interface ServerUtilizationRow {
  serverId: string
  serverName: string
  status: string
  avgCpuPercent: number | null
  peakCpuPercent: number | null
  avgMemoryPercent: number | null
  peakMemoryPercent: number | null
}

export async function getUptimeReport(hours: number): Promise<UptimeReportRow[]> {
  const { data } = await apiClient.get<UptimeReportRow[]>('/reports/uptime', { params: { hours } })
  return data
}

export async function getIncidentSummary(hours: number): Promise<IncidentSummaryRow> {
  const { data } = await apiClient.get<IncidentSummaryRow>('/reports/incidents', { params: { hours } })
  return data
}

export async function getServerUtilization(hours: number): Promise<ServerUtilizationRow[]> {
  const { data } = await apiClient.get<ServerUtilizationRow[]>('/reports/servers', { params: { hours } })
  return data
}

// ---- Audit logs ----
export interface AuditLogDto {
  id: string
  userEmail: string | null
  action: string
  entityType: string | null
  entityId: string | null
  details: string | null
  ipAddress: string | null
  occurredAtUtc: string
}

export async function listAuditLogs(params: { page?: number; pageSize?: number; search?: string } = {}): Promise<Paged<AuditLogDto>> {
  const { data } = await apiClient.get<Paged<AuditLogDto>>('/audit-logs', { params: clean(params) })
  return data
}

// ---- API keys ----
export interface ApiKeyDto {
  id: string
  name: string
  keyPrefix: string
  isActive: boolean
  lastUsedAtUtc: string | null
  createdAtUtc: string
}

export async function listApiKeys(): Promise<ApiKeyDto[]> {
  const { data } = await apiClient.get<ApiKeyDto[]>('/api-keys')
  return data
}

export async function createApiKey(name: string): Promise<{ apiKey: ApiKeyDto; key: string }> {
  const { data } = await apiClient.post<{ apiKey: ApiKeyDto; key: string }>('/api-keys', { name })
  return data
}

export async function revokeApiKey(id: string): Promise<void> {
  await apiClient.delete(`/api-keys/${id}`)
}
