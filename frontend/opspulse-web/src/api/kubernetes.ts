import { apiClient } from './client'
import type { KubernetesClusterDto, KubernetesNodeDto, KubernetesPodDto } from './monitoring'

export type { KubernetesClusterDto, KubernetesNodeDto, KubernetesPodDto }

export interface KubernetesDeploymentDto {
  id: string
  namespace: string
  name: string
  replicasDesired: number
  replicasReady: number
  image: string | null
  syncedAtUtc: string | null
}

export interface KubernetesClusterDetailsDto {
  cluster: KubernetesClusterDto
  nodes: KubernetesNodeDto[]
  pods: KubernetesPodDto[]
  deployments: KubernetesDeploymentDto[]
}

export async function getClusterDetails(id: string): Promise<KubernetesClusterDetailsDto> {
  const { data } = await apiClient.get<KubernetesClusterDetailsDto>(`/kubernetes/${id}`)
  return data
}

export interface PodLogsDto {
  namespace: string
  pod: string
  lines: number
  previous: boolean
  logs: string
}

export async function getPodLogs(clusterId: string, namespaceParameter: string, pod: string, lines = 200, previous = false): Promise<PodLogsDto> {
  const { data } = await apiClient.get<PodLogsDto>(`/kubernetes/${clusterId}/pods/logs`, {
    params: { namespaceParameter, pod, lines, previous },
  })
  return data
}

export async function restartPod(clusterId: string, namespaceParameter: string, pod: string): Promise<void> {
  await apiClient.post(`/kubernetes/${clusterId}/pods/${namespaceParameter}/${pod}/restart`)
}

export async function scaleDeployment(clusterId: string, namespaceParameter: string, deployment: string, replicas: number): Promise<KubernetesDeploymentDto> {
  const { data } = await apiClient.post<KubernetesDeploymentDto>(
    `/kubernetes/${clusterId}/deployments/${namespaceParameter}/${deployment}/scale`,
    { replicas },
  )
  return data
}
