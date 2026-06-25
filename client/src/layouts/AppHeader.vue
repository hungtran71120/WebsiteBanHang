<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
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
          <span class="logo__icon">S</span>
          <span class="logo__text">ShopeeClone</span>
        </RouterLink>

        <form class="search" @submit.prevent="search">
          <input v-model="keyword" type="text" placeholder="Tìm kiếm sản phẩm..." />
          <button type="submit" aria-label="Tìm kiếm">🔍</button>
        </form>

        <NotificationBell v-if="authStore.isAuthenticated && !authStore.isAdmin" />

        <RouterLink v-if="authStore.isAuthenticated && !authStore.isAdmin" to="/wishlist" class="wishlist">
          ❤️
          <span v-if="wishlistStore.itemCount > 0" class="wishlist__badge">{{ wishlistStore.itemCount }}</span>
        </RouterLink>

        <RouterLink to="/cart" class="cart">
          🛒
          <span v-if="cartStore.itemCount > 0" class="cart__badge">{{ cartStore.itemCount }}</span>
        </RouterLink>
      </div>
    </div>
  </header>
</template>

<style scoped>
.app-header {
  background: var(--gradient-header);
}

.utility-bar {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.9);
}

.utility-bar__inner {
  max-width: 1200px;
  margin: 0 auto;
  padding: 6px 16px;
  display: flex;
  justify-content: space-between;
}

.utility-bar__left,
.utility-bar__right {
  display: flex;
  gap: 8px;
  align-items: center;
}

.utility-bar__right a {
  color: inherit;
  text-decoration: none;
  cursor: pointer;
}

.divider {
  opacity: 0.6;
}

.main-bar__inner {
  max-width: 1200px;
  margin: 0 auto;
  padding: 12px 16px 18px;
  display: flex;
  align-items: center;
  gap: 24px;
}

.logo {
  display: flex;
  align-items: center;
  gap: 8px;
  text-decoration: none;
  color: #fff;
  flex-shrink: 0;
}

.logo__icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  background: #fff;
  color: var(--shopee-orange);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 18px;
}

.logo__text {
  font-size: 22px;
  font-weight: 600;
}

.search {
  flex: 1;
  display: flex;
  background: #fff;
  border-radius: 2px;
  overflow: hidden;
}

.search input {
  flex: 1;
  border: none;
  padding: 10px 12px;
  outline: none;
  font-size: 14px;
}

.search button {
  border: none;
  background: var(--shopee-orange);
  color: #fff;
  padding: 0 20px;
  font-size: 16px;
}

.cart,
.wishlist {
  position: relative;
  color: #fff;
  font-size: 24px;
  flex-shrink: 0;
  text-decoration: none;
}

.cart__badge,
.wishlist__badge {
  position: absolute;
  top: -8px;
  right: -10px;
  background: #fff;
  color: var(--shopee-orange);
  font-size: 11px;
  font-weight: 700;
  border-radius: 10px;
  padding: 1px 6px;
  line-height: 1.4;
}

@media (max-width: 768px) {
  .utility-bar__left {
    display: none;
  }

  .main-bar__inner {
    gap: 12px;
    padding: 10px 12px 14px;
  }

  .logo__text {
    display: none;
  }

  .search input {
    padding: 8px 10px;
    font-size: 13px;
  }

  .cart,
  .wishlist {
    font-size: 20px;
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
