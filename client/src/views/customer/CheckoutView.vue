<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { createOrder } from '../../api/orders'
import { getAvailableVouchers, validateVoucher } from '../../api/vouchers'
import { useAuthStore } from '../../stores/auth'
import { useCartStore } from '../../stores/cart'
import type { PaymentMethod } from '../../types/order'
import type { Voucher, VoucherValidationResult } from '../../types/voucher'

const router = useRouter()
const authStore = useAuthStore()
const cartStore = useCartStore()

const shippingAddress = ref(authStore.user?.address ?? '')
const paymentMethod = ref<PaymentMethod>('Cod')
const isLoading = ref(true)
const isSubmitting = ref(false)
const errorMessage = ref('')

const voucherCodeInput = ref('')
const appliedVoucher = ref<VoucherValidationResult | null>(null)
const voucherError = ref('')
const isApplyingVoucher = ref(false)
const availableVouchers = ref<Voucher[]>([])
const isVoucherModalOpen = ref(false)

const finalTotal = computed(() => appliedVoucher.value?.finalTotal ?? cartStore.cart?.totalAmount ?? 0)

function isVoucherEligible(voucher: Voucher) {
  const subtotal = cartStore.cart?.totalAmount ?? 0
  if (subtotal < voucher.minOrderAmount) {
    return false
  }
  return !(voucher.maxUsageCount !== null && voucher.usedCount >= voucher.maxUsageCount)
}

function describeDiscount(voucher: Voucher) {
  return voucher.discountType === 'Percentage'
    ? `Giảm ${voucher.discountValue}%`
    : `Giảm ₫${voucher.discountValue.toLocaleString('vi-VN')}`
}

function openVoucherModal() {
  voucherError.value = ''
  isVoucherModalOpen.value = true
}

function closeVoucherModal() {
  isVoucherModalOpen.value = false
}

async function applyVoucher(code?: string) {
  const codeToApply = (code ?? voucherCodeInput.value).trim()
  if (!codeToApply) {
    return
  }
  voucherError.value = ''
  isApplyingVoucher.value = true
  try {
    appliedVoucher.value = await validateVoucher(codeToApply)
    voucherCodeInput.value = codeToApply
    isVoucherModalOpen.value = false
  } catch (error: unknown) {
    appliedVoucher.value = null
    const messages = (error as { response?: { data?: string[] } })?.response?.data
    voucherError.value = messages?.[0] ?? 'Mã giảm giá không hợp lệ.'
  } finally {
    isApplyingVoucher.value = false
  }
}

function removeVoucher() {
  appliedVoucher.value = null
  voucherCodeInput.value = ''
  voucherError.value = ''
}

onMounted(async () => {
  try {
    await cartStore.fetchCart()
    if (!cartStore.cart || cartStore.cart.items.length === 0) {
      router.replace('/cart')
    }
  } finally {
    isLoading.value = false
  }

  try {
    availableVouchers.value = await getAvailableVouchers()
  } catch {
    availableVouchers.value = []
  }
})

async function placeOrder() {
  errorMessage.value = ''
  if (!shippingAddress.value.trim()) {
    errorMessage.value = 'Vui lòng nhập địa chỉ giao hàng.'
    return
  }

  isSubmitting.value = true
  try {
    const order = await createOrder({
      shippingAddress: shippingAddress.value,
      paymentMethod: paymentMethod.value,
      voucherCode: appliedVoucher.value?.code ?? null,
    })
    cartStore.reset()
    router.push(`/orders/${order.id}`)
  } catch (error: unknown) {
    const messages = (error as { response?: { data?: string[] } })?.response?.data
    errorMessage.value = messages?.[0] ?? 'Đặt hàng thất bại. Vui lòng thử lại.'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <main class="checkout-page">
    <div class="checkout-page__inner">
      <h1>Thanh Toán</h1>
      <div v-if="isLoading">
        <section class="card">
          <div class="skeleton-block skeleton-line skeleton-line--heading"></div>
          <div class="skeleton-block skeleton-line skeleton-line--textarea"></div>
        </section>
        <section class="card">
          <div class="skeleton-block skeleton-line skeleton-line--heading"></div>
          <div class="order-line">
            <div class="skeleton-block skeleton-line skeleton-line--product"></div>
            <div class="skeleton-block skeleton-line skeleton-line--price"></div>
          </div>
        </section>
        <section class="card summary">
          <div class="skeleton-block skeleton-line skeleton-line--total"></div>
        </section>
      </div>

      <template v-else-if="cartStore.cart">
        <section class="card">
          <h2>Địa Chỉ Giao Hàng</h2>
          <textarea v-model="shippingAddress" rows="2" placeholder="Số nhà, đường, phường/xã, quận/huyện, tỉnh/thành"></textarea>
        </section>

        <section class="card">
          <h2>Sản Phẩm</h2>
          <div v-for="item in cartStore.cart.items" :key="item.id" class="order-line">
            <span class="line-clamp-2">
              {{ item.productName }}<span v-if="item.variantDescription"> ({{ item.variantDescription }})</span> × {{ item.quantity }}
            </span>
            <span class="price">₫{{ item.lineTotal.toLocaleString('vi-VN') }}</span>
          </div>
        </section>

        <section class="card">
          <h2>Phương Thức Thanh Toán</h2>
          <label class="radio">
            <input v-model="paymentMethod" type="radio" value="Cod" />
            Thanh toán khi nhận hàng (COD)
          </label>
          <label class="radio">
            <input v-model="paymentMethod" type="radio" value="MockPaid" />
            Đã thanh toán (mô phỏng)
          </label>
        </section>

        <section class="card">
          <h2>Mã Giảm Giá</h2>
          <div v-if="!appliedVoucher" class="voucher-input">
            <input v-model="voucherCodeInput" type="text" placeholder="Nhập mã giảm giá" @keyup.enter="applyVoucher()" />
            <button type="button" :disabled="isApplyingVoucher" @click="applyVoucher()">
              {{ isApplyingVoucher ? 'Đang áp dụng...' : 'Áp Dụng' }}
            </button>
            <button type="button" class="choose-voucher-btn" @click="openVoucherModal">Chọn voucher</button>
          </div>
          <div v-else class="voucher-applied">
            <span>Đã áp dụng mã <strong>{{ appliedVoucher.code }}</strong></span>
            <button type="button" class="remove-voucher-btn" @click="removeVoucher">Hủy</button>
          </div>
          <p v-if="voucherError" class="error">{{ voucherError }}</p>
        </section>

        <section class="card summary-breakdown">
          <div class="summary-row">
            <span>Tạm tính:</span>
            <span>₫{{ cartStore.cart.totalAmount.toLocaleString('vi-VN') }}</span>
          </div>
          <div v-if="appliedVoucher" class="summary-row discount">
            <span>Giảm giá:</span>
            <span>−₫{{ appliedVoucher.discountAmount.toLocaleString('vi-VN') }}</span>
          </div>
          <div class="summary-row summary-row--total">
            <span>Tổng thanh toán:</span>
            <span class="price total">₫{{ finalTotal.toLocaleString('vi-VN') }}</span>
          </div>
        </section>

        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <button class="place-order-btn" :disabled="isSubmitting" @click="placeOrder">
          {{ isSubmitting ? 'Đang đặt hàng...' : 'Đặt Hàng' }}
        </button>
      </template>
    </div>

    <div v-if="isVoucherModalOpen" class="voucher-modal-overlay" @click.self="closeVoucherModal">
      <div class="voucher-modal-box">
        <h2>Chọn Voucher</h2>
        <p v-if="voucherError" class="error">{{ voucherError }}</p>
        <ul v-if="availableVouchers.length > 0" class="voucher-list">
          <li
            v-for="voucher in availableVouchers"
            :key="voucher.id"
            class="voucher-list-item"
            :class="{ 'voucher-list-item--disabled': !isVoucherEligible(voucher) }"
          >
            <div class="voucher-list-item__info">
              <strong>{{ voucher.code }}</strong>
              <span>{{ describeDiscount(voucher) }}</span>
              <span class="voucher-list-item__meta">
                Đơn tối thiểu ₫{{ voucher.minOrderAmount.toLocaleString('vi-VN') }} · HSD {{ new Date(voucher.expiresAt).toLocaleDateString('vi-VN') }}
              </span>
            </div>
            <button
              type="button"
              :disabled="!isVoucherEligible(voucher) || isApplyingVoucher"
              @click="applyVoucher(voucher.code)"
            >
              Áp dụng
            </button>
          </li>
        </ul>
        <p v-else class="state-message">Hiện chưa có voucher nào khả dụng.</p>

        <div class="voucher-modal-actions">
          <button type="button" class="voucher-modal-close-btn" @click="closeVoucherModal">Đóng</button>
        </div>
      </div>
    </div>
  </main>
</template>

<style scoped>
.checkout-page {
  background: var(--bg-page);
  padding: 16px 0 40px;
  min-height: 60vh;
}

.checkout-page__inner {
  max-width: 800px;
  margin: 0 auto;
  padding: 0 16px;
}

h1 {
  font-size: 20px;
  margin-bottom: 16px;
}

.state-message {
  background: #fff;
  padding: 60px;
  text-align: center;
  color: var(--text-secondary);
}

.card {
  background: #fff;
  padding: 20px;
  margin-bottom: 12px;
}

.card h2 {
  font-size: 14px;
  margin-bottom: 12px;
}

textarea {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  resize: vertical;
}

.order-line {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  padding: 6px 0;
  font-size: 13px;
}

.radio {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 0;
  font-size: 14px;
}

.price {
  color: var(--shopee-orange);
  font-weight: 600;
}

.summary {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 12px;
  font-size: 14px;
}

.summary .total {
  font-size: 22px;
}

.voucher-input {
  display: flex;
  gap: 8px;
}

.voucher-input input {
  flex: 1;
  padding: 10px 12px;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
}

.voucher-input button {
  background: var(--shopee-orange);
  color: #fff;
  border: none;
  padding: 0 16px;
  font-size: 13px;
  border-radius: var(--radius-sm);
}

.voucher-input button:disabled {
  opacity: 0.6;
}

.choose-voucher-btn {
  background: #fff;
  color: var(--shopee-orange);
  border: 1px solid var(--shopee-orange);
  padding: 0 16px;
  font-size: 13px;
  border-radius: var(--radius-sm);
}

.voucher-modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
  z-index: 100;
}

.voucher-modal-box {
  background: #fff;
  border-radius: var(--radius-sm);
  padding: 20px;
  width: 480px;
  max-width: 100%;
  max-height: 80vh;
  overflow-y: auto;
}

.voucher-modal-box h2 {
  font-size: 16px;
  margin-bottom: 12px;
}

.voucher-list {
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 16px;
}

.voucher-list-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  border: 1px dashed var(--shopee-orange);
  border-radius: var(--radius-sm);
  padding: 10px 12px;
}

.voucher-list-item--disabled {
  opacity: 0.5;
  border-color: var(--border);
}

.voucher-list-item__info {
  display: flex;
  flex-direction: column;
  gap: 2px;
  font-size: 13px;
}

.voucher-list-item__meta {
  color: var(--text-secondary);
  font-size: 12px;
}

.voucher-list-item button {
  background: var(--shopee-orange);
  color: #fff;
  border: none;
  padding: 6px 14px;
  font-size: 13px;
  border-radius: var(--radius-sm);
  white-space: nowrap;
}

.voucher-list-item button:disabled {
  opacity: 0.6;
}

.voucher-modal-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

.voucher-modal-close-btn {
  background: none;
  border: 1px solid var(--border);
  padding: 8px 16px;
  border-radius: var(--radius-sm);
  font-size: 13px;
}

.voucher-applied {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 14px;
}

.remove-voucher-btn {
  background: none;
  border: none;
  color: var(--text-secondary);
  font-size: 13px;
  cursor: pointer;
}

.remove-voucher-btn:hover {
  color: var(--shopee-orange);
}

.summary-breakdown {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 14px;
}

.summary-row {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

.summary-row.discount {
  color: var(--shopee-orange);
}

.summary-row--total {
  font-weight: 600;
  border-top: 1px solid var(--border);
  padding-top: 8px;
  margin-top: 2px;
}

.error {
  color: var(--shopee-orange-dark);
  font-size: 13px;
  margin-bottom: 8px;
  text-align: right;
}

.place-order-btn {
  display: block;
  margin-left: auto;
  background: var(--shopee-orange);
  color: #fff;
  border: none;
  padding: 12px 40px;
  font-size: 15px;
  border-radius: var(--radius-sm);
}

.place-order-btn:disabled {
  opacity: 0.6;
}

.skeleton-line {
  height: 12px;
  margin-bottom: 8px;
}

.skeleton-line--heading {
  width: 30%;
  height: 14px;
}

.skeleton-line--textarea {
  width: 100%;
  height: 48px;
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

.skeleton-line--total {
  width: 25%;
  height: 22px;
  margin-bottom: 0;
  margin-left: auto;
}

@media (max-width: 480px) {
  .card {
    padding: 16px;
  }

  .voucher-input {
    flex-direction: column;
  }

  .voucher-input button {
    padding: 10px 0;
  }

  .place-order-btn {
    width: 100%;
  }
}
</style>
