<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { confirmDelivery as confirmDeliveryApi, getOrderById } from '../../api/orders'
import AppIcon from '../../components/icons/AppIcon.vue'
import { resolveImageUrl } from '../../utils/url'
import { useAuthStore } from '../../stores/auth'
import { useChatStore } from '../../stores/chat'
import type { Order, OrderStatus } from '../../types/order'

const route = useRoute()
const authStore = useAuthStore()
const chatStore = useChatStore()
const order = ref<Order | null>(null)
const isLoading = ref(true)
const errorMessage = ref('')

const headerStatusLabels: Record<OrderStatus, string> = {
  Pending: 'CHỜ XÁC NHẬN',
  Confirmed: 'ĐÃ XÁC NHẬN',
  Shipped: 'ĐANG VẬN CHUYỂN',
  Delivered: 'ĐƠN HÀNG ĐÃ HOÀN THÀNH',
  Cancelled: 'ĐƠN HÀNG ĐÃ HỦY',
}

const paymentLabels: Record<string, string> = {
  Cod: 'Thanh toán khi nhận hàng (COD)',
  MockPaid: 'Đã thanh toán (mô phỏng)',
}

const stageDefinitions: { status: OrderStatus; label: string; icon: 'box' | 'check' | 'truck' | 'home' }[] = [
  { status: 'Pending', label: 'Đơn Hàng Đã Đặt', icon: 'box' },
  { status: 'Confirmed', label: 'Đơn Hàng Đã Xác Nhận', icon: 'check' },
  { status: 'Shipped', label: 'Đang Vận Chuyển', icon: 'truck' },
  { status: 'Delivered', label: 'Đã Nhận Được Hàng', icon: 'home' },
]

const timestampByStatus = computed(() => {
  const map: Partial<Record<OrderStatus, string>> = {}
  for (const entry of order.value?.statusHistory ?? []) {
    if (!map[entry.status]) {
      map[entry.status] = entry.createdAt
    }
  }
  return map
})

const currentStageIndex = computed(() => {
  if (!order.value) {
    return -1
  }
  return stageDefinitions.findIndex((s) => s.status === order.value!.status)
})

const steps = computed(() =>
  stageDefinitions.map((stage, index) => ({
    ...stage,
    timestamp: timestampByStatus.value[stage.status] ?? null,
    reached: currentStageIndex.value >= index,
  })),
)

function formatDateTime(value: string) {
  const date = new Date(value)
  const pad = (n: number) => n.toString().padStart(2, '0')
  return `${pad(date.getHours())}:${pad(date.getMinutes())} ${pad(date.getDate())}-${pad(date.getMonth() + 1)}-${date.getFullYear()}`
}

function contactSeller() {
  chatStore.openWidget()
}

const isConfirming = ref(false)

async function confirmDelivery() {
  if (!order.value) {
    return
  }
  isConfirming.value = true
  try {
    order.value = await confirmDeliveryApi(order.value.id)
  } catch {
    alert('Không thể xác nhận đã nhận hàng. Vui lòng thử lại.')
  } finally {
    isConfirming.value = false
  }
}

onMounted(async () => {
  try {
    order.value = await getOrderById(route.params.id as string)
  } catch {
    errorMessage.value = 'Không tìm thấy đơn hàng.'
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <main class="order-detail-page">
    <div class="order-detail-page__inner">
      <div v-if="isLoading">
        <div class="skeleton-block skeleton-line skeleton-line--breadcrumb"></div>
        <div class="card status-card">
          <div class="skeleton-block skeleton-line skeleton-line--status"></div>
          <div class="skeleton-block skeleton-line skeleton-line--date"></div>
        </div>
        <div class="card">
          <div class="skeleton-block skeleton-line skeleton-line--heading"></div>
          <div class="skeleton-block skeleton-line skeleton-line--full"></div>
        </div>
        <div class="card">
          <div class="skeleton-block skeleton-line skeleton-line--heading"></div>
          <div class="order-line">
            <div class="skeleton-block skeleton-line skeleton-line--product"></div>
            <div class="skeleton-block skeleton-line skeleton-line--price"></div>
          </div>
        </div>
      </div>
      <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>

      <template v-else-if="order">
        <div class="detail-header">
          <RouterLink to="/orders" class="detail-header__back">‹ Trở Lại</RouterLink>
          <span class="detail-header__id">Mã Đơn Hàng: {{ order.id.slice(0, 8).toUpperCase() }}</span>
          <span class="detail-header__status" :class="{ cancelled: order.status === 'Cancelled' }">
            {{ headerStatusLabels[order.status] }}
          </span>
        </div>

        <div class="card stepper-card">
          <div v-if="order.status === 'Cancelled'" class="stepper">
            <div class="step done">
              <div class="step__icon"><AppIcon name="box" :size="18" /></div>
              <p class="step__label">Đơn Hàng Đã Đặt</p>
              <p v-if="timestampByStatus.Pending" class="step__time">{{ formatDateTime(timestampByStatus.Pending) }}</p>
            </div>
            <div class="step__line done cancelled"></div>
            <div class="step done cancelled">
              <div class="step__icon"><AppIcon name="x" :size="18" /></div>
              <p class="step__label">Đơn Hàng Đã Hủy</p>
              <p v-if="timestampByStatus.Cancelled" class="step__time">
                {{ formatDateTime(timestampByStatus.Cancelled) }}
              </p>
            </div>
          </div>

          <div v-else class="stepper">
            <template v-for="(step, index) in steps" :key="step.status">
              <div class="step" :class="{ done: step.reached }">
                <div class="step__icon"><AppIcon :name="step.icon" :size="18" /></div>
                <p class="step__label">{{ step.label }}</p>
                <p v-if="step.timestamp" class="step__time">{{ formatDateTime(step.timestamp) }}</p>
              </div>
              <div v-if="index < steps.length - 1" class="step__line" :class="{ done: steps[index + 1].reached }"></div>
            </template>
          </div>
        </div>

        <div v-if="order.status === 'Shipped'" class="thank-you-banner">
          <span>Đơn sẽ tự động chuyển sang "Đã giao" sau 7 ngày nếu bạn không xác nhận.</span>
          <div class="thank-you-banner__actions">
            <button type="button" class="btn-solid" :disabled="isConfirming" @click="confirmDelivery">
              {{ isConfirming ? 'Đang xác nhận...' : 'Đã Nhận Được Hàng' }}
            </button>
          </div>
        </div>

        <div v-if="order.status === 'Delivered'" class="thank-you-banner">
          <span>Cảm ơn bạn đã mua sắm tại Hưng Store!</span>
          <div class="thank-you-banner__actions">
            <button type="button" class="btn-outline" @click="contactSeller">Liên Hệ Người Bán</button>
            <RouterLink v-if="order.items[0]" :to="`/products/${order.items[0].productId}`" class="btn-solid">
              Mua Lại
            </RouterLink>
          </div>
        </div>

        <div class="ticket-divider"></div>

        <div class="card">
          <h2>Địa Chỉ Nhận Hàng</h2>
          <p class="recipient-line">
            {{ authStore.user?.fullName }}
            <span v-if="authStore.user?.phoneNumber"> | {{ authStore.user.phoneNumber }}</span>
          </p>
          <p>{{ order.shippingAddress }}</p>
        </div>

        <div class="card">
          <h2>Sản Phẩm</h2>
          <div v-for="item in order.items" :key="item.productId" class="order-line">
            <img
              v-if="resolveImageUrl(item.imageUrl)"
              :src="resolveImageUrl(item.imageUrl)!"
              :alt="item.productName"
              class="order-line__image"
            />
            <div v-else class="order-line__image order-line__placeholder">Không có ảnh</div>
            <span class="line-clamp-2 order-line__name">
              {{ item.productName }}<span v-if="item.variantDescription"> ({{ item.variantDescription }})</span> × {{ item.quantity }}
            </span>
            <span class="price">₫{{ item.lineTotal.toLocaleString('vi-VN') }}</span>
          </div>
        </div>

        <div class="card summary">
          <div class="summary-row">
            <span>Phương thức thanh toán</span>
            <span>{{ paymentLabels[order.paymentMethod] ?? order.paymentMethod }}</span>
          </div>
          <div class="summary-row">
            <span>Tạm tính</span>
            <span>₫{{ order.subtotal.toLocaleString('vi-VN') }}</span>
          </div>
          <div v-if="order.voucherCode" class="summary-row">
            <span>Giảm giá ({{ order.voucherCode }})</span>
            <span>−₫{{ order.discountAmount.toLocaleString('vi-VN') }}</span>
          </div>
          <div class="summary-row">
            <span>Tổng thanh toán</span>
            <span class="price total">₫{{ order.totalAmount.toLocaleString('vi-VN') }}</span>
          </div>
        </div>
      </template>
    </div>
  </main>
</template>

<style scoped>
.order-detail-page {
  background: var(--bg-page);
  padding: 16px 0 40px;
  min-height: 60vh;
}

.order-detail-page__inner {
  max-width: 800px;
  margin: 0 auto;
  padding: 0 16px;
}

.state-message {
  text-align: center;
  padding: 60px 0;
  color: var(--text-secondary);
}

.detail-header {
  display: flex;
  align-items: center;
  gap: 16px;
  background: #fff;
  padding: 16px 20px;
  margin-bottom: 1px;
  font-size: 13px;
}

.detail-header__back {
  color: var(--text);
  text-decoration: none;
  flex-shrink: 0;
}

.detail-header__id {
  flex: 1;
  color: var(--text);
}

.detail-header__status {
  color: var(--shopee-orange);
  font-weight: 600;
}

.detail-header__status.cancelled {
  color: var(--text-secondary);
}

.card {
  background: #fff;
  padding: 20px;
  margin-bottom: 12px;
}

.card h2 {
  font-size: 14px;
  margin-bottom: 8px;
}

.stepper-card {
  overflow-x: auto;
}

.stepper {
  display: flex;
  align-items: flex-start;
}

.step {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex: 0 0 140px;
  text-align: center;
  color: var(--text-secondary);
}

.step__icon {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  border: 2px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 8px;
  background: var(--surface);
  color: var(--text-secondary);
}

.step.done {
  color: var(--text);
}

.step.done .step__icon {
  border-color: #1ba94c;
  color: #1ba94c;
}

.step.done.cancelled .step__icon {
  border-color: var(--shopee-orange-dark);
  color: var(--shopee-orange-dark);
}

.step__label {
  font-size: 13px;
  font-weight: 600;
}

.step__time {
  font-size: 11.5px;
  color: var(--text-secondary);
  margin-top: 2px;
}

.step__line {
  flex: 1 1 auto;
  height: 2px;
  background: var(--border);
  margin-top: 22px;
}

.step__line.done {
  background: #1ba94c;
}

.step__line.done.cancelled {
  background: var(--shopee-orange-dark);
}

.thank-you-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  background: #fffaf0;
  padding: 16px 20px;
  font-size: 14px;
  flex-wrap: wrap;
}

.thank-you-banner__actions {
  display: flex;
  gap: 8px;
  flex-shrink: 0;
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

.ticket-divider {
  height: 3px;
  margin-bottom: 12px;
  background: repeating-linear-gradient(
    90deg,
    var(--shopee-orange) 0 12px,
    #4096ff 12px 24px
  );
}

.recipient-line {
  font-weight: 600;
  margin-bottom: 4px;
}

.order-line {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 6px 0;
  font-size: 13px;
}

.order-line__image {
  width: 44px;
  height: 44px;
  object-fit: cover;
  flex-shrink: 0;
  background: var(--bg-page);
}

.order-line__placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.55rem;
  color: #aaa;
  text-align: center;
}

.order-line__name {
  flex: 1;
}

.price {
  color: var(--shopee-orange);
  font-weight: 600;
}

.summary-row {
  display: flex;
  justify-content: space-between;
  padding: 4px 0;
  font-size: 14px;
}

.summary-row .total {
  font-size: 20px;
}

.skeleton-line {
  height: 12px;
  margin-bottom: 8px;
}

.skeleton-line--breadcrumb {
  width: 200px;
  height: 14px;
  margin-bottom: 12px;
}

.skeleton-line--status {
  width: 30%;
  height: 15px;
}

.skeleton-line--date {
  width: 25%;
}

.skeleton-line--heading {
  width: 35%;
  height: 14px;
}

.skeleton-line--full {
  width: 90%;
  margin-bottom: 0;
}

.skeleton-line--product {
  width: 60%;
  margin-bottom: 0;
}

.skeleton-line--price {
  width: 18%;
  margin-bottom: 0;
}
</style>
