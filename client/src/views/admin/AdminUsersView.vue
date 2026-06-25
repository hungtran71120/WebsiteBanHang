<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { getAllUsers, setUserLockout } from '../../api/users'
import type { AdminUser } from '../../types/admin'
import { useAuthStore } from '../../stores/auth'

const authStore = useAuthStore()

const users = ref<AdminUser[]>([])
const page = ref(1)
const totalPages = ref(1)
const isLoading = ref(true)
const errorMessage = ref('')
const keyword = ref('')
const updatingId = ref<string | null>(null)
const pageSize = 10

let searchTimeout: ReturnType<typeof setTimeout> | undefined

async function load() {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const result = await getAllUsers(keyword.value || undefined, page.value, pageSize)
    users.value = result.items
    totalPages.value = result.totalPages || 1
  } catch {
    errorMessage.value = 'Không thể tải danh sách người dùng.'
  } finally {
    isLoading.value = false
  }
}

function goToPage(target: number) {
  if (target < 1 || target > totalPages.value) {
    return
  }
  page.value = target
  load()
}

async function toggleLock(user: AdminUser) {
  updatingId.value = user.id
  try {
    await setUserLockout(user.id, !user.isLocked)
    await load()
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể cập nhật trạng thái tài khoản.')
  } finally {
    updatingId.value = null
  }
}

watch(keyword, () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    page.value = 1
    load()
  }, 300)
})

onMounted(load)
</script>

<template>
  <div class="admin-page">
    <div class="admin-page__header">
      <h1>Người dùng</h1>
    </div>

    <div class="admin-filters">
      <input v-model="keyword" type="text" placeholder="Tìm theo email hoặc họ tên..." />
    </div>

    <p v-if="isLoading" class="state-message">Đang tải...</p>
    <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>
    <template v-else>
      <table class="admin-table">
        <thead>
          <tr>
            <th>Email</th>
            <th>Họ tên</th>
            <th>Vai trò</th>
            <th>Trạng thái</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="user in users" :key="user.id">
            <td>{{ user.email }}</td>
            <td>{{ user.fullName }}</td>
            <td>{{ user.role }}</td>
            <td>
              <span class="status-badge" :class="user.isLocked ? 'status-badge--cancelled' : 'status-badge--delivered'">
                {{ user.isLocked ? 'Đã khóa' : 'Hoạt động' }}
              </span>
            </td>
            <td class="admin-table__actions">
              <button
                type="button"
                class="btn-link"
                :class="{ 'btn-link--danger': !user.isLocked }"
                :disabled="user.id === authStore.user?.id || updatingId === user.id"
                @click="toggleLock(user)"
              >
                {{ user.isLocked ? 'Mở khóa' : 'Khóa' }}
              </button>
            </td>
          </tr>
          <tr v-if="users.length === 0">
            <td colspan="5" class="state-message">Không tìm thấy người dùng nào.</td>
          </tr>
        </tbody>
      </table>

      <div v-if="totalPages > 1" class="pagination">
        <button :disabled="page === 1" @click="goToPage(page - 1)">‹</button>
        <span>{{ page }}/{{ totalPages }}</span>
        <button :disabled="page === totalPages" @click="goToPage(page + 1)">›</button>
      </div>
    </template>
  </div>
</template>
