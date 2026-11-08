import { apiClient, infraClient } from './client'

// ---- Services (application monitoring) ----
export type ServiceStatus = 'Unknown' | 'Up' | 'Degraded' | 'Down'
export type ServiceEnvironment = 'Development' | 'Staging' | 'Production'

export interface ServiceDto {
  id: string
  name: string
  url: string
  description: string | null
  environment: ServiceEnvironment
  checkIntervalSeconds: number
  timeoutSeconds: number
  expectedStatusCodes: string
  isActive: boolean
  currentStatus: ServiceStatus
  lastCheckedAtUtc: string | null
  lastResponseTimeMs: number | null
  consecutiveFailures: number
  uptimePercent24h: number | null
  avgResponseMs24h: number | null
  createdAtUtc: string
}

export interface ServiceCheckDto {
  checkedAtUtc: string
  isSuccess: boolean
  statusCode: number | null
  responseTimeMs: number
  errorMessage: string | null
}

export interface ServiceDetailsDto extends Omit<ServiceDto, never> {
  uptimePercent7d: number | null
  avgResponseMs24h: number | null
  recentChecks: ServiceCheckDto[]
}

export interface ServiceInput {
  name: string
  url: string
  description?: string | null
  environment: ServiceEnvironment
  checkIntervalSeconds: number
  timeoutSeconds: number
  expectedStatusCodes: string
}

export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

const clean = (params: Record<string, unknown>) =>
  Object.fromEntries(Object.entries(params).filter(([, v]) => v !== '' && v !== undefined && v !== null))

export async function listServices(params: { page?: number; pageSize?: number; search?: string; status?: ServiceStatus | '' } = {}): Promise<Paged<ServiceDto>> {
  const { data } = await apiClient.get<Paged<ServiceDto>>('/services', { params: clean(params) })
  return data
}

export async function getService(id: string): Promise<ServiceDetailsDto> {
  const { data } = await apiClient.get<ServiceDetailsDto>(`/services/${id}`)
  return data
}

export async function createService(input: ServiceInput): Promise<ServiceDto> {
  const { data } = await apiClient.post<ServiceDto>('/services', input)
  return data
}

export async function updateService(id: string, input: Partial<ServiceInput> & { isActive?: boolean }): Promise<ServiceDto> {
  const { data } = await apiClient.put<ServiceDto>(`/services/${id}`, input)
  return data
}

export async function deleteService(id: string): Promise<void> {
  await apiClient.delete(`/services/${id}`)
}

export async function checkServiceNow(id: string): Promise<ServiceCheckDto> {
  const { data } = await apiClient.post<ServiceCheckDto>(`/services/${id}/check`)
  return data
}

// ---- Infrastructure health (dashboard) ----
export interface InfrastructureHealth {
  status: string
  totalDurationMs: number
  entries: Record<string, { status: string; description: string | null; durationMs: number }>
}

export async function fetchInfrastructureHealth(): Promise<InfrastructureHealth> {
  const { data } = await infraClient.get<InfrastructureHealth>('/health')
  return data
}

// ---- Servers ----
export type ServerStatus = 'Unknown' | 'Online' | 'Offline'

export interface ServerDto {
  id: string
  name: string
  hostname: string
  ipAddress: string | null
  osName: string | null
  osVersion: string | null
  agentVersion: string | null
  cpuCores: number
  totalMemoryMb: number
  status: ServerStatus
  lastHeartbeatAtUtc: string | null
  lastCpuUsagePercent: number | null
  lastMemoryUsagePercent: number | null
  createdAtUtc: string
}

export interface ServerMetricDto {
  collectedAtUtc: string
  cpuUsagePercent: number
  memoryUsedMb: number
  memoryTotalMb: number
  diskUsedPercent: number | null
  networkInMbps: number | null
  networkOutMbps: number | null
}

export async function listServers(params: { page?: number; pageSize?: number; search?: string; status?: ServerStatus | '' } = {}): Promise<Paged<ServerDto>> {
  const { data } = await apiClient.get<Paged<ServerDto>>('/servers', { params: clean(params) })
  return data
}

export async function getServer(id: string): Promise<{ server: ServerDto; recentMetrics: ServerMetricDto[] }> {
  const { data } = await apiClient.get(`/servers/${id}`)
  return data
}

export async function deleteServer(id: string): Promise<void> {
  await apiClient.delete(`/servers/${id}`)
}

// ---- Databases ----
export type DatabaseType = 'PostgreSQL' | 'SqlServer'
export type DatabaseHealth = 'Unknown' | 'Healthy' | 'Degraded' | 'Unreachable'

export interface DatabaseConnectionDto {
  id: string
  name: string
  databaseType: DatabaseType
  host: string
  port: number
  databaseName: string
  username: string
  health: DatabaseHealth
  lastCheckedAtUtc: string | null
  isActive: boolean
  createdAtUtc: string
}

export interface DatabaseMetricDto {
  collectedAtUtc: string
  isReachable: boolean
  activeConnections: number | null
  maxConnections: number | null
  databaseSizeMb: number | null
  responseTimeMs: number | null
  errorMessage: string | null
}

export async function listDatabases(params: { page?: number; pageSize?: number; search?: string } = {}): Promise<Paged<DatabaseConnectionDto>> {
  const { data } = await apiClient.get<Paged<DatabaseConnectionDto>>('/databases', { params: clean(params) })
  return data
}

export async function getDatabase(id: string): Promise<{ connection: DatabaseConnectionDto; recentMetrics: DatabaseMetricDto[] }> {
  const { data } = await apiClient.get(`/databases/${id}`)
  return data
}

export async function createDatabase(input: { name: string; databaseType: DatabaseType; host: string; port: number; databaseName: string; username: string; password: string }): Promise<DatabaseConnectionDto> {
  const { data } = await apiClient.post<DatabaseConnectionDto>('/databases', input)
  return data
}

export async function deleteDatabase(id: string): Promise<void> {
  await apiClient.delete(`/databases/${id}`)
}

// ---- Kubernetes ----
export interface KubernetesClusterDto {
  id: string
  name: string
  apiEndpoint: string | null
  isReachable: boolean
  kubernetesVersion: string | null
  nodeCount: number
  podCount: number
  failedPodCount: number
  lastSyncedAtUtc: string | null
  lastError: string | null
  createdAtUtc: string
}

export interface KubernetesNodeDto {
  id: string
  name: string
  status: string
  roles: string[]
  kubernetesVersion: string | null
  cpuCapacityCores: number | null
  memoryCapacityMb: number | null
  syncedAtUtc: string | null
}

export interface KubernetesPodDto {
  id: string
  namespace: string
  name: string
  status: string
  restartCount: number
  nodeName: string | null
  isFailed: boolean
  startedAtUtc: string | null
}

export async function listClusters(): Promise<KubernetesClusterDto[]> {
  const { data } = await apiClient.get<KubernetesClusterDto[]>('/kubernetes')
  return data
}

export async function getCluster(id: string): Promise<{ cluster: KubernetesClusterDto; nodes: KubernetesNodeDto[]; pods: KubernetesPodDto[] }> {
  const { data } = await apiClient.get(`/kubernetes/${id}`)
  return data
}

export async function createCluster(input: { name: string; kubeConfigYaml?: string | null }): Promise<KubernetesClusterDto> {
  const { data } = await apiClient.post<KubernetesClusterDto>('/kubernetes', input)
  return data
}

export async function syncCluster(id: string): Promise<KubernetesClusterDto> {
  const { data } = await apiClient.post<KubernetesClusterDto>(`/kubernetes/${id}/sync`)
  return data
}

export async function deleteCluster(id: string): Promise<void> {
  await apiClient.delete(`/kubernetes/${id}`)
}

// ---- Docker ----
export interface DockerHostDto {
  id: string
  name: string
  endpoint: string
  isReachable: boolean
  containerCount: number
  runningCount: number
  lastSyncedAtUtc: string | null
  lastError: string | null
  createdAtUtc: string
}

export interface DockerContainerDto {
  id: string
  containerId: string
  name: string
  image: string
  state: string
  status: string
  cpuUsagePercent: number | null
  memoryUsedMb: number | null
  memoryLimitMb: number | null
  startedAtUtc: string | null
  syncedAtUtc: string | null
}

export async function listDockerHosts(): Promise<DockerHostDto[]> {
  const { data } = await apiClient.get<DockerHostDto[]>('/docker')
  return data
}

export async function getDockerHost(id: string): Promise<{ host: DockerHostDto; containers: DockerContainerDto[] }> {
  const { data } = await apiClient.get(`/docker/${id}`)
  return data
}

export async function createDockerHost(input: { name: string; endpoint: string }): Promise<DockerHostDto> {
  const { data } = await apiClient.post<DockerHostDto>('/docker', input)
  return data
}

export async function syncDockerHost(id: string): Promise<DockerHostDto> {
  const { data } = await apiClient.post<DockerHostDto>(`/docker/${id}/sync`)
  return data
}

export async function deleteDockerHost(id: string): Promise<void> {
  await apiClient.delete(`/docker/${id}`)
}
