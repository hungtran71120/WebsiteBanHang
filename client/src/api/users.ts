import apiClient from './axios'
import type { PagedResult } from '../types/product'
import type { AdminUser } from '../types/admin'
import type { AuthUser, UpdateProfileRequest } from '../types/auth'

export function updateProfile(request: UpdateProfileRequest) {
  return apiClient.put<AuthUser>('/users/me', request).then((response) => response.data)
}

export function getAllUsers(keyword: string | undefined, page: number, pageSize: number) {
  return apiClient
    .get<PagedResult<AdminUser>>('/users', { params: { keyword, page, pageSize } })
    .then((response) => response.data)
}

export function setUserLockout(id: string, locked: boolean) {
  return apiClient.put<AdminUser>(`/users/${id}/lockout`, { locked }).then((response) => response.data)
}
