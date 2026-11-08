import { apiClient } from './client'
import type { PagedResult } from './users'

export interface OrganizationDto {
  id: string
  name: string
  slug: string
  description: string | null
  createdAtUtc: string
}

export async function listOrganizations(
  page = 1,
  pageSize = 100,
  search?: string,
): Promise<PagedResult<OrganizationDto>> {
  const { data } = await apiClient.get<PagedResult<OrganizationDto>>('/organizations', {
    params: { page, pageSize, search },
  })
  return data
}

export async function fetchCurrentOrganization(): Promise<OrganizationDto> {
  const { data } = await apiClient.get<OrganizationDto>('/organizations/current')
  return data
}

export async function updateCurrentOrganization(input: {
  name: string
  description?: string | null
}): Promise<OrganizationDto> {
  const { data } = await apiClient.put<OrganizationDto>('/organizations/current', input)
  return data
}
