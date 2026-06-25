import { HubConnectionBuilder, type HubConnection } from '@microsoft/signalr'
import { defineStore } from 'pinia'
import { getMyNotifications, getUnreadCount, markAllNotificationsAsRead, markNotificationAsRead } from '../api/notifications'
import { resolveNotificationHubUrl } from '../utils/url'
import type { AppNotification } from '../types/notification'
import { useAuthStore } from './auth'

interface NotificationState {
  connection: HubConnection | null
  notifications: AppNotification[]
  unreadCount: number
}

export const useNotificationStore = defineStore('notification', {
  state: (): NotificationState => ({
    connection: null,
    notifications: [],
    unreadCount: 0,
  }),
  actions: {
    connect() {
      if (this.connection) {
        return
      }

      const authStore = useAuthStore()
      const connection = new HubConnectionBuilder()
        .withUrl(resolveNotificationHubUrl(), { accessTokenFactory: () => authStore.accessToken ?? '' })
        .withAutomaticReconnect()
        .build()

      connection.on('ReceiveNotification', (notification: AppNotification) => {
        this.notifications = [notification, ...this.notifications]
        this.unreadCount += 1
      })

      this.connection = connection
      connection.start().catch(() => {
        this.connection = null
      })
    },
    disconnect() {
      this.connection?.stop()
      this.connection = null
    },
    async loadUnreadCount() {
      this.unreadCount = await getUnreadCount()
    },
    async loadNotifications() {
      const result = await getMyNotifications(1, 20)
      this.notifications = result.items
    },
    async markAsRead(id: string) {
      await markNotificationAsRead(id)
      const notification = this.notifications.find((n) => n.id === id)
      if (notification && !notification.isRead) {
        notification.isRead = true
        this.unreadCount = Math.max(0, this.unreadCount - 1)
      }
    },
    async markAllAsRead() {
      await markAllNotificationsAsRead()
      this.notifications = this.notifications.map((n) => ({ ...n, isRead: true }))
      this.unreadCount = 0
    },
  },
})
