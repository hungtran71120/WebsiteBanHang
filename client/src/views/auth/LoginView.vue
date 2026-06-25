<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { login } from '../../api/auth'
import { useAuthStore } from '../../stores/auth'
import { useCartStore } from '../../stores/cart'
import { useWishlistStore } from '../../stores/wishlist'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const cartStore = useCartStore()
const wishlistStore = useWishlistStore()

const email = ref('')
const password = ref('')
const errorMessage = ref('')
const isSubmitting = ref(false)
const showPassword = ref(false)

async function submit() {
  errorMessage.value = ''
  isSubmitting.value = true
  try {
    const auth = await login({ email: email.value, password: password.value })
    authStore.setSession(auth.accessToken, auth.user)
    await cartStore.fetchCart()
    if (auth.user.role !== 'Admin') {
      await wishlistStore.fetchWishlist()
    }
    const defaultRedirect = auth.user.role === 'Admin' ? '/admin' : '/products'
    const redirect = (route.query.redirect as string) || defaultRedirect
    router.push(redirect)
  } catch {
    errorMessage.value = 'Email hoặc mật khẩu không đúng.'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <main class="auth-page">
    <div class="auth-card">
      <h1>Đăng Nhập</h1>
      <form @submit.prevent="submit">
        <label>
          Email
          <input v-model="email" type="email" required />
        </label>
        <label>
          Mật khẩu
          <div class="password-field">
            <input v-model="password" :type="showPassword ? 'text' : 'password'" required />
            <button
              type="button"
              class="toggle-visibility"
              :aria-label="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
              @click="showPassword = !showPassword"
            >
              <svg v-if="showPassword" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M3 3l18 18" stroke-linecap="round" />
                <path
                  d="M10.58 10.58a2 2 0 0 0 2.83 2.83M9.36 5.3A10.4 10.4 0 0 1 12 5c5 0 9 4 10 7-.36 1.08-1.06 2.27-2.05 3.36M6.1 6.1C4.2 7.4 2.78 9.4 2 12c1 3 5 7 10 7 1.06 0 2.07-.18 3-.5"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </svg>
              <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path
                  d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7-10-7-10-7z"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
                <circle cx="12" cy="12" r="3" />
              </svg>
            </button>
          </div>
        </label>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <button type="submit" :disabled="isSubmitting">{{ isSubmitting ? 'Đang đăng nhập...' : 'Đăng Nhập' }}</button>
      </form>
      <p class="switch">
        Chưa có tài khoản?
        <RouterLink to="/register">Đăng Ký</RouterLink>
      </p>
    </div>
  </main>
</template>

<style scoped>
.auth-page {
  min-height: 60vh;
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 40px 16px;
}

.auth-card {
  background: #fff;
  width: 100%;
  max-width: 380px;
  padding: 32px;
  border-radius: 4px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
}

.auth-card h1 {
  font-size: 20px;
  text-align: center;
  margin-bottom: 20px;
}

form {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

label {
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-size: 13px;
  color: var(--text-secondary);
}

input {
  padding: 10px 12px;
  border: 1px solid var(--border);
  border-radius: 2px;
  width: 100%;
}

input::-ms-reveal,
input::-ms-clear {
  display: none;
}

.password-field {
  position: relative;
  display: flex;
}

.password-field input {
  padding-right: 40px;
}

.toggle-visibility {
  position: absolute;
  right: 6px;
  top: 50%;
  transform: translateY(-50%);
  width: 28px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: none;
  border: none;
  border-radius: 2px;
  padding: 0;
  color: var(--text-secondary);
}

.toggle-visibility:hover {
  color: var(--text);
}

.toggle-visibility svg {
  width: 18px;
  height: 18px;
}

button[type='submit'] {
  margin-top: 6px;
  background: var(--shopee-orange);
  color: #fff;
  border: none;
  border-radius: 2px;
  padding: 10px 0;
  font-size: 14px;
}

button[type='submit']:disabled {
  opacity: 0.6;
}

.error {
  color: var(--shopee-orange-dark);
  font-size: 13px;
}

.switch {
  text-align: center;
  margin-top: 16px;
  font-size: 13px;
  color: var(--text-secondary);
}

.switch a {
  color: var(--shopee-orange);
  text-decoration: none;
}
</style>
