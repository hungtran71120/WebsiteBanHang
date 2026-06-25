import { defineStore } from 'pinia'
import { getCurrentUser } from '../api/auth'
import type { AuthUser } from '../types/auth'

interface AuthState {
  accessToken: string | null
  user: AuthUser | null
}

function loadStoredUser(): AuthUser | null {
  const raw = localStorage.getItem('authUser')
  return raw ? (JSON.parse(raw) as AuthUser) : null
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    accessToken: localStorage.getItem('accessToken'),
    user: loadStoredUser(),
  }),
  getters: {
    isAuthenticated: (state) => !!state.accessToken,
    isAdmin: (state) => state.user?.role === 'Admin',
  },
  actions: {
    setSession(accessToken: string, user: AuthUser) {
      this.accessToken = accessToken
      this.user = user
      localStorage.setItem('accessToken', accessToken)
      localStorage.setItem('authUser', JSON.stringify(user))
    },
    updateUser(user: AuthUser) {
      this.user = user
      localStorage.setItem('authUser', JSON.stringify(user))
    },
    async hydrate() {
      if (this.accessToken && !this.user) {
        try {
          const user = await getCurrentUser()
          this.user = user
          localStorage.setItem('authUser', JSON.stringify(user))
        } catch {
          this.logout()
        }
      }
    },
    logout() {
      this.accessToken = null
      this.user = null
      localStorage.removeItem('accessToken')
      localStorage.removeItem('authUser')
    },
  },
})
