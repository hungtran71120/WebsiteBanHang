<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useNotificationStore } from '../stores/notification'
import type { AppNotification } from '../types/notification'

const router = useRouter()
const notificationStore = useNotificationStore()
const isOpen = ref(false)
const isLoading = ref(false)

onMounted(async () => {
  notificationStore.connect()
  await notificationStore.loadUnreadCount()
})

async function toggleOpen() {
  isOpen.value = !isOpen.value
  if (isOpen.value) {
    isLoading.value = true
    try {
      await notificationStore.loadNotifications()
    } finally {
      isLoading.value = false
    }
  }
}

async function handleNotificationClick(notification: AppNotification) {
  await notificationStore.markAsRead(notification.id)
  isOpen.value = false
  router.push(`/orders/${notification.orderId}`)
}

async function handleMarkAllAsRead() {
  await notificationStore.markAllAsRead()
}

function formatTime(value: string) {
  return new Date(value).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' })
}
</script>

<template>
  <div class="notification-bell">
    <button type="button" class="bell-btn" @click="toggleOpen" aria-label="Thông báo">
      🔔
      <span v-if="notificationStore.unreadCount > 0" class="bell-badge">
        {{ notificationStore.unreadCount > 9 ? '9+' : notificationStore.unreadCount }}
      </span>
    </button>
    <div v-if="isOpen" class="notification-panel">
      <header class="notification-panel-header">
        <span>Thông báo</span>
        <a v-if="notificationStore.unreadCount > 0" @click="handleMarkAllAsRead">Đánh dấu đã đọc tất cả</a>
      </header>
      <div class="notification-list">
        <p v-if="isLoading" class="notification-hint">Đang tải...</p>
        <p v-else-if="notificationStore.notifications.length === 0" class="notification-hint">Chưa có thông báo nào.</p>
        <button
          v-for="notification in notificationStore.notifications"
          :key="notification.id"
          type="button"
          class="notification-item"
          :class="{ 'notification-item--unread': !notification.isRead }"
          @click="handleNotificationClick(notification)"
        >
          <p class="notification-message">{{ notification.message }}</p>
          <span class="notification-time">{{ formatTime(notification.createdAt) }}</span>
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.notification-bell {
  position: relative;
}

.bell-btn {
  position: relative;
  border: none;
  background: none;
  color: #fff;
  font-size: 22px;
  cursor: pointer;
  line-height: 1;
}

.bell-badge {
  position: absolute;
  top: -8px;
  right: -10px;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: 8px;
  background: #fff;
  color: var(--shopee-orange);
  font-size: 11px;
  font-weight: 700;
  line-height: 16px;
  text-align: center;
}

.notification-panel {
  position: absolute;
  right: 0;
  top: 36px;
  width: 320px;
  max-height: 420px;
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  z-index: 1000;
}

.notification-panel-header {
  padding: 12px 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  border-bottom: 1px solid var(--border);
}

.notification-panel-header a {
  font-size: 12px;
  font-weight: 400;
  color: var(--shopee-orange);
  cursor: pointer;
}

.notification-list {
  overflow-y: auto;
  flex: 1;
}

.notification-hint {
  color: var(--text-secondary);
  font-size: 13px;
  text-align: center;
  padding: 24px 12px;
}

.notification-item {
  display: block;
  width: 100%;
  text-align: left;
  padding: 12px 16px;
  border: none;
  border-bottom: 1px solid var(--border);
  background: #fff;
  cursor: pointer;
}

.notification-item--unread {
  background: var(--shopee-orange-light);
}

.notification-message {
  font-size: 13px;
  color: var(--text-primary);
  margin: 0 0 4px;
}

.notification-time {
  font-size: 11px;
  color: var(--text-secondary);
}
</style>
