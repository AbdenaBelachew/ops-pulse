import { apiClient } from './client'

export type RoleName =
  | 'SuperAdmin'
  | 'OrganizationAdmin'
  | 'DevOpsEngineer'
  | 'Operator'
  | 'Viewer'

export interface UserDto {
  id: string
  email: string
  fullName: string
  role: RoleName
  organizationId: string | null
  organizationName: string | null
  isActive: boolean
  createdAtUtc: string
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresAtUtc: string
  user: UserDto
}

export interface LoginInput {
  email: string
  password: string
}

export interface RegisterInput {
  organizationName: string
  fullName: string
  email: string
  password: string
}

export async function login(input: LoginInput): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>('/auth/login', input)
  return data
}

export async function register(input: RegisterInput): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>('/auth/register', input)
  return data
}

export async function logout(refreshToken: string): Promise<void> {
  await apiClient.post('/auth/logout', { refreshToken })
}

export async function fetchMe(): Promise<UserDto> {
  const { data } = await apiClient.get<UserDto>('/auth/me')
  return data
}
