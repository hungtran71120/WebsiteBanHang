<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterLink, RouterView, useRouter } from 'vue-router'
import AppIcon from '../components/icons/AppIcon.vue'
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
  { to: '/admin', label: 'Thống kê', icon: 'grid' as const },
  { to: '/admin/categories', label: 'Danh mục', icon: 'tag' as const },
  { to: '/admin/products', label: 'Sản phẩm', icon: 'box' as const },
  { to: '/admin/orders', label: 'Đơn hàng', icon: 'cart' as const },
  { to: '/admin/vouchers', label: 'Voucher', icon: 'percent' as const },
  { to: '/admin/flash-sales', label: 'Flash Sale', icon: 'zap' as const },
  { to: '/admin/banners', label: 'Banner', icon: 'image' as const },
  { to: '/admin/users', label: 'Người dùng', icon: 'users' as const },
  { to: '/admin/chat', label: 'Chat', icon: 'chat' as const },
]

function handleLogout() {
  authStore.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="admin-layout">
    <aside class="admin-sidebar">
      <RouterLink to="/" class="admin-logo">Hưng Store <span>Admin</span></RouterLink>
      <nav class="admin-nav">
        <RouterLink
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          class="admin-nav-link"
          active-class="admin-nav-link--active"
          exact-active-class="admin-nav-link--active"
        >
          <AppIcon :name="item.icon" :size="18" />
          <span>{{ item.label }}</span>
          <span v-if="item.to === '/admin/chat' && chatStore.totalUnreadMessagesForAdmin > 0" class="admin-nav-badge">
            {{ chatStore.totalUnreadMessagesForAdmin > 9 ? '9+' : chatStore.totalUnreadMessagesForAdmin }}
          </span>
        </RouterLink>
      </nav>
    </aside>
    <div class="admin-main">
      <header class="admin-topbar">
        <RouterLink to="/" class="admin-back-link">
          <AppIcon name="chevron-left" :size="16" />
          Về trang khách hàng
        </RouterLink>
        <div class="admin-account">
          <span class="admin-account__avatar"><AppIcon name="user" :size="16" /></span>
          <span>{{ authStore.user?.fullName }}</span>
          <button type="button" class="admin-logout-btn" @click="handleLogout">
            <AppIcon name="log-out" :size="14" />
            Đăng xuất
          </button>
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
  width: 232px;
  flex-shrink: 0;
  background: #18181b;
  color: #fff;
  display: flex;
  flex-direction: column;
}

.admin-logo {
  display: block;
  padding: 22px 20px;
  font-size: 16px;
  font-weight: 700;
  letter-spacing: -0.01em;
  color: #fff;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.admin-logo span {
  color: var(--shopee-orange);
}

.admin-nav {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: 12px;
}

.admin-nav-link {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border-radius: var(--radius-sm);
  color: #a1a1aa;
  text-decoration: none;
  font-size: 13.5px;
  font-weight: 500;
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
  margin-left: auto;
}

.admin-nav-link:hover {
  background: rgba(255, 255, 255, 0.06);
  color: #fff;
}

.admin-nav-link--active {
  background: var(--shopee-orange);
  color: #fff;
}

.admin-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.admin-topbar {
  height: 60px;
  flex-shrink: 0;
  background: var(--surface);
  border-bottom: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
}

.admin-back-link {
  display: flex;
  align-items: center;
  gap: 4px;
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
  gap: 10px;
  font-size: 13px;
}

.admin-account__avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: var(--shopee-orange-light);
  color: var(--shopee-orange);
}

.admin-logout-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  border: 1px solid var(--border);
  background: var(--surface);
  border-radius: var(--radius-sm);
  padding: 6px 12px;
  font-size: 13px;
  color: var(--text);
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

@media (max-width: 900px) {
  .admin-layout {
    flex-direction: column;
  }

  .admin-sidebar {
    width: 100%;
    flex-direction: row;
    align-items: center;
    overflow-x: auto;
  }

  .admin-logo {
    flex-shrink: 0;
    padding: 14px 16px;
    border-bottom: none;
    border-right: 1px solid #333;
  }

  .admin-nav {
    flex-direction: row;
    padding: 0;
  }

  .admin-nav-link {
    white-space: nowrap;
    padding: 14px 16px;
  }

  .admin-content {
    padding: 16px;
  }
}

@media (max-width: 480px) {
  .admin-topbar {
    height: auto;
    flex-wrap: wrap;
    gap: 8px;
    padding: 8px 12px;
  }

  .admin-account span {
    max-width: 120px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}
</style>
