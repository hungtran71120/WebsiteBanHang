<script setup lang="ts">
import { onMounted, reactive, ref, watch } from 'vue'
import { getAllOrders, updateOrderStatus } from '../../api/orders'
import type { AdminOrderSummary } from '../../types/admin'
import type { OrderStatus } from '../../types/order'

const orders = ref<AdminOrderSummary[]>([])
const page = ref(1)
const totalPages = ref(1)
const isLoading = ref(true)
const errorMessage = ref('')
const statusFilter = ref<OrderStatus | ''>('')
const pendingStatusById = reactive<Record<string, OrderStatus>>({})
const updatingId = ref<string | null>(null)
const pageSize = 10

const statusLabels: Record<OrderStatus, string> = {
  Pending: 'Chờ xác nhận',
  Confirmed: 'Đã xác nhận',
  Shipped: 'Đang giao',
  Delivered: 'Đã giao',
  Cancelled: 'Đã hủy',
}

const statusBadgeClass: Record<OrderStatus, string> = {
  Pending: 'status-badge--pending',
  Confirmed: 'status-badge--confirmed',
  Shipped: 'status-badge--shipped',
  Delivered: 'status-badge--delivered',
  Cancelled: 'status-badge--cancelled',
}

const allowedTransitions: Record<OrderStatus, OrderStatus[]> = {
  Pending: ['Confirmed', 'Cancelled'],
  Confirmed: ['Shipped', 'Cancelled'],
  Shipped: [],
  Delivered: [],
  Cancelled: [],
}

async function load() {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const result = await getAllOrders(page.value, pageSize, statusFilter.value || undefined)
    orders.value = result.items
    totalPages.value = result.totalPages || 1
    for (const order of result.items) {
      pendingStatusById[order.id] = order.status
    }
  } catch {
    errorMessage.value = 'Không thể tải danh sách đơn hàng.'
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

async function applyStatusChange(order: AdminOrderSummary) {
  const newStatus = pendingStatusById[order.id]
  if (!newStatus || newStatus === order.status) {
    return
  }
  updatingId.value = order.id
  try {
    await updateOrderStatus(order.id, newStatus)
    await load()
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể cập nhật trạng thái.')
    pendingStatusById[order.id] = order.status
  } finally {
    updatingId.value = null
  }
}

watch(statusFilter, () => {
  page.value = 1
  load()
})

onMounted(load)
</script>

<template>
  <div class="admin-page">
    <div class="admin-page__header">
      <h1>Đơn hàng</h1>
    </div>

    <div class="admin-filters">
      <select v-model="statusFilter">
        <option value="">Tất cả trạng thái</option>
        <option v-for="(label, status) in statusLabels" :key="status" :value="status">{{ label }}</option>
      </select>
    </div>

    <p v-if="isLoading" class="state-message">Đang tải...</p>
    <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>
    <template v-else>
      <table class="admin-table">
        <thead>
          <tr>
            <th>Mã đơn</th>
            <th>Khách hàng</th>
            <th>Trạng thái</th>
            <th>Tổng tiền</th>
            <th>Ngày tạo</th>
            <th>Đổi trạng thái</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="order in orders" :key="order.id">
            <td>{{ order.id.slice(0, 8).toUpperCase() }}</td>
            <td>{{ order.customerEmail }}</td>
            <td><span class="status-badge" :class="statusBadgeClass[order.status]">{{ statusLabels[order.status] }}</span></td>
            <td>₫{{ order.totalAmount.toLocaleString('vi-VN') }}</td>
            <td>{{ new Date(order.createdAt).toLocaleDateString('vi-VN') }}</td>
            <td class="admin-table__actions">
              <select v-model="pendingStatusById[order.id]" :disabled="allowedTransitions[order.status].length === 0">
                <option :value="order.status">{{ statusLabels[order.status] }}</option>
                <option v-for="next in allowedTransitions[order.status]" :key="next" :value="next">
                  {{ statusLabels[next] }}
                </option>
              </select>
              <button
                type="button"
                class="btn-link"
                :disabled="pendingStatusById[order.id] === order.status || updatingId === order.id"
                @click="applyStatusChange(order)"
              >
                Cập nhật
              </button>
              <p v-if="order.status === 'Shipped'" class="admin-table__hint">
                Chờ khách xác nhận hoặc tự động hoàn thành sau 7 ngày
              </p>
            </td>
          </tr>
          <tr v-if="orders.length === 0">
            <td colspan="6" class="state-message">Chưa có đơn hàng nào.</td>
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
