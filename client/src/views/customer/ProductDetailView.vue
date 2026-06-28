<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppIcon from '../../components/icons/AppIcon.vue'
import { createReview, deleteReview, getProductReviews, updateReview } from '../../api/reviews'
import { getProductById, getRelatedProducts } from '../../api/products'
import { useAuthStore } from '../../stores/auth'
import { useCartStore } from '../../stores/cart'
import { useNotifyStore } from '../../stores/notify'
import { useWishlistStore } from '../../stores/wishlist'
import type { Product, ProductVariantOptionValue } from '../../types/product'
import type { Review } from '../../types/review'
import { resolveImageUrl } from '../../utils/url'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const cartStore = useCartStore()
const wishlistStore = useWishlistStore()
const notifyStore = useNotifyStore()

const canUseWishlist = computed(() => authStore.isAuthenticated && !authStore.isAdmin)

function toggleWishlist() {
  if (!canUseWishlist.value || !product.value) {
    return
  }
  wishlistStore.toggle(product.value.id)
}

const product = ref<Product | null>(null)
const errorMessage = ref('')
const isLoading = ref(true)
const quantity = ref(1)
const isAddingToCart = ref(false)
const selectedValue1 = ref<ProductVariantOptionValue | null>(null)
const selectedValue2 = ref<ProductVariantOptionValue | null>(null)

const reviews = ref<Review[]>([])
const reviewsTotalCount = ref(0)
const reviewsPage = ref(1)
const reviewsPageSize = 10
const isLoadingReviews = ref(false)
const loadMoreReviewsError = ref('')

const reviewRating = ref(5)
const reviewComment = ref('')
const reviewFormError = ref('')
const isSubmittingReview = ref(false)
const editingReviewId = ref<string | null>(null)

const roundedAverageRating = computed(() => Math.round(product.value?.averageRating ?? 0))
const hasMoreReviews = computed(() => reviews.value.length < reviewsTotalCount.value)

const relatedProducts = ref<Product[]>([])
const now = ref(Date.now())
let flashSaleTimer: ReturnType<typeof setInterval> | null = null

function flashSaleCountdown(endsAt: string) {
  const diff = new Date(endsAt).getTime() - now.value
  if (diff <= 0) {
    return 'Đã kết thúc'
  }
  const hours = Math.floor(diff / 3_600_000)
  const minutes = Math.floor((diff % 3_600_000) / 60_000)
  return `${hours}h ${minutes}m`
}

onMounted(async () => {
  try {
    product.value = await getProductById(route.params.id as string)
    await loadReviewsPage(true)
    relatedProducts.value = await getRelatedProducts(product.value.id)
  } catch {
    errorMessage.value = 'Không tìm thấy sản phẩm.'
  } finally {
    isLoading.value = false
  }

  flashSaleTimer = setInterval(() => {
    now.value = Date.now()
  }, 30_000)
})

onBeforeUnmount(() => {
  if (flashSaleTimer) {
    clearInterval(flashSaleTimer)
  }
})

async function loadReviewsPage(reset: boolean) {
  if (!product.value) {
    return
  }
  if (reset) {
    reviewsPage.value = 1
  }
  isLoadingReviews.value = true
  loadMoreReviewsError.value = ''
  try {
    const result = await getProductReviews(product.value.id, reviewsPage.value, reviewsPageSize)
    reviews.value = reset ? result.items : [...reviews.value, ...result.items]
    reviewsTotalCount.value = result.totalCount
  } catch {
    loadMoreReviewsError.value = 'Không thể tải đánh giá.'
  } finally {
    isLoadingReviews.value = false
  }
}

function loadMoreReviews() {
  reviewsPage.value += 1
  loadReviewsPage(false)
}

function startEditReview(review: Review) {
  editingReviewId.value = review.id
  reviewRating.value = review.rating
  reviewComment.value = review.comment
  reviewFormError.value = ''
}

function cancelEditReview() {
  editingReviewId.value = null
  reviewRating.value = 5
  reviewComment.value = ''
  reviewFormError.value = ''
}

async function submitReview() {
  if (!product.value) {
    return
  }
  reviewFormError.value = ''
  isSubmittingReview.value = true
  try {
    if (editingReviewId.value) {
      const updated = await updateReview(editingReviewId.value, { rating: reviewRating.value, comment: reviewComment.value })
      const index = reviews.value.findIndex((r) => r.id === updated.id)
      if (index !== -1) {
        reviews.value[index] = updated
      }
      cancelEditReview()
    } else {
      await createReview(product.value.id, { rating: reviewRating.value, comment: reviewComment.value })
      reviewComment.value = ''
      reviewRating.value = 5
      product.value = await getProductById(product.value.id)
      await loadReviewsPage(true)
    }
  } catch (error: unknown) {
    const messages = (error as { response?: { data?: string[] } })?.response?.data
    reviewFormError.value = messages?.[0] ?? 'Không thể gửi đánh giá.'
  } finally {
    isSubmittingReview.value = false
  }
}

async function removeReview(reviewId: string) {
  if (!product.value) {
    return
  }
  try {
    await deleteReview(reviewId)
    product.value = await getProductById(product.value.id)
    await loadReviewsPage(true)
  } catch {
    loadMoreReviewsError.value = 'Không thể xóa đánh giá.'
  }
}

const option1 = computed(() => product.value?.variantOptions.find((o) => o.displayOrder === 1) ?? null)
const option2 = computed(() => product.value?.variantOptions.find((o) => o.displayOrder === 2) ?? null)

const selectedVariant = computed(() => {
  if (!product.value || !selectedValue1.value) {
    return null
  }
  if (option2.value && !selectedValue2.value) {
    return null
  }

  return (
    product.value.variants.find(
      (v) => v.optionValue1Id === selectedValue1.value!.id && v.optionValue2Id === (selectedValue2.value?.id ?? null),
    ) ?? null
  )
})

const effectiveStock = computed(() => {
  if (!product.value) {
    return 0
  }
  if (product.value.variantOptions.length === 0) {
    return product.value.stock
  }
  return selectedVariant.value?.stock ?? 0
})

const displayImageUrl = computed(() => selectedValue1.value?.imageUrl ?? product.value?.imageUrl ?? null)

const addToCartDisabled = computed(() => {
  if (isAddingToCart.value || !product.value) {
    return true
  }
  if (product.value.variantOptions.length === 0) {
    return product.value.stock === 0
  }
  return !!selectedVariant.value && selectedVariant.value.stock === 0
})

const addToCartLabel = computed(() => {
  if (!product.value) {
    return 'Thêm Vào Giỏ'
  }
  const stock = product.value.variantOptions.length === 0 ? product.value.stock : selectedVariant.value?.stock
  return stock === 0 ? 'Hết hàng' : 'Thêm Vào Giỏ'
})

watch(selectedVariant, () => {
  quantity.value = 1
})

function selectValue1(value: ProductVariantOptionValue) {
  selectedValue1.value = value
  selectedValue2.value = null
}

function selectValue2(value: ProductVariantOptionValue) {
  selectedValue2.value = value
}

function isValue1Disabled(value: ProductVariantOptionValue): boolean {
  if (!product.value) {
    return true
  }
  const variants = product.value.variants.filter((v) => v.optionValue1Id === value.id)
  return variants.length === 0 || variants.every((v) => v.stock === 0)
}

function isValue2Disabled(value: ProductVariantOptionValue): boolean {
  if (!product.value || !selectedValue1.value) {
    return false
  }
  const variant = product.value.variants.find(
    (v) => v.optionValue1Id === selectedValue1.value!.id && v.optionValue2Id === value.id,
  )
  return !variant || variant.stock === 0
}

async function addToCart() {
  if (!authStore.isAuthenticated) {
    router.push({ name: 'login', query: { redirect: route.fullPath } })
    return
  }

  if (product.value!.variantOptions.length > 0 && !selectedVariant.value) {
    notifyStore.show('Vui lòng chọn đầy đủ phân loại.')
    return
  }

  isAddingToCart.value = true
  try {
    await cartStore.addItem(product.value!.id, quantity.value, selectedVariant.value?.id ?? null)
    notifyStore.show('Đã thêm vào giỏ hàng.')
  } catch (error: unknown) {
    const messages = (error as { response?: { data?: string[] } })?.response?.data
    notifyStore.show(messages?.[0] ?? 'Không thể thêm vào giỏ hàng.')
  } finally {
    isAddingToCart.value = false
  }
}
</script>

<template>
  <main class="product-detail-page">
    <div v-if="isLoading" class="product-detail-page__inner">
      <div class="skeleton-block skeleton-breadcrumb"></div>
      <div class="product-card">
        <div class="skeleton-block product-card__image"></div>
        <div class="product-card__info">
          <div class="skeleton-block skeleton-line skeleton-line--title"></div>
          <div class="skeleton-block skeleton-line skeleton-line--price"></div>
          <div class="skeleton-block skeleton-line skeleton-line--meta"></div>
          <div class="skeleton-block skeleton-line skeleton-line--meta-short"></div>
        </div>
      </div>
    </div>
    <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>

    <div v-else-if="product" class="product-detail-page__inner">
      <nav class="breadcrumb">
        <RouterLink to="/products">Sản phẩm</RouterLink>
        <span>›</span>
        <RouterLink :to="`/products?categoryId=${product.categoryId}`">{{ product.categoryName }}</RouterLink>
        <span>›</span>
        <span>{{ product.name }}</span>
      </nav>

      <div class="product-card">
        <div class="product-card__image">
          <img
            v-if="resolveImageUrl(displayImageUrl)"
            :src="resolveImageUrl(displayImageUrl)!"
            :alt="product.name"
          />
          <div v-else class="product-card__placeholder">Không có ảnh</div>
        </div>

        <div class="product-card__info">
          <h1>{{ product.name }}</h1>

          <div v-if="product.reviewCount > 0" class="rating-summary">
            <span class="rating-summary__stars">
              <span v-for="i in 5" :key="i" class="star" :class="{ 'star--filled': i <= roundedAverageRating }">★</span>
            </span>
            <span class="rating-summary__score">{{ product.averageRating.toFixed(1) }}</span>
            <span class="rating-summary__count">({{ product.reviewCount }} đánh giá)</span>
          </div>

          <div v-if="product.flashSalePrice != null" class="price-box price-box--flash">
            <div class="price-box__flash-header">
              <span class="price-box__flash-label"><AppIcon name="zap" :size="14" :filled="true" /> Flash Sale</span>
              <span v-if="product.flashSaleEndsAt" class="price-box__flash-countdown">
                Kết thúc trong {{ flashSaleCountdown(product.flashSaleEndsAt) }}
              </span>
            </div>
            <span class="price">₫{{ product.flashSalePrice.toLocaleString('vi-VN') }}</span>
            <span class="price-box__original">₫{{ product.price.toLocaleString('vi-VN') }}</span>
            <span v-if="product.flashSaleQuantityRemaining != null" class="price-box__remaining">
              Còn {{ product.flashSaleQuantityRemaining }} suất ưu đãi
            </span>
          </div>
          <div v-else class="price-box">
            <span class="price">₫{{ product.price.toLocaleString('vi-VN') }}</span>
          </div>

          <dl class="meta">
            <dt>Danh mục</dt>
            <dd>{{ product.categoryName }}</dd>
            <dt>Tồn kho</dt>
            <dd>{{ effectiveStock }}</dd>
          </dl>

          <div v-if="option1" class="color-picker">
            <span class="color-picker__label">{{ option1.name }}</span>
            <div class="color-picker__options">
              <button
                v-for="value in option1.values"
                :key="value.id"
                type="button"
                class="color-option"
                :class="{ 'color-option--selected': selectedValue1?.id === value.id }"
                :disabled="isValue1Disabled(value)"
                @click="selectValue1(value)"
              >
                <img
                  v-if="resolveImageUrl(value.imageUrl ?? product.imageUrl)"
                  :src="resolveImageUrl(value.imageUrl ?? product.imageUrl)!"
                  :alt="value.value"
                />
                <div v-else class="color-option__placeholder"></div>
                <span class="color-option__label">{{ value.value }}</span>
              </button>
            </div>
          </div>

          <div v-if="option2" class="color-picker">
            <span class="color-picker__label">{{ option2.name }}</span>
            <div class="color-picker__options">
              <button
                v-for="value in option2.values"
                :key="value.id"
                type="button"
                class="text-option"
                :class="{ 'text-option--selected': selectedValue2?.id === value.id }"
                :disabled="isValue2Disabled(value)"
                @click="selectValue2(value)"
              >
                {{ value.value }}
              </button>
            </div>
          </div>

          <div class="add-to-cart">
            <span>Số lượng</span>
            <div class="qty-stepper">
              <button @click="quantity = Math.max(1, quantity - 1)">−</button>
              <input v-model.number="quantity" type="number" min="1" :max="effectiveStock" />
              <button @click="quantity = Math.min(effectiveStock, quantity + 1)">+</button>
            </div>
            <button class="add-to-cart-btn" :disabled="addToCartDisabled" @click="addToCart">
              {{ addToCartLabel }}
            </button>
            <button
              v-if="canUseWishlist"
              type="button"
              class="wishlist-btn"
              :class="{ active: wishlistStore.has(product.id) }"
              @click="toggleWishlist"
            >
              <AppIcon name="heart" :size="16" :filled="wishlistStore.has(product.id)" />
              {{ wishlistStore.has(product.id) ? 'Đã Yêu Thích' : 'Yêu Thích' }}
            </button>
          </div>
        </div>
      </div>

      <div class="description-box">
        <h2>CHI TIẾT SẢN PHẨM</h2>
        <p>{{ product.description }}</p>
      </div>

      <div class="review-section">
        <h2>ĐÁNH GIÁ SẢN PHẨM</h2>

        <div v-if="authStore.isAuthenticated" class="review-form">
          <p class="review-form__label">{{ editingReviewId ? 'Sửa đánh giá của bạn' : 'Viết đánh giá' }}</p>
          <div class="review-form__stars">
            <button
              v-for="i in 5"
              :key="i"
              type="button"
              class="star-input"
              :class="{ 'star-input--filled': i <= reviewRating }"
              @click="reviewRating = i"
            >★</button>
          </div>
          <textarea v-model="reviewComment" rows="3" placeholder="Chia sẻ cảm nhận của bạn về sản phẩm..."></textarea>
          <div class="review-form__actions">
            <button type="button" class="review-form__submit" :disabled="isSubmittingReview" @click="submitReview">
              {{ editingReviewId ? 'Lưu' : 'Gửi đánh giá' }}
            </button>
            <button v-if="editingReviewId" type="button" class="review-form__cancel" @click="cancelEditReview">Hủy</button>
          </div>
          <p v-if="reviewFormError" class="review-form__error">{{ reviewFormError }}</p>
        </div>
        <p v-else class="review-section__login-hint">
          <RouterLink :to="{ name: 'login', query: { redirect: route.fullPath } }">Đăng nhập</RouterLink> để viết đánh giá.
        </p>

        <div v-if="isLoadingReviews && reviews.length === 0" class="skeleton-block review-skeleton"></div>
        <p v-else-if="reviews.length === 0" class="review-section__empty">Chưa có đánh giá nào cho sản phẩm này.</p>
        <ul v-else class="review-list">
          <li v-for="review in reviews" :key="review.id" class="review-item">
            <div class="review-item__header">
              <span class="review-item__name">{{ review.userName }}</span>
              <span class="review-item__date">{{ new Date(review.createdAt).toLocaleDateString('vi-VN') }}</span>
            </div>
            <div class="review-item__stars">
              <span v-for="i in 5" :key="i" class="star" :class="{ 'star--filled': i <= review.rating }">★</span>
            </div>
            <p class="review-item__comment">{{ review.comment }}</p>
            <div v-if="review.userId === authStore.user?.id" class="review-item__actions">
              <button type="button" @click="startEditReview(review)">Sửa</button>
              <button type="button" @click="removeReview(review.id)">Xóa</button>
            </div>
          </li>
        </ul>

        <button
          v-if="hasMoreReviews"
          type="button"
          class="review-section__load-more"
          :disabled="isLoadingReviews"
          @click="loadMoreReviews"
        >
          Xem thêm đánh giá
        </button>
        <p v-if="loadMoreReviewsError" class="review-section__error">{{ loadMoreReviewsError }}</p>
      </div>

      <div v-if="relatedProducts.length > 0" class="related-section">
        <h2>SẢN PHẨM LIÊN QUAN</h2>
        <div class="related-grid">
          <RouterLink
            v-for="related in relatedProducts"
            :key="related.id"
            :to="`/products/${related.id}`"
            class="related-card"
          >
            <img
              v-if="resolveImageUrl(related.imageUrl)"
              :src="resolveImageUrl(related.imageUrl)!"
              :alt="related.name"
            />
            <div v-else class="related-card__placeholder">Không có ảnh</div>
            <h3 class="line-clamp-2">{{ related.name }}</h3>
            <p class="price">₫{{ related.price.toLocaleString('vi-VN') }}</p>
          </RouterLink>
        </div>
      </div>
    </div>
  </main>
</template>

<style scoped>
.product-detail-page {
  background: var(--bg-page);
  padding: 16px 0 40px;
  min-height: 60vh;
}

.product-detail-page__inner {
  max-width: 1100px;
  margin: 0 auto;
  padding: 0 16px;
}

.state-message {
  text-align: center;
  padding: 60px 0;
  color: var(--text-secondary);
}

.breadcrumb {
  display: flex;
  gap: 8px;
  font-size: 13px;
  color: var(--text-secondary);
  margin-bottom: 12px;
}

.breadcrumb a {
  text-decoration: none;
  color: var(--text-secondary);
}

.product-card {
  background: #fff;
  display: flex;
  gap: 32px;
  padding: 24px;
}

@media (max-width: 700px) {
  .product-card {
    flex-direction: column;
    gap: 16px;
    padding: 16px;
  }
}

.product-card__image {
  flex: 0 0 360px;
}

@media (max-width: 700px) {
  .product-card__image {
    flex: 1 1 auto;
  }
}

.product-card__image img,
.product-card__placeholder {
  width: 100%;
  aspect-ratio: 1;
  object-fit: cover;
  background: #f5f5f5;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #aaa;
}

.product-card__info {
  flex: 1;
  min-width: 0;
}

.product-card__info h1 {
  font-size: 20px;
  font-weight: 400;
  margin-bottom: 12px;
}

.skeleton-breadcrumb {
  width: 240px;
  height: 14px;
  margin-bottom: 12px;
}

.skeleton-line {
  height: 14px;
  margin-bottom: 16px;
}

.skeleton-line--title {
  width: 70%;
  height: 22px;
}

.skeleton-line--price {
  height: 48px;
  width: 100%;
}

.skeleton-line--meta {
  width: 50%;
}

.skeleton-line--meta-short {
  width: 35%;
}

.price-box {
  background: var(--shopee-orange-light);
  padding: 16px 20px;
  margin-bottom: 16px;
}

.price-box .price {
  color: var(--shopee-orange);
  font-size: 28px;
  font-weight: 600;
}

.price-box--flash {
  display: flex;
  align-items: baseline;
  gap: 12px;
  flex-wrap: wrap;
  border: 1px solid #ffd9cc;
}

.price-box__flash-header {
  flex: 0 0 100%;
  display: flex;
  align-items: baseline;
  gap: 10px;
  margin-bottom: 4px;
}

.price-box__flash-label {
  font-weight: 700;
  color: var(--shopee-orange);
  font-size: 14px;
}

.price-box__flash-countdown {
  font-size: 12.5px;
  color: var(--text-secondary);
}

.price-box__original {
  color: var(--text-secondary);
  text-decoration: line-through;
  font-size: 15px;
}

.price-box__remaining {
  flex: 0 0 100%;
  font-size: 12.5px;
  color: var(--text-secondary);
}

.meta {
  display: grid;
  grid-template-columns: 100px 1fr;
  row-gap: 10px;
  font-size: 14px;
}

.meta dt {
  color: var(--text-secondary);
}

.meta dd {
  margin: 0;
}

.color-picker {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  margin-top: 20px;
}

.color-picker__label {
  flex: 0 0 90px;
  color: var(--text-secondary);
  font-size: 14px;
  padding-top: 8px;
}

.color-picker__options {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.color-option {
  display: flex;
  align-items: center;
  gap: 8px;
  border: 1px solid var(--border);
  background: #fff;
  padding: 6px 12px 6px 6px;
  font-size: 13px;
  border-radius: var(--radius-sm);
  cursor: pointer;
  min-width: 140px;
}

.color-option img,
.color-option__placeholder {
  width: 36px;
  height: 36px;
  object-fit: cover;
  background: #f5f5f5;
  flex-shrink: 0;
}

.color-option__label {
  text-align: left;
  line-height: 1.3;
}

.color-option--selected {
  border-color: var(--shopee-orange);
  color: var(--shopee-orange);
}

.color-option:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.color-option:disabled .color-option__label {
  text-decoration: line-through;
}

.text-option {
  border: 1px solid var(--border);
  background: #fff;
  padding: 8px 16px;
  font-size: 13px;
  border-radius: var(--radius-sm);
  cursor: pointer;
}

.text-option--selected {
  border-color: var(--shopee-orange);
  color: var(--shopee-orange);
}

.text-option:disabled {
  opacity: 0.4;
  cursor: not-allowed;
  text-decoration: line-through;
}

.add-to-cart {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-top: 20px;
  flex-wrap: wrap;
}

.qty-stepper {
  display: flex;
  align-items: center;
  gap: 6px;
}

.qty-stepper button {
  width: 30px;
  height: 30px;
  border: 1px solid var(--border);
  background: #fff;
}

.qty-stepper input {
  width: 54px;
  height: 30px;
  text-align: center;
  border: 1px solid var(--border);
}

.add-to-cart-btn {
  background: var(--shopee-orange);
  color: #fff;
  border: none;
  padding: 12px 28px;
  font-size: 14px;
  border-radius: var(--radius-sm);
}

.add-to-cart-btn:disabled {
  opacity: 0.6;
}

.wishlist-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: var(--surface);
  border: 1px solid var(--border);
  padding: 12px 20px;
  font-size: 14px;
  border-radius: var(--radius-sm);
  cursor: pointer;
}

.wishlist-btn:hover {
  border-color: var(--shopee-orange);
  color: var(--shopee-orange);
}

.wishlist-btn.active {
  border-color: var(--shopee-orange);
  color: var(--shopee-orange);
}

.description-box {
  background: #fff;
  margin-top: 16px;
  padding: 24px;
}

.description-box h2 {
  font-size: 15px;
  margin-bottom: 12px;
}

.description-box p {
  white-space: pre-line;
  line-height: 1.6;
  color: var(--text);
}

.rating-summary {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
  font-size: 13px;
  color: var(--text-secondary);
}

.rating-summary__score {
  color: var(--shopee-orange);
  font-weight: 600;
}

.star {
  color: var(--border);
}

.star--filled {
  color: var(--shopee-orange);
}

.review-section {
  background: #fff;
  margin-top: 16px;
  padding: 24px;
}

.review-section h2 {
  font-size: 15px;
  margin-bottom: 16px;
}

.review-section__login-hint {
  font-size: 14px;
  color: var(--text-secondary);
}

.review-section__login-hint a {
  color: var(--shopee-orange);
}

.review-section__empty {
  color: var(--text-secondary);
  font-size: 14px;
}

.review-skeleton {
  height: 80px;
}

.review-form {
  border: 1px solid var(--border);
  padding: 16px;
  margin-bottom: 20px;
}

.review-form__label {
  font-size: 14px;
  margin-bottom: 8px;
}

.review-form__stars {
  display: flex;
  gap: 6px;
  margin-bottom: 10px;
}

.star-input {
  background: none;
  border: none;
  font-size: 34px;
  line-height: 1;
  color: #d0d0d0;
  cursor: pointer;
  padding: 0;
  transition: color 0.15s, transform 0.1s;
}

.star-input:hover {
  color: var(--shopee-orange);
  transform: scale(1.1);
}

.star-input--filled {
  color: var(--shopee-orange);
}

.review-form textarea {
  width: 100%;
  border: 1px solid var(--border);
  padding: 10px;
  font-size: 14px;
  resize: vertical;
  font-family: inherit;
}

.review-form__actions {
  display: flex;
  gap: 10px;
  margin-top: 10px;
}

.review-form__submit {
  background: var(--shopee-orange);
  color: #fff;
  border: none;
  padding: 8px 20px;
  font-size: 13px;
  border-radius: var(--radius-sm);
}

.review-form__submit:disabled {
  opacity: 0.6;
}

.review-form__cancel {
  background: #fff;
  border: 1px solid var(--border);
  padding: 8px 20px;
  font-size: 13px;
  border-radius: var(--radius-sm);
}

.review-form__error {
  margin-top: 8px;
  font-size: 13px;
  color: var(--shopee-orange);
}

.review-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.review-item {
  border-bottom: 1px solid var(--border);
  padding: 16px 0;
}

.review-item__header {
  display: flex;
  justify-content: space-between;
  font-size: 13px;
  color: var(--text-secondary);
  margin-bottom: 6px;
}

.review-item__stars {
  font-size: 14px;
  margin-bottom: 6px;
}

.review-item__comment {
  font-size: 14px;
  color: var(--text);
  white-space: pre-line;
}

.review-item__actions {
  display: flex;
  gap: 12px;
  margin-top: 8px;
}

.review-item__actions button {
  background: none;
  border: none;
  color: var(--shopee-orange);
  font-size: 13px;
  cursor: pointer;
  padding: 0;
}

.review-section__load-more {
  display: block;
  margin: 16px auto 0;
  background: #fff;
  border: 1px solid var(--border);
  padding: 8px 24px;
  font-size: 13px;
  border-radius: var(--radius-sm);
}

.review-section__error {
  margin-top: 10px;
  font-size: 13px;
  color: var(--shopee-orange);
}

.related-section {
  background: #fff;
  margin-top: 16px;
  padding: 24px;
}

.related-section h2 {
  font-size: 15px;
  margin-bottom: 16px;
}

.related-grid {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 12px;
}

@media (max-width: 900px) {
  .related-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 480px) {
  .related-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

.related-card {
  text-decoration: none;
  color: inherit;
  display: flex;
  flex-direction: column;
  border: 1px solid var(--border);
}

.related-card img,
.related-card__placeholder {
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

.related-card h3 {
  font-size: 13px;
  font-weight: 400;
  margin: 8px 8px 4px;
  min-height: 2.6em;
}

.related-card .price {
  margin: 0 8px 10px;
  color: var(--shopee-orange);
  font-weight: 600;
  font-size: 15px;
}
</style>
