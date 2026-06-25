<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterLink, RouterView, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useChatStore } from '../stores/chat'
import '../styles/admin.css'

const authStore = useAuthStore()
const chatStore = useChatStore()
const router = useRouter()

onMounted(async () => {
  chatStore.connect()
  await chatStore.loadConversationsForAdmin()
})

const navItems = [
  { to: '/admin', label: 'Thống kê' },
  { to: '/admin/categories', label: 'Danh mục' },
  { to: '/admin/products', label: 'Sản phẩm' },
  { to: '/admin/orders', label: 'Đơn hàng' },
  { to: '/admin/vouchers', label: 'Voucher' },
  { to: '/admin/flash-sales', label: 'Flash Sale' },
  { to: '/admin/users', label: 'Người dùng' },
  { to: '/admin/chat', label: 'Chat' },
]

function handleLogout() {
  authStore.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="admin-layout">
    <aside class="admin-sidebar">
      <RouterLink to="/" class="admin-logo">Shopee Clone <span>Admin</span></RouterLink>
      <nav class="admin-nav">
        <RouterLink
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          class="admin-nav-link"
          active-class="admin-nav-link--active"
          exact-active-class="admin-nav-link--active"
        >
          {{ item.label }}
          <span v-if="item.to === '/admin/chat' && chatStore.totalUnreadMessagesForAdmin > 0" class="admin-nav-badge">
            {{ chatStore.totalUnreadMessagesForAdmin > 9 ? '9+' : chatStore.totalUnreadMessagesForAdmin }}
          </span>
        </RouterLink>
      </nav>
    </aside>
    <div class="admin-main">
      <header class="admin-topbar">
        <RouterLink to="/" class="admin-back-link">← Về trang khách hàng</RouterLink>
        <div class="admin-account">
          <span>{{ authStore.user?.fullName }}</span>
          <button type="button" class="admin-logout-btn" @click="handleLogout">Đăng xuất</button>
        </div>
      </header>
      <main class="admin-content">
        <RouterView />
      </main>
    </div>
  </div>
</template>

<style scoped>
.admin-layout {
  display: flex;
  min-height: 100vh;
  width: 100%;
  background: var(--bg-page);
}

.admin-sidebar {
  width: 220px;
  flex-shrink: 0;
  background: #1a1a1a;
  color: #fff;
  display: flex;
  flex-direction: column;
}

.admin-logo {
  display: block;
  padding: 20px 16px;
  font-size: 16px;
  font-weight: 600;
  color: #fff;
  border-bottom: 1px solid #333;
}

.admin-logo span {
  color: var(--shopee-orange);
}

.admin-nav {
  display: flex;
  flex-direction: column;
  padding: 8px 0;
}

.admin-nav-link {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 20px;
  color: #ccc;
  text-decoration: none;
  font-size: 14px;
}

.admin-nav-badge {
  min-width: 18px;
  height: 18px;
  padding: 0 5px;
  border-radius: 9px;
  background: var(--shopee-orange);
  color: #fff;
  font-size: 11px;
  font-weight: 600;
  line-height: 18px;
  text-align: center;
}

.admin-nav-link:hover {
  background: #2a2a2a;
  color: #fff;
}

.admin-nav-link--active {
  background: var(--shopee-orange);
  color: #fff;
  font-weight: 500;
}

.admin-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.admin-topbar {
  height: 56px;
  flex-shrink: 0;
  background: #fff;
  border-bottom: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
}

.admin-back-link {
  text-decoration: none;
  color: var(--text-secondary);
  font-size: 13px;
}

.admin-back-link:hover {
  color: var(--shopee-orange);
}

.admin-account {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 13px;
}

.admin-logout-btn {
  border: 1px solid var(--border);
  background: #fff;
  border-radius: 2px;
  padding: 6px 12px;
}

.admin-logout-btn:hover {
  border-color: var(--shopee-orange);
  color: var(--shopee-orange);
}

.admin-content {
  flex: 1;
  padding: 24px;
  overflow-y: auto;
}
</style>
