<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { useCartStore } from '../../stores/cart'
import { useWishlistStore } from '../../stores/wishlist'
import { resolveImageUrl } from '../../utils/url'

const wishlistStore = useWishlistStore()
const cartStore = useCartStore()
const isLoading = ref(true)
const errorMessage = ref('')
const addToCartMessage = ref('')

onMounted(async () => {
  try {
    await wishlistStore.fetchWishlist()
  } catch {
    errorMessage.value = 'Không thể tải danh sách yêu thích.'
  } finally {
    isLoading.value = false
  }
})

async function removeItem(productId: string) {
  await wishlistStore.toggle(productId)
}

async function addToCart(productId: string) {
  addToCartMessage.value = ''
  try {
    await cartStore.addItem(productId, 1)
    addToCartMessage.value = 'Đã thêm vào giỏ hàng.'
  } catch {
    addToCartMessage.value = 'Không thể thêm vào giỏ hàng.'
  }
}
</script>

<template>
  <main class="wishlist-page">
    <div class="wishlist-page__inner">
      <h1>Sản Phẩm Yêu Thích</h1>

      <p v-if="isLoading" class="state-message">Đang tải...</p>
      <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>
      <p v-else-if="!wishlistStore.wishlist || wishlistStore.wishlist.items.length === 0" class="state-message">
        Bạn chưa thêm sản phẩm yêu thích nào.
        <RouterLink to="/products">Tiếp tục mua sắm</RouterLink>
      </p>

      <template v-else>
        <p v-if="addToCartMessage" class="add-to-cart-message">{{ addToCartMessage }}</p>
        <div class="wishlist-grid">
          <div v-for="item in wishlistStore.wishlist.items" :key="item.productId" class="wishlist-card">
            <RouterLink :to="`/products/${item.productId}`">
              <img
                v-if="resolveImageUrl(item.productImageUrl)"
                :src="resolveImageUrl(item.productImageUrl)!"
                :alt="item.productName"
              />
              <div v-else class="wishlist-card__placeholder">Không có ảnh</div>
              <h3 class="line-clamp-2">{{ item.productName }}</h3>
              <p class="price">₫{{ item.price.toLocaleString('vi-VN') }}</p>
              <p v-if="!item.inStock" class="out-of-stock">Hết hàng</p>
            </RouterLink>
            <div class="wishlist-card__actions">
              <button type="button" :disabled="!item.inStock" @click="addToCart(item.productId)">Thêm Vào Giỏ</button>
              <button type="button" class="remove-btn" @click="removeItem(item.productId)">Xóa</button>
            </div>
          </div>
        </div>
      </template>
    </div>
  </main>
</template>

<style scoped>
.wishlist-page {
  background: var(--bg-page);
  padding: 16px 0 40px;
  min-height: 60vh;
}

.wishlist-page__inner {
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

.add-to-cart-message {
  margin-bottom: 12px;
  font-size: 13px;
  color: var(--shopee-orange);
}

.wishlist-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 12px;
}

@media (max-width: 900px) {
  .wishlist-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

.wishlist-card {
  background: #fff;
  display: flex;
  flex-direction: column;
}

.wishlist-card a {
  text-decoration: none;
  color: inherit;
  display: flex;
  flex-direction: column;
}

.wishlist-card img,
.wishlist-card__placeholder {
  width: 100%;
  aspect-ratio: 1;
  object-fit: cover;
  background: #f5f5f5;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.75rem;
  color: #aaa;
}

.wishlist-card h3 {
  font-size: 13px;
  font-weight: 400;
  margin: 8px 8px 4px;
  min-height: 2.6em;
}

.wishlist-card .price {
  margin: 0 8px 4px;
  color: var(--shopee-orange);
  font-weight: 600;
  font-size: 15px;
}

.out-of-stock {
  margin: 0 8px 8px;
  font-size: 12px;
  color: var(--text-secondary);
}

.wishlist-card__actions {
  display: flex;
  gap: 8px;
  padding: 0 8px 10px;
}

.wishlist-card__actions button {
  flex: 1;
  border: 1px solid var(--border);
  background: #fff;
  padding: 6px 0;
  font-size: 12px;
  border-radius: 2px;
}

.wishlist-card__actions button:disabled {
  opacity: 0.5;
}

.remove-btn {
  color: var(--text-secondary);
}

.remove-btn:hover {
  color: var(--shopee-orange);
}
</style>
