import { apiClient } from './client'
import type { RoleName, UserDto } from './auth'

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface UserListQuery {
  page?: number
  pageSize?: number
  search?: string
  role?: RoleName | ''
  isActive?: boolean | ''
}

export async function listUsers(query: UserListQuery = {}): Promise<PagedResult<UserDto>> {
  const params = Object.fromEntries(
    Object.entries(query).filter(([, value]) => value !== '' && value !== undefined),
  )
  const { data } = await apiClient.get<PagedResult<UserDto>>('/users', { params })
  return data
}

export interface CreateUserInput {
  email: string
  fullName: string
  password: string
  role: RoleName
  /** Target organization — required when a SuperAdmin creates an org-scoped user. */
  organizationId?: string
}

export async function createUser(input: CreateUserInput): Promise<UserDto> {
  const { data } = await apiClient.post<UserDto>('/users', input)
  return data
}

export interface UpdateUserInput {
  fullName?: string
  role?: RoleName
  isActive?: boolean
}

export async function updateUser(id: string, input: UpdateUserInput): Promise<UserDto> {
  const { data } = await apiClient.put<UserDto>(`/users/${id}`, input)
  return data
}
