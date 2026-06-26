<script setup lang="ts">
import { onMounted, ref } from 'vue'
import AccountSidebar from '../../components/AccountSidebar.vue'
import { getCurrentUser } from '../../api/auth'
import { updateProfile } from '../../api/users'
import { useAuthStore } from '../../stores/auth'

const authStore = useAuthStore()

const email = ref('')
const fullName = ref('')
const phoneNumber = ref('')
const address = ref('')
const isLoading = ref(true)
const isSubmitting = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

async function load() {
  isLoading.value = true
  try {
    const user = await getCurrentUser()
    authStore.updateUser(user)
    email.value = user.email
    fullName.value = user.fullName
    phoneNumber.value = user.phoneNumber ?? ''
    address.value = user.address ?? ''
  } finally {
    isLoading.value = false
  }
}

async function submit() {
  errorMessage.value = ''
  successMessage.value = ''
  isSubmitting.value = true
  try {
    const updated = await updateProfile({
      fullName: fullName.value,
      phoneNumber: phoneNumber.value || null,
      address: address.value || null,
    })
    authStore.updateUser(updated)
    successMessage.value = 'Cập nhật hồ sơ thành công.'
  } catch (error: unknown) {
    const messages = (error as { response?: { data?: string[] } })?.response?.data
    errorMessage.value = messages?.[0] ?? 'Cập nhật hồ sơ thất bại. Vui lòng thử lại.'
  } finally {
    isSubmitting.value = false
  }
}

onMounted(load)
</script>

<template>
  <main class="account-page">
    <div class="account-page__inner">
      <AccountSidebar />

      <section class="account-content">
        <h1>Hồ Sơ Của Tôi</h1>
        <p class="account-content__subtitle">Quản lý thông tin hồ sơ để bảo mật tài khoản</p>

        <p v-if="isLoading" class="state-message">Đang tải...</p>
        <form v-else class="profile-form" @submit.prevent="submit">
          <label>
            Email
            <input :value="email" type="email" disabled />
          </label>
          <label>
            Họ tên
            <input v-model="fullName" type="text" required maxlength="200" />
          </label>
          <label>
            Số điện thoại
            <input v-model="phoneNumber" type="tel" maxlength="20" />
          </label>
          <label>
            Địa chỉ giao hàng
            <input
              v-model="address"
              type="text"
              maxlength="500"
              placeholder="Dùng làm địa chỉ mặc định khi đặt hàng"
            />
          </label>
          <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
          <p v-if="successMessage" class="success">{{ successMessage }}</p>
          <button type="submit" :disabled="isSubmitting">{{ isSubmitting ? 'Đang lưu...' : 'Lưu' }}</button>
        </form>
      </section>
    </div>
  </main>
</template>

<style scoped>
.account-page {
  background: var(--bg-page);
  padding: 16px 0 40px;
  min-height: 60vh;
}

.account-page__inner {
  max-width: 1000px;
  margin: 0 auto;
  padding: 0 16px;
  display: flex;
  gap: 16px;
  align-items: flex-start;
}

@media (max-width: 768px) {
  .account-page__inner {
    flex-direction: column;
  }
}

.account-content {
  flex: 1;
  min-width: 0;
  background: #fff;
  padding: 24px 32px;
}

@media (max-width: 480px) {
  .account-content {
    padding: 20px 16px;
  }
}

.account-content h1 {
  font-size: 18px;
  margin-bottom: 4px;
}

.account-content__subtitle {
  font-size: 13px;
  color: var(--text-secondary);
  margin-bottom: 24px;
  padding-bottom: 20px;
  border-bottom: 1px solid var(--border);
}

.profile-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
  max-width: 420px;
}

.profile-form label {
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-size: 13px;
  color: var(--text-secondary);
}

.profile-form input {
  padding: 10px 12px;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
}

.profile-form input:disabled {
  background: var(--bg-page);
  color: var(--text-secondary);
}

.profile-form button {
  align-self: flex-start;
  margin-top: 6px;
  background: var(--shopee-orange);
  color: #fff;
  border: none;
  border-radius: var(--radius-sm);
  padding: 10px 24px;
  font-size: 14px;
}

.profile-form button:disabled {
  opacity: 0.6;
}

.error {
  color: var(--shopee-orange-dark);
  font-size: 13px;
}

.success {
  color: #2e7d32;
  font-size: 13px;
}

.state-message {
  color: var(--text-secondary);
  font-size: 13px;
}
</style>
