<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppIcon from './icons/AppIcon.vue'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const authStore = useAuthStore()

const avatarInitial = computed(() => authStore.user?.fullName?.trim().charAt(0).toUpperCase() || '?')
</script>

<template>
  <aside class="account-sidebar">
    <div class="account-sidebar__profile">
      <div class="account-sidebar__avatar">{{ avatarInitial }}</div>
      <div class="account-sidebar__info">
        <p class="account-sidebar__name line-clamp-2">{{ authStore.user?.fullName }}</p>
        <RouterLink to="/account/profile" class="account-sidebar__edit">
          <AppIcon name="edit" :size="13" />
          Sửa Hồ Sơ
        </RouterLink>
      </div>
    </div>

    <nav class="account-sidebar__nav">
      <RouterLink
        to="/account/profile"
        class="account-sidebar__link"
        :class="{ active: route.name === 'account-profile' }"
      >
        Tài Khoản Của Tôi
      </RouterLink>
      <RouterLink
        v-if="!authStore.isAdmin"
        to="/orders"
        class="account-sidebar__link"
        :class="{ active: route.path.startsWith('/orders') }"
      >
        Đơn Mua
      </RouterLink>
    </nav>
  </aside>
</template>

<style scoped>
.account-sidebar {
  flex: 0 0 220px;
  background: var(--surface);
  border-radius: var(--radius-md);
  border: 1px solid var(--border);
  align-self: flex-start;
}

@media (max-width: 768px) {
  .account-sidebar {
    flex: 1 1 100%;
    width: 100%;
  }

  .account-sidebar__nav {
    flex-direction: row;
    overflow-x: auto;
    padding: 0;
  }

  .account-sidebar__link {
    white-space: nowrap;
  }
}

.account-sidebar__profile {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 20px 16px;
  border-bottom: 1px solid var(--border);
}

.account-sidebar__avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: var(--bg-page);
  border: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  font-weight: 600;
  color: var(--text-secondary);
  flex-shrink: 0;
}

.account-sidebar__name {
  font-size: 14px;
  font-weight: 600;
  margin-bottom: 4px;
}

.account-sidebar__edit {
  font-size: 12.5px;
  color: var(--text-secondary);
  text-decoration: none;
  display: flex;
  align-items: center;
  gap: 4px;
}

.account-sidebar__edit:hover {
  color: var(--shopee-orange);
}

.account-sidebar__nav {
  display: flex;
  flex-direction: column;
  padding: 8px 0;
}

.account-sidebar__link {
  padding: 10px 16px;
  font-size: 13.5px;
  color: var(--text);
  text-decoration: none;
}

.account-sidebar__link.active {
  color: var(--shopee-orange);
  font-weight: 600;
}
</style>
