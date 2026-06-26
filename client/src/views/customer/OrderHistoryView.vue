<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import AccountSidebar from '../../components/AccountSidebar.vue'
import AppIcon from '../../components/icons/AppIcon.vue'
import { confirmDelivery as confirmDeliveryApi, getMyOrders } from '../../api/orders'
import { resolveImageUrl } from '../../utils/url'
import { useChatStore } from '../../stores/chat'
import type { OrderStatus, OrderSummary } from '../../types/order'

const chatStore = useChatStore()

const orders = ref<OrderSummary[]>([])
const page = ref(1)
const totalPages = ref(1)
const isLoading = ref(true)
const errorMessage = ref('')
const keyword = ref('')
const activeStatus = ref<OrderStatus | 'All'>('All')
const pageSize = 10

const statusLabels: Record<OrderStatus, string> = {
  Pending: 'Chờ xác nhận',
  Confirmed: 'Đã xác nhận',
  Shipped: 'Đang giao',
  Delivered: 'Đã giao',
  Cancelled: 'Đã hủy',
}

const tabs: { label: string; value: OrderStatus | 'All' }[] = [
  { label: 'Tất cả', value: 'All' },
  { label: 'Chờ xác nhận', value: 'Pending' },
  { label: 'Đã xác nhận', value: 'Confirmed' },
  { label: 'Đang giao', value: 'Shipped' },
  { label: 'Đã giao', value: 'Delivered' },
  { label: 'Đã hủy', value: 'Cancelled' },
]

const filteredOrders = computed(() => {
  const kw = keyword.value.trim().toLowerCase()
  if (!kw) {
    return orders.value
  }
  return orders.value.filter(
    (order) =>
      order.id.toLowerCase().includes(kw) ||
      order.items.some((item) => item.productName.toLowerCase().includes(kw)),
  )
})

async function load() {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const status = activeStatus.value === 'All' ? undefined : activeStatus.value
    const result = await getMyOrders(page.value, pageSize, status)
    orders.value = result.items
    totalPages.value = result.totalPages || 1
  } catch {
    errorMessage.value = 'Không thể tải lịch sử đơn hàng.'
  } finally {
    isLoading.value = false
  }
}

function setStatus(status: OrderStatus | 'All') {
  if (activeStatus.value === status) {
    return
  }
  activeStatus.value = status
  page.value = 1
  load()
}

function goToPage(target: number) {
  if (target < 1 || target > totalPages.value) {
    return
  }
  page.value = target
  load()
}

function contactSeller() {
  chatStore.openWidget()
}

const confirmingId = ref<string | null>(null)

async function confirmDelivery(order: OrderSummary) {
  confirmingId.value = order.id
  try {
    await confirmDeliveryApi(order.id)
    await load()
  } catch {
    alert('Không thể xác nhận đã nhận hàng. Vui lòng thử lại.')
  } finally {
    confirmingId.value = null
  }
}

onMounted(load)
</script>

<template>
  <main class="order-history-page">
    <div class="order-history-page__inner">
      <AccountSidebar />

      <section class="order-history-content">
        <div class="order-tabs">
          <button
            v-for="tab in tabs"
            :key="tab.value"
            type="button"
            :class="['order-tabs__item', { active: activeStatus === tab.value }]"
            @click="setStatus(tab.value)"
          >
            {{ tab.label }}
          </button>
        </div>

        <div class="order-search">
          <span class="order-search__icon"><AppIcon name="search" :size="16" /></span>
          <input v-model="keyword" type="text" placeholder="Tìm theo Mã đơn hàng hoặc Tên sản phẩm" />
        </div>

        <div v-if="isLoading">
          <div v-for="n in 3" :key="n" class="order-card">
            <div class="order-card__header">
              <div class="skeleton-block skeleton-line skeleton-line--id"></div>
              <div class="skeleton-block skeleton-line skeleton-line--status"></div>
            </div>
            <div class="order-card__body">
              <div class="skeleton-block skeleton-line skeleton-line--meta"></div>
              <div class="skeleton-block skeleton-line skeleton-line--price"></div>
            </div>
          </div>
        </div>
        <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>
        <p v-else-if="filteredOrders.length === 0" class="state-message">
          Bạn chưa có đơn hàng nào.
          <RouterLink to="/products">Mua sắm ngay</RouterLink>
        </p>

        <template v-else>
          <div v-for="order in filteredOrders" :key="order.id" class="order-card">
            <div class="order-card__header">
              <span class="order-card__id">Mã đơn: {{ order.id.slice(0, 8).toUpperCase() }}</span>
              <span class="order-card__status">{{ statusLabels[order.status] ?? order.status }}</span>
            </div>

            <RouterLink :to="`/orders/${order.id}`" class="order-card__items">
              <div v-for="(item, index) in order.items" :key="`${item.productId}-${index}`" class="order-item">
                <img
                  v-if="resolveImageUrl(item.imageUrl)"
                  :src="resolveImageUrl(item.imageUrl)!"
                  :alt="item.productName"
                />
                <div v-else class="order-item__placeholder">Không có ảnh</div>
                <div class="order-item__info">
                  <p class="order-item__name line-clamp-2">{{ item.productName }}</p>
                  <p v-if="item.variantDescription" class="order-item__variant">
                    Phân loại: {{ item.variantDescription }}
                  </p>
                  <p class="order-item__qty">x{{ item.quantity }}</p>
                </div>
                <p class="order-item__price">₫{{ item.unitPrice.toLocaleString('vi-VN') }}</p>
              </div>
            </RouterLink>

            <p v-if="order.status === 'Shipped'" class="order-card__auto-note">
              Đơn sẽ tự động chuyển sang "Đã giao" sau 7 ngày nếu bạn không xác nhận.
            </p>

            <div class="order-card__footer">
              <span class="order-card__total">
                Thành tiền: <strong>₫{{ order.totalAmount.toLocaleString('vi-VN') }}</strong>
              </span>
              <div class="order-card__actions">
                <button type="button" class="btn-outline" @click="contactSeller">Liên Hệ Người Bán</button>
                <button
                  v-if="order.status === 'Shipped'"
                  type="button"
                  class="btn-solid"
                  :disabled="confirmingId === order.id"
                  @click="confirmDelivery(order)"
                >
                  {{ confirmingId === order.id ? 'Đang xác nhận...' : 'Đã Nhận Được Hàng' }}
                </button>
                <RouterLink v-if="order.items[0]" :to="`/products/${order.items[0].productId}`" class="btn-solid">
                  Mua Lại
                </RouterLink>
              </div>
            </div>
          </div>

          <div v-if="totalPages > 1" class="pagination">
            <button :disabled="page === 1" @click="goToPage(page - 1)">‹</button>
            <span>{{ page }}/{{ totalPages }}</span>
            <button :disabled="page === totalPages" @click="goToPage(page + 1)">›</button>
          </div>
        </template>
      </section>
    </div>
  </main>
</template>

<style scoped>
.order-history-page {
  background: var(--bg-page);
  padding: 16px 0 40px;
  min-height: 60vh;
}

.order-history-page__inner {
  max-width: 1000px;
  margin: 0 auto;
  padding: 0 16px;
  display: flex;
  gap: 16px;
  align-items: flex-start;
}

@media (max-width: 768px) {
  .order-history-page__inner {
    flex-direction: column;
  }
}

.order-history-content {
  flex: 1;
  min-width: 0;
}

.order-tabs {
  display: flex;
  background: #fff;
  border-bottom: 1px solid var(--border);
  overflow-x: auto;
}

.order-tabs__item {
  flex: 0 0 auto;
  padding: 14px 20px;
  font-size: 14px;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  color: var(--text);
  cursor: pointer;
  white-space: nowrap;
}

.order-tabs__item.active {
  color: var(--shopee-orange);
  border-bottom-color: var(--shopee-orange);
  font-weight: 600;
}

.order-search {
  display: flex;
  align-items: center;
  gap: 8px;
  background: var(--surface);
  padding: 12px 16px;
  margin-bottom: 12px;
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
}

.order-search__icon {
  color: var(--text-secondary);
  display: flex;
}

.order-search input {
  flex: 1;
  border: none;
  outline: none;
  font-size: 13px;
  background: none;
}

.state-message {
  background: #fff;
  padding: 60px;
  text-align: center;
  color: var(--text-secondary);
}

.state-message a {
  color: var(--shopee-orange);
  text-decoration: none;
}

.order-card {
  display: block;
  background: #fff;
  margin-bottom: 12px;
}

.order-card__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  font-size: 13px;
  color: var(--text-secondary);
  border-bottom: 1px solid var(--border);
}

.order-card__status {
  color: var(--shopee-orange);
  font-weight: 600;
}

.order-card__body {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px;
}

.order-card__items {
  display: block;
  text-decoration: none;
  color: inherit;
}

.order-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
}

.order-item + .order-item {
  border-top: 1px solid var(--border);
}

.order-item img,
.order-item__placeholder {
  width: 56px;
  height: 56px;
  object-fit: cover;
  background: var(--bg-page);
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.6rem;
  color: #aaa;
  text-align: center;
}

.order-item__info {
  flex: 1;
  min-width: 0;
}

.order-item__name {
  font-size: 13.5px;
  margin-bottom: 4px;
}

.order-item__variant,
.order-item__qty {
  font-size: 12px;
  color: var(--text-secondary);
}

.order-item__price {
  font-size: 13.5px;
  flex-shrink: 0;
}

.order-card__auto-note {
  padding: 8px 16px 0;
  font-size: 12px;
  color: var(--text-secondary);
  text-align: right;
}

.order-card__footer {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  border-top: 1px solid var(--border);
  flex-wrap: wrap;
}

.order-card__total {
  font-size: 13px;
}

.order-card__total strong {
  color: var(--shopee-orange);
  font-size: 16px;
}

.order-card__actions {
  display: flex;
  gap: 8px;
}

.btn-outline,
.btn-solid {
  display: inline-flex;
  align-items: center;
  padding: 8px 16px;
  font-size: 13px;
  border-radius: var(--radius-sm);
  text-decoration: none;
  cursor: pointer;
}

.btn-outline {
  background: #fff;
  border: 1px solid var(--border);
  color: var(--text);
}

.btn-solid {
  background: var(--shopee-orange);
  border: 1px solid var(--shopee-orange);
  color: #fff;
}

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 12px;
  margin-top: 16px;
}

.pagination button {
  width: 32px;
  height: 32px;
  border: 1px solid var(--border);
  background: #fff;
  border-radius: var(--radius-sm);
}

.pagination button:disabled {
  opacity: 0.4;
}

.skeleton-line {
  height: 12px;
}

.skeleton-line--id {
  width: 35%;
}

.skeleton-line--status {
  width: 20%;
}

.skeleton-line--meta {
  width: 45%;
}

.skeleton-line--price {
  width: 25%;
  height: 16px;
}
</style>
