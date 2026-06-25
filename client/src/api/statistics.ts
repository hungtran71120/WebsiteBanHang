import apiClient from './axios'
import type { DashboardStatistics } from '../types/admin'

export function getDashboardStatistics() {
  return apiClient.get<DashboardStatistics>('/statistics/dashboard').then((response) => response.data)
}
