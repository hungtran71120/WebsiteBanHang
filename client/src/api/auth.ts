import apiClient from './axios'
import type { AuthResponse, AuthUser, LoginRequest, RegisterRequest } from '../types/auth'

export function login(request: LoginRequest) {
  return apiClient.post<AuthResponse>('/auth/login', request).then((response) => response.data)
}

export function register(request: RegisterRequest) {
  return apiClient.post<AuthResponse>('/auth/register', request).then((response) => response.data)
}

export function getCurrentUser() {
  return apiClient.get<AuthUser>('/users/me').then((response) => response.data)
}
