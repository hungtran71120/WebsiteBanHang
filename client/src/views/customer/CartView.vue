<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useCartStore } from '../../stores/cart'
import { resolveImageUrl } from '../../utils/url'

const router = useRouter()
const cartStore = useCartStore()
const isLoading = ref(true)
const errorMessage = ref('')

onMounted(async () => {
  try {
    await cartStore.fetchCart()
  } catch {
    errorMessage.value = 'Không thể tải giỏ hàng.'
  } finally {
    isLoading.value = false
  }
})

async function changeQuantity(cartItemId: string, quantity: number) {
  if (quantity < 1) {
    return
  }
  try {
    await cartStore.updateItem(cartItemId, quantity)
  } catch {
    errorMessage.value = 'Số lượng vượt quá tồn kho.'
  }
}

async function removeItem(cartItemId: string) {
  await cartStore.removeItem(cartItemId)
}

function goToCheckout() {
  router.push('/checkout')
}
</script>

<template>
  <main class="cart-page">
    <div class="cart-page__inner">
      <h1>Giỏ Hàng</h1>

      <div v-if="isLoading" class="cart-table">
        <div class="cart-table__header">
          <span class="col-product">Sản phẩm</span>
          <span class="col-price">Đơn giá</span>
          <span class="col-qty">Số lượng</span>
          <span class="col-total">Số tiền</span>
          <span class="col-action"></span>
        </div>
        <div v-for="n in 3" :key="n" class="cart-row">
          <div class="col-product">
            <div class="skeleton-block skeleton-thumb"></div>
            <div class="skeleton-block skeleton-line skeleton-line--name"></div>
          </div>
          <div class="skeleton-block skeleton-line skeleton-line--short"></div>
          <div class="skeleton-block skeleton-line skeleton-line--short"></div>
          <div class="skeleton-block skeleton-line skeleton-line--short"></div>
          <span class="col-action"></span>
        </div>
      </div>
      <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>
      <p v-else-if="!cartStore.cart || cartStore.cart.items.length === 0" class="state-message">
        Giỏ hàng của bạn đang trống.
        <RouterLink to="/products">Tiếp tục mua sắm</RouterLink>
      </p>

      <template v-else>
        <div class="cart-table">
          <div class="cart-table__header">
            <span class="col-product">Sản phẩm</span>
            <span class="col-price">Đơn giá</span>
            <span class="col-qty">Số lượng</span>
            <span class="col-total">Số tiền</span>
            <span class="col-action"></span>
          </div>

          <div v-for="item in cartStore.cart.items" :key="item.id" class="cart-row">
            <div class="col-product">
              <img
                v-if="resolveImageUrl(item.productImageUrl)"
                :src="resolveImageUrl(item.productImageUrl)!"
                :alt="item.productName"
              />
              <div v-else class="placeholder">Không có ảnh</div>
              <span class="line-clamp-2">
                {{ item.productName }}
                <span v-if="item.variantDescription" class="color-tag">Phân loại: {{ item.variantDescription }}</span>
              </span>
            </div>
            <span class="col-price">₫{{ item.unitPrice.toLocaleString('vi-VN') }}</span>
            <span class="col-qty">
              <button @click="changeQuantity(item.id, item.quantity - 1)">−</button>
              <input
                type="number"
                min="1"
                :max="item.stock"
                :value="item.quantity"
                @change="changeQuantity(item.id, Number(($event.target as HTMLInputElement).value))"
              />
              <button @click="changeQuantity(item.id, item.quantity + 1)">+</button>
            </span>
            <span class="col-total price">₫{{ item.lineTotal.toLocaleString('vi-VN') }}</span>
            <span class="col-action">
              <button class="remove-btn" @click="removeItem(item.id)">Xóa</button>
            </span>
          </div>
        </div>

        <div class="cart-summary">
          <span>Tổng thanh toán ({{ cartStore.itemCount }} sản phẩm):</span>
          <span class="price">₫{{ cartStore.cart.totalAmount.toLocaleString('vi-VN') }}</span>
          <button class="checkout-btn" @click="goToCheckout">Mua Hàng</button>
        </div>
      </template>
    </div>
  </main>
</template>

<style scoped>
.cart-page {
  background: var(--bg-page);
  padding: 16px 0 40px;
  min-height: 60vh;
}

.cart-page__inner {
  max-width: 1100px;
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

.state-message a {
  color: var(--shopee-orange);
  text-decoration: none;
}

.cart-table {
  background: #fff;
}

.cart-table__header,
.cart-row {
  display: grid;
  grid-template-columns: 3fr 1fr 1.2fr 1fr 0.6fr;
  align-items: center;
  padding: 14px 20px;
  gap: 12px;
}

.cart-table__header {
  color: var(--text-secondary);
  font-size: 13px;
  border-bottom: 1px solid var(--border);
}

.cart-row {
  border-bottom: 1px solid var(--border);
}

.col-product {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 13px;
}

.color-tag {
  display: block;
  margin-top: 4px;
  color: var(--text-secondary);
  font-size: 12px;
}

.col-product img,
.col-product .placeholder {
  width: 60px;
  height: 60px;
  object-fit: cover;
  background: #f5f5f5;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 10px;
  color: #aaa;
}

.col-qty {
  display: flex;
  align-items: center;
  gap: 6px;
}

.col-qty button {
  width: 26px;
  height: 26px;
  border: 1px solid var(--border);
  background: #fff;
}

.col-qty input {
  width: 48px;
  height: 26px;
  text-align: center;
  border: 1px solid var(--border);
}

.price {
  color: var(--shopee-orange);
  font-weight: 600;
}

.remove-btn {
  border: none;
  background: none;
  color: var(--text-secondary);
  font-size: 13px;
}

.remove-btn:hover {
  color: var(--shopee-orange);
}

.cart-summary {
  background: #fff;
  margin-top: 12px;
  padding: 20px;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 16px;
  font-size: 14px;
}

.cart-summary .price {
  font-size: 22px;
}

.checkout-btn {
  background: var(--shopee-orange);
  color: #fff;
  border: none;
  padding: 12px 40px;
  font-size: 15px;
  border-radius: 2px;
}

.skeleton-thumb {
  width: 60px;
  height: 60px;
  flex-shrink: 0;
}

.skeleton-line {
  height: 12px;
}

.skeleton-line--name {
  width: 70%;
}

.skeleton-line--short {
  width: 50%;
}
</style>
