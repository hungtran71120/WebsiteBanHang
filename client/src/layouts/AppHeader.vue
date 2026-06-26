<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppIcon from '../components/icons/AppIcon.vue'
import NotificationBell from '../components/NotificationBell.vue'
import { useAuthStore } from '../stores/auth'
import { useCartStore } from '../stores/cart'
import { useWishlistStore } from '../stores/wishlist'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const cartStore = useCartStore()
const wishlistStore = useWishlistStore()
const keyword = ref((route.query.keyword as string) ?? '')

watch(
  () => route.query.keyword,
  (value) => {
    keyword.value = (value as string) ?? ''
  },
)

function search() {
  router.push({ path: '/products', query: keyword.value ? { keyword: keyword.value } : {} })
}

function logout() {
  authStore.logout()
  cartStore.reset()
  wishlistStore.reset()
  router.push('/products')
}
</script>

<template>
  <header class="app-header">
    <div class="utility-bar">
      <div class="utility-bar__inner">
        <div class="utility-bar__left">
          <span>Kênh Người Bán</span>
          <span class="divider">|</span>
          <span>Liên hệ hỗ trợ</span>
        </div>
        <div class="utility-bar__right">
          <template v-if="authStore.isAuthenticated">
            <RouterLink to="/orders">Đơn Hàng Của Tôi</RouterLink>
            <span class="divider">|</span>
            <RouterLink to="/account/profile">{{ authStore.user?.fullName }}</RouterLink>
            <span class="divider">|</span>
            <a @click="logout">Đăng Xuất</a>
          </template>
          <template v-else>
            <RouterLink to="/login">Đăng Nhập</RouterLink>
            <span class="divider">|</span>
            <RouterLink to="/register">Đăng Ký</RouterLink>
          </template>
        </div>
      </div>
    </div>

    <div class="main-bar">
      <div class="main-bar__inner">
        <RouterLink to="/" class="logo">
          <span class="logo__icon">H</span>
          <span class="logo__text">Hưng Store</span>
        </RouterLink>

        <form class="search" @submit.prevent="search">
          <input v-model="keyword" type="text" placeholder="Tìm kiếm sản phẩm..." />
          <button type="submit" aria-label="Tìm kiếm"><AppIcon name="search" :size="18" /></button>
        </form>

        <NotificationBell v-if="authStore.isAuthenticated && !authStore.isAdmin" />

        <RouterLink v-if="authStore.isAuthenticated && !authStore.isAdmin" to="/wishlist" class="icon-link">
          <AppIcon name="heart" :size="22" :filled="wishlistStore.itemCount > 0" />
          <span v-if="wishlistStore.itemCount > 0" class="icon-link__badge">{{ wishlistStore.itemCount }}</span>
        </RouterLink>

        <RouterLink to="/cart" class="icon-link">
          <AppIcon name="cart" :size="22" />
          <span v-if="cartStore.itemCount > 0" class="icon-link__badge">{{ cartStore.itemCount }}</span>
        </RouterLink>
      </div>
    </div>
  </header>
</template>

<style scoped>
.app-header {
  background: var(--surface);
  position: sticky;
  top: 0;
  z-index: 50;
  box-shadow: var(--shadow-sm);
}

.utility-bar {
  font-size: 12px;
  color: var(--text-secondary);
  border-bottom: 1px solid var(--border);
}

.utility-bar__inner {
  max-width: 1200px;
  margin: 0 auto;
  padding: 7px 16px;
  display: flex;
  justify-content: space-between;
}

.utility-bar__left,
.utility-bar__right {
  display: flex;
  gap: 10px;
  align-items: center;
}

.utility-bar__right a {
  color: inherit;
  text-decoration: none;
  cursor: pointer;
}

.utility-bar__right a:hover {
  color: var(--shopee-orange);
}

.divider {
  opacity: 0.5;
}

.main-bar__inner {
  max-width: 1200px;
  margin: 0 auto;
  padding: 14px 16px;
  display: flex;
  align-items: center;
  gap: 24px;
}

.logo {
  display: flex;
  align-items: center;
  gap: 9px;
  text-decoration: none;
  color: var(--text);
  flex-shrink: 0;
}

.logo__icon {
  width: 34px;
  height: 34px;
  border-radius: var(--radius-md);
  background: var(--shopee-orange);
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 18px;
}

.logo__text {
  font-size: 19px;
  font-weight: 700;
  letter-spacing: -0.01em;
}

.search {
  flex: 1;
  display: flex;
  background: var(--bg-page);
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  overflow: hidden;
}

.search:focus-within {
  border-color: var(--shopee-orange);
  background: var(--surface);
}

.search input {
  flex: 1;
  border: none;
  background: transparent;
  padding: 10px 14px;
  outline: none;
  font-size: 14px;
}

.search button {
  border: none;
  background: var(--shopee-orange);
  color: #fff;
  padding: 0 18px;
  display: flex;
  align-items: center;
}

.search button:hover {
  background: var(--shopee-orange-dark);
}

.icon-link {
  position: relative;
  color: var(--text);
  flex-shrink: 0;
  text-decoration: none;
  display: flex;
}

.icon-link:hover {
  color: var(--shopee-orange);
}

.icon-link__badge {
  position: absolute;
  top: -7px;
  right: -9px;
  background: var(--shopee-orange);
  color: #fff;
  font-size: 11px;
  font-weight: 700;
  border-radius: 10px;
  padding: 1px 5px;
  min-width: 16px;
  text-align: center;
  line-height: 1.5;
}

@media (max-width: 768px) {
  .utility-bar__left {
    display: none;
  }

  .main-bar__inner {
    gap: 12px;
    padding: 10px 12px;
  }

  .logo__text {
    display: none;
  }

  .search input {
    padding: 8px 10px;
    font-size: 13px;
  }
}

@media (max-width: 480px) {
  .utility-bar__inner {
    padding: 6px 12px;
  }

  .utility-bar__right {
    font-size: 11px;
    gap: 6px;
  }

  .main-bar__inner {
    gap: 8px;
  }
}
</style>
