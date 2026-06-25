<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getDashboardStatistics } from '../../api/statistics'
import type { DashboardStatistics } from '../../types/admin'

const stats = ref<DashboardStatistics | null>(null)
const isLoading = ref(true)
const errorMessage = ref('')

const statusLabels: Record<string, string> = {
  Pending: 'Chờ xác nhận',
  Confirmed: 'Đã xác nhận',
  Shipped: 'Đang giao',
  Delivered: 'Đã giao',
  Cancelled: 'Đã hủy',
}

async function load() {
  isLoading.value = true
  errorMessage.value = ''
  try {
    stats.value = await getDashboardStatistics()
  } catch {
    errorMessage.value = 'Không thể tải số liệu thống kê.'
  } finally {
    isLoading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="admin-page">
    <div class="admin-page__header">
      <h1>Thống kê</h1>
    </div>

    <p v-if="isLoading" class="state-message">Đang tải...</p>
    <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>
    <template v-else-if="stats">
      <div class="admin-card-grid">
        <div class="admin-stat-card">
          <div class="admin-stat-card__label">Tổng doanh thu</div>
          <div class="admin-stat-card__value admin-stat-card__value--orange">
            ₫{{ stats.totalRevenue.toLocaleString('vi-VN') }}
          </div>
        </div>
        <div class="admin-stat-card">
          <div class="admin-stat-card__label">Tổng số đơn hàng</div>
          <div class="admin-stat-card__value">{{ stats.totalOrders }}</div>
        </div>
        <div class="admin-stat-card">
          <div class="admin-stat-card__label">Tổng số sản phẩm</div>
          <div class="admin-stat-card__value">{{ stats.totalProducts }}</div>
        </div>
        <div class="admin-stat-card">
          <div class="admin-stat-card__label">Tổng số người dùng</div>
          <div class="admin-stat-card__value">{{ stats.totalUsers }}</div>
        </div>
      </div>

      <div class="admin-section">
        <h2>Số đơn hàng theo trạng thái</h2>
        <table class="admin-table">
          <thead>
            <tr>
              <th>Trạng thái</th>
              <th>Số đơn</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(count, status) in stats.ordersByStatus" :key="status">
              <td>{{ statusLabels[status] ?? status }}</td>
              <td>{{ count }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>
