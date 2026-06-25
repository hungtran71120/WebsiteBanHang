<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import AppHeader from './layouts/AppHeader.vue'
import AppFooter from './layouts/AppFooter.vue'
import ChatWidget from './components/ChatWidget.vue'
import { useAuthStore } from './stores/auth'
import { useCartStore } from './stores/cart'
import { useWishlistStore } from './stores/wishlist'

const authStore = useAuthStore()
const cartStore = useCartStore()
const wishlistStore = useWishlistStore()
const route = useRoute()
const isAdminRoute = computed(() => route.path.startsWith('/admin'))
const showChatWidget = computed(() => !isAdminRoute.value && authStore.isAuthenticated && !authStore.isAdmin)

onMounted(async () => {
  if (authStore.isAuthenticated) {
    await authStore.hydrate()
    await cartStore.fetchCart()
    if (!authStore.isAdmin) {
      await wishlistStore.fetchWishlist()
    }
  }
})
</script>

<template>
  <AppHeader v-if="!isAdminRoute" />
  <RouterView />
  <AppFooter v-if="!isAdminRoute" />
  <ChatWidget v-if="showChatWidget" />
</template>
