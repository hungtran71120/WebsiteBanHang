import apiClient from './axios'
import type { AppNotification, PagedResult } from '../types/notification'

export function getMyNotifications(page: number, pageSize: number) {
  return apiClient
    .get<PagedResult<AppNotification>>('/notifications', { params: { page, pageSize } })
    .then((response) => response.data)
}

export function getUnreadCount() {
  return apiClient.get<number>('/notifications/unread-count').then((response) => response.data)
}

export function markNotificationAsRead(id: string) {
  return apiClient.put(`/notifications/${id}/read`)
}

export function markAllNotificationsAsRead() {
  return apiClient.put('/notifications/read-all')
}
