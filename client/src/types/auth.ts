export type UserRole = 'Admin' | 'Customer'

export interface AuthUser {
  id: string
  email: string
  fullName: string
  phoneNumber: string | null
  address: string | null
  role: UserRole
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: AuthUser
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
  fullName: string
  phoneNumber?: string
  address?: string
}

export interface UpdateProfileRequest {
  fullName: string
  phoneNumber?: string | null
  address?: string | null
}
