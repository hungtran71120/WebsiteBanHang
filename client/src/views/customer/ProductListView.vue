<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { RouterLink } from 'vue-router'
import AppIcon from '../../components/icons/AppIcon.vue'
import BannerCarousel from '../../components/BannerCarousel.vue'
import { getCategories } from '../../api/categories'
import { getProducts } from '../../api/products'
import { getRecommendations } from '../../api/recommendations'
import { getActiveFlashSale } from '../../api/flashSales'
import type { Category, Product, ProductSortBy } from '../../types/product'
import type { FlashSale } from '../../types/flashSale'
import { resolveImageUrl } from '../../utils/url'
import { useAuthStore } from '../../stores/auth'
import { useWishlistStore } from '../../stores/wishlist'

const route = useRoute()
const authStore = useAuthStore()
const wishlistStore = useWishlistStore()

const canUseWishlist = computed(() => authStore.isAuthenticated && !authStore.isAdmin)

function toggleWishlist(productId: string) {
  if (!canUseWishlist.value) {
    return
  }
  wishlistStore.toggle(productId)
}

const categories = ref<Category[]>([])
const products = ref<Product[]>([])
const totalCount = ref(0)
const totalPages = ref(1)
const page = ref(1)
const isLoading = ref(false)
const isLoadingMore = ref(false)
const errorMessage = ref('')
const loadMoreErrorMessage = ref('')

const keyword = ref('')
const categoryId = ref('')
const minPriceInput = ref('')
const maxPriceInput = ref('')
const sortBy = ref<ProductSortBy>('Default')
const pageSize = 12

const sentinel = ref<HTMLElement | null>(null)
let observer: IntersectionObserver | null = null

const hasMore = computed(() => page.value < totalPages.value)

const rootCategories = computed(() => categories.value.filter((c) => !c.parentCategoryId))
function childrenOf(parentId: string) {
  return categories.value.filter((c) => c.parentCategoryId === parentId)
}

const recommendations = ref<Product[]>([])
const showRecommendations = computed(() => !keyword.value && !categoryId.value && recommendations.value.length > 0)

async function loadRecommendations() {
  try {
    recommendations.value = await getRecommendations()
  } catch {
    recommendations.value = []
  }
}

const activeFlashSale = ref<FlashSale | null>(null)
const now = ref(Date.now())
let flashSaleTimer: ReturnType<typeof setInterval> | null = null

const showFlashSaleBanner = computed(
  () => !keyword.value && !categoryId.value && !!activeFlashSale.value && activeFlashSale.value.items.length > 0,
)

async function loadActiveFlashSale() {
  try {
    activeFlashSale.value = await getActiveFlashSale()
  } catch {
    activeFlashSale.value = null
  }
}

function flashSaleCountdownParts(endsAt: string) {
  const diff = new Date(endsAt).getTime() - now.value
  if (diff <= 0) {
    return { hours: '00', minutes: '00', seconds: '00', ended: true }
  }
  const pad = (n: number) => n.toString().padStart(2, '0')
  const hours = Math.floor(diff / 3_600_000)
  const minutes = Math.floor((diff % 3_600_000) / 60_000)
  const seconds = Math.floor((diff % 60_000) / 1000)
  return { hours: pad(hours), minutes: pad(minutes), seconds: pad(seconds), ended: false }
}

const flashSaleHeaderCountdown = computed(() =>
  activeFlashSale.value ? flashSaleCountdownParts(activeFlashSale.value.endsAt) : null,
)

function roundedRating(rating: number) {
  return Math.round(rating)
}

function formatSoldCount(count: number) {
  if (count >= 1000) {
    return `${(count / 1000).toFixed(1).replace(/\.0$/, '')}k`
  }
  return count.toString()
}

const sortTabs: { label: string; value: ProductSortBy }[] = [
  { label: 'Phổ Biến', value: 'Default' },
  { label: 'Mới Nhất', value: 'Newest' },
  { label: 'Giá: Thấp Đến Cao', value: 'PriceAsc' },
  { label: 'Giá: Cao Đến Thấp', value: 'PriceDesc' },
]

async function loadCategories() {
  categories.value = await getCategories()
}

function fetchPage(targetPage: number) {
  return getProducts({
    keyword: keyword.value || undefined,
    categoryId: categoryId.value || undefined,
    minPrice: minPriceInput.value ? Number(minPriceInput.value) : undefined,
    maxPrice: maxPriceInput.value ? Number(maxPriceInput.value) : undefined,
    sortBy: sortBy.value,
    page: targetPage,
    pageSize,
  })
}

async function resetAndLoad() {
  isLoading.value = true
  errorMessage.value = ''
  loadMoreErrorMessage.value = ''
  try {
    const result = await fetchPage(1)
    products.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages || 1
    page.value = 1
  } catch {
    errorMessage.value = 'Không thể tải danh sách sản phẩm. Vui lòng thử lại.'
  } finally {
    isLoading.value = false
  }
}

async function loadMore() {
  if (isLoading.value || isLoadingMore.value || !hasMore.value) {
    return
  }
  isLoadingMore.value = true
  loadMoreErrorMessage.value = ''
  try {
    const nextPage = page.value + 1
    const result = await fetchPage(nextPage)
    products.value = [...products.value, ...result.items]
    totalPages.value = result.totalPages || 1
    page.value = nextPage
  } catch {
    loadMoreErrorMessage.value = 'Không thể tải thêm sản phẩm.'
  } finally {
    isLoadingMore.value = false
  }
}

function selectCategory(id: string) {
  categoryId.value = id
  resetAndLoad()
}

function applyPriceFilter() {
  resetAndLoad()
}

function setSort(value: ProductSortBy) {
  sortBy.value = value
  resetAndLoad()
}

function syncFromRoute() {
  keyword.value = (route.query.keyword as string) ?? ''
  categoryId.value = (route.query.categoryId as string) ?? ''
}

watch(
  () => [route.query.keyword, route.query.categoryId],
  () => {
    syncFromRoute()
    resetAndLoad()
  },
)

onMounted(async () => {
  syncFromRoute()
  loadCategories()
  loadRecommendations()
  loadActiveFlashSale()
  await resetAndLoad()

  observer = new IntersectionObserver(
    (entries) => {
      if (entries[0]?.isIntersecting) {
        loadMore()
      }
    },
    { rootMargin: '300px' },
  )
  if (sentinel.value) {
    observer.observe(sentinel.value)
  }

  flashSaleTimer = setInterval(() => {
    now.value = Date.now()
  }, 1_000)
})

onBeforeUnmount(() => {
  observer?.disconnect()
  if (flashSaleTimer) {
    clearInterval(flashSaleTimer)
  }
})
</script>

<template>
  <main class="category-page">
    <div class="category-page__inner">
      <aside class="sidebar">
        <div class="sidebar__section">
          <h3><AppIcon name="menu" :size="16" /> Tất Cả Danh Mục</h3>
          <ul class="category-tree">
            <li>
              <a :class="{ active: categoryId === '' }" @click="selectCategory('')">Tất cả sản phẩm</a>
            </li>
            <li v-for="root in rootCategories" :key="root.id">
              <a :class="{ active: categoryId === root.id }" @click="selectCategory(root.id)">{{ root.name }}</a>
              <ul v-if="childrenOf(root.id).length" class="category-tree__children">
                <li v-for="child in childrenOf(root.id)" :key="child.id">
                  <a :class="{ active: categoryId === child.id }" @click="selectCategory(child.id)">{{ child.name }}</a>
                </li>
              </ul>
            </li>
          </ul>
        </div>

        <div class="sidebar__section">
          <h3>▽ Bộ Lọc Tìm Kiếm</h3>
          <h4>Khoảng Giá</h4>
          <div class="price-range">
            <input v-model="minPriceInput" type="number" min="0" placeholder="₫ Từ" />
            <span>—</span>
            <input v-model="maxPriceInput" type="number" min="0" placeholder="₫ Đến" />
          </div>
          <button class="apply-btn" @click="applyPriceFilter">Áp Dụng</button>
        </div>
      </aside>

      <section class="content">
        <BannerCarousel />

        <div v-if="showFlashSaleBanner" class="flash-sale">
          <div class="flash-sale__header">
            <h3><AppIcon name="zap" :size="16" :filled="true" /> Flash Sale</h3>
            <div v-if="flashSaleHeaderCountdown && !flashSaleHeaderCountdown.ended" class="flash-sale__countdown">
              <span class="flash-sale__countdown-label">Kết thúc trong</span>
              <span class="flash-sale__countdown-box">{{ flashSaleHeaderCountdown.hours }}</span>
              <span class="flash-sale__countdown-sep">:</span>
              <span class="flash-sale__countdown-box">{{ flashSaleHeaderCountdown.minutes }}</span>
              <span class="flash-sale__countdown-sep">:</span>
              <span class="flash-sale__countdown-box">{{ flashSaleHeaderCountdown.seconds }}</span>
            </div>
            <span v-else class="flash-sale__countdown-label">Đã kết thúc</span>
          </div>
          <div class="flash-sale__scroller">
            <RouterLink
              v-for="item in activeFlashSale!.items"
              :key="item.id"
              :to="`/products/${item.productId}`"
              class="flash-sale-card"
            >
              <img
                v-if="resolveImageUrl(item.imageUrl)"
                :src="resolveImageUrl(item.imageUrl)!"
                :alt="item.productName"
              />
              <div v-else class="product-card__placeholder">Không có ảnh</div>
              <h4 class="line-clamp-2">{{ item.productName }}</h4>
              <p class="flash-sale-card__price">
                <span class="sale">₫{{ item.salePrice.toLocaleString('vi-VN') }}</span>
                <span class="original">₫{{ item.originalPrice.toLocaleString('vi-VN') }}</span>
              </p>
              <p v-if="item.reviewCount > 0" class="flash-sale-card__rating">
                <span
                  v-for="i in 5"
                  :key="i"
                  class="star"
                  :class="{ 'star--filled': i <= roundedRating(item.averageRating) }"
                  >★</span
                >
                <span class="flash-sale-card__rating-count">({{ item.reviewCount }})</span>
              </p>
              <div class="flash-sale-card__progress">
                <div class="flash-sale-card__progress-bar" :style="{ width: item.soldPercentage + '%' }"></div>
                <span>Đã bán {{ item.quantitySold }}/{{ item.quantityLimit }}</span>
              </div>
            </RouterLink>
          </div>
        </div>

        <div v-if="showRecommendations" class="recommendations">
          <h3>Gợi Ý Cho Bạn</h3>
          <div class="recommendations__scroller">
            <RouterLink
              v-for="item in recommendations"
              :key="item.id"
              :to="`/products/${item.id}`"
              class="recommendation-card"
            >
              <img
                v-if="resolveImageUrl(item.imageUrl)"
                :src="resolveImageUrl(item.imageUrl)!"
                :alt="item.name"
              />
              <div v-else class="product-card__placeholder">Không có ảnh</div>
              <h4 class="line-clamp-2">{{ item.name }}</h4>
              <p class="price">₫{{ item.price.toLocaleString('vi-VN') }}</p>
            </RouterLink>
          </div>
        </div>

        <div class="toolbar">
          <span class="toolbar__label">Sắp xếp theo</span>
          <button
            v-for="tab in sortTabs"
            :key="tab.value"
            :class="['toolbar__tab', { active: sortBy === tab.value }]"
            @click="setSort(tab.value)"
          >
            {{ tab.label }}
          </button>
        </div>

        <div v-if="isLoading" class="product-grid">
          <div v-for="n in pageSize" :key="n" class="skeleton-card">
            <div class="skeleton-block skeleton-card__image"></div>
            <div class="skeleton-block skeleton-card__line skeleton-card__line--title"></div>
            <div class="skeleton-block skeleton-card__line skeleton-card__line--title-short"></div>
            <div class="skeleton-block skeleton-card__line skeleton-card__line--price"></div>
          </div>
        </div>
        <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>
        <p v-else-if="products.length === 0" class="state-message">Không tìm thấy sản phẩm phù hợp.</p>

        <template v-else>
          <p class="result-count">Hiển thị {{ products.length }} / {{ totalCount }} sản phẩm</p>
          <div class="product-grid">
            <RouterLink
              v-for="product in products"
              :key="product.id"
              :to="`/products/${product.id}`"
              class="product-card"
            >
              <button
                v-if="canUseWishlist"
                type="button"
                class="wishlist-toggle"
                :class="{ active: wishlistStore.has(product.id) }"
                :aria-label="wishlistStore.has(product.id) ? 'Bỏ yêu thích' : 'Thêm vào yêu thích'"
                @click.stop.prevent="toggleWishlist(product.id)"
              >
                <AppIcon name="heart" :size="16" :filled="wishlistStore.has(product.id)" />
              </button>
              <img
                v-if="resolveImageUrl(product.imageUrl)"
                :src="resolveImageUrl(product.imageUrl)!"
                :alt="product.name"
              />
              <div v-else class="product-card__placeholder">Không có ảnh</div>
              <h3 class="line-clamp-2">{{ product.name }}</h3>
              <p v-if="product.flashSalePrice != null" class="price price--flash">
                <span class="sale">₫{{ product.flashSalePrice.toLocaleString('vi-VN') }}</span>
                <span class="original">₫{{ product.price.toLocaleString('vi-VN') }}</span>
              </p>
              <p v-else class="price">₫{{ product.price.toLocaleString('vi-VN') }}</p>
              <p v-if="product.reviewCount > 0 || product.soldCount > 0" class="product-card__meta">
                <span v-if="product.reviewCount > 0" class="product-card__rating">
                  <span
                    v-for="i in 5"
                    :key="i"
                    class="star"
                    :class="{ 'star--filled': i <= roundedRating(product.averageRating) }"
                    >★</span
                  >
                </span>
                <span v-if="product.soldCount > 0" class="product-card__sold">
                  Đã bán {{ formatSoldCount(product.soldCount) }}
                </span>
              </p>
            </RouterLink>

            <template v-if="isLoadingMore">
              <div v-for="n in 4" :key="`more-${n}`" class="skeleton-card">
                <div class="skeleton-block skeleton-card__image"></div>
                <div class="skeleton-block skeleton-card__line skeleton-card__line--title"></div>
                <div class="skeleton-block skeleton-card__line skeleton-card__line--title-short"></div>
                <div class="skeleton-block skeleton-card__line skeleton-card__line--price"></div>
              </div>
            </template>
          </div>

          <p v-if="loadMoreErrorMessage" class="load-more-error">{{ loadMoreErrorMessage }}</p>
          <p v-else-if="!hasMore" class="end-message">Đã hiển thị tất cả sản phẩm.</p>
        </template>

        <div ref="sentinel" class="scroll-sentinel"></div>
      </section>
    </div>
  </main>
</template>

<style scoped>
.category-page {
  background: var(--bg-page);
  padding: 16px 0 40px;
}

.category-page__inner {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 16px;
  display: flex;
  gap: 16px;
  align-items: flex-start;
}

@media (max-width: 768px) {
  .category-page__inner {
    flex-direction: column;
    align-items: stretch;
  }
}

.sidebar {
  flex: 0 0 220px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

@media (max-width: 768px) {
  .sidebar {
    flex: 1 1 100%;
    width: 100%;
  }
}

.sidebar__section {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  padding: 14px 0;
  overflow: hidden;
}

.sidebar__section h3 {
  font-size: 14px;
  font-weight: 600;
  padding: 0 16px 12px;
  border-bottom: 1px solid var(--border);
  margin-bottom: 8px;
}

.sidebar__section h4 {
  font-size: 13px;
  font-weight: 500;
  padding: 0 16px;
  margin-bottom: 8px;
}

.category-tree {
  list-style: none;
  margin: 0;
  padding: 0;
}

.category-tree a {
  display: block;
  padding: 6px 16px;
  font-size: 13px;
  color: var(--text);
  text-decoration: none;
  cursor: pointer;
}

.category-tree a.active {
  color: var(--shopee-orange);
}

.category-tree__children a {
  padding-left: 28px;
  font-size: 12.5px;
}

.price-range {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 16px;
  margin-bottom: 12px;
}

.price-range input {
  width: 0;
  flex: 1;
  padding: 6px 8px;
  border: 1px solid #ccc;
  border-radius: var(--radius-sm);
}

.apply-btn {
  display: block;
  margin: 0 16px;
  width: calc(100% - 32px);
  background: var(--shopee-orange);
  color: #fff;
  border: none;
  border-radius: var(--radius-sm);
  padding: 8px 0;
  font-size: 13px;
}

.content {
  flex: 1;
  min-width: 0;
}

.flash-sale {
  background: linear-gradient(180deg, var(--shopee-orange-light), var(--surface));
  padding: 14px 16px;
  margin-bottom: 12px;
  border: 1px solid #ffd9cc;
  border-radius: var(--radius-md);
}

.flash-sale__header {
  display: flex;
  align-items: baseline;
  gap: 10px;
  margin-bottom: 10px;
}

.flash-sale__header h3 {
  font-size: 16px;
  font-weight: 700;
  color: var(--shopee-orange);
  margin: 0;
}

.flash-sale__countdown {
  display: flex;
  align-items: center;
  gap: 4px;
}

.flash-sale__countdown-label {
  font-size: 12.5px;
  color: var(--text-secondary);
}

.flash-sale__countdown-box {
  background: #2b2b2b;
  color: #fff;
  font-weight: 700;
  font-size: 12.5px;
  min-width: 18px;
  text-align: center;
  border-radius: var(--radius-sm);
  padding: 2px 4px;
}

.flash-sale__countdown-sep {
  color: var(--shopee-orange);
  font-weight: 700;
}

.flash-sale__scroller {
  display: flex;
  gap: 12px;
  overflow-x: auto;
}

.flash-sale-card {
  flex: 0 0 150px;
  text-decoration: none;
  color: inherit;
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  overflow: hidden;
  background: var(--surface);
  transition: box-shadow 0.18s, transform 0.18s;
}

.flash-sale-card:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.flash-sale-card img,
.flash-sale-card .product-card__placeholder {
  width: 100%;
  aspect-ratio: 1;
  object-fit: cover;
  background: #f5f5f5;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.7rem;
  color: #aaa;
}

.flash-sale-card h4 {
  font-size: 12px;
  font-weight: 400;
  margin: 6px 6px 4px;
  min-height: 2.4em;
}

.flash-sale-card__price {
  margin: 0 6px 6px;
  display: flex;
  align-items: baseline;
  gap: 6px;
}

.flash-sale-card__price .sale {
  color: var(--shopee-orange);
  font-weight: 600;
  font-size: 13px;
}

.flash-sale-card__price .original {
  color: var(--text-secondary);
  text-decoration: line-through;
  font-size: 11px;
}

.flash-sale-card__rating {
  margin: 0 6px 6px;
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 10px;
}

.flash-sale-card__rating .star {
  color: var(--border);
  font-size: 10px;
}

.flash-sale-card__rating .star--filled {
  color: var(--shopee-orange);
}

.flash-sale-card__rating-count {
  color: var(--text-secondary);
}

.flash-sale-card__progress {
  position: relative;
  margin: 0 6px 8px;
  height: 16px;
  background: #ffe2d6;
  border-radius: 8px;
  overflow: hidden;
}

.flash-sale-card__progress-bar {
  position: absolute;
  inset: 0;
  background: var(--shopee-orange);
  border-radius: 8px;
}

.flash-sale-card__progress span {
  position: relative;
  display: block;
  text-align: center;
  font-size: 10px;
  line-height: 16px;
  color: #fff;
  mix-blend-mode: difference;
}

.recommendations {
  background: #fff;
  padding: 12px 16px;
  margin-bottom: 12px;
}

.recommendations h3 {
  font-size: 14px;
  font-weight: 600;
  margin-bottom: 10px;
}

.recommendations__scroller {
  display: flex;
  gap: 12px;
  overflow-x: auto;
}

.recommendation-card {
  flex: 0 0 140px;
  text-decoration: none;
  color: inherit;
  border: 1px solid var(--border);
}

.recommendation-card img,
.recommendation-card .product-card__placeholder {
  width: 100%;
  aspect-ratio: 1;
  object-fit: cover;
  background: #f5f5f5;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.7rem;
  color: #aaa;
}

.recommendation-card h4 {
  font-size: 12px;
  font-weight: 400;
  margin: 6px 6px 4px;
  min-height: 2.4em;
}

.recommendation-card .price {
  margin: 0 6px 8px;
  color: var(--shopee-orange);
  font-weight: 600;
  font-size: 13px;
}

.toolbar {
  background: #fff;
  padding: 12px 16px;
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  margin-bottom: 12px;
}

.toolbar__label {
  font-size: 13px;
  color: var(--text-secondary);
}

.toolbar__tab {
  border: 1px solid var(--border);
  background: #fff;
  padding: 6px 14px;
  font-size: 13px;
  border-radius: var(--radius-sm);
}

.toolbar__tab.active {
  background: var(--shopee-orange);
  color: #fff;
  border-color: var(--shopee-orange);
}

.state-message {
  background: #fff;
  padding: 40px;
  text-align: center;
  color: var(--text-secondary);
}

.result-count {
  font-size: 13px;
  color: var(--text-secondary);
  margin-bottom: 8px;
}

.product-grid {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 12px;
}

@media (max-width: 900px) {
  .product-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 480px) {
  .product-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

.product-card {
  position: relative;
  background: var(--surface);
  text-decoration: none;
  color: inherit;
  display: flex;
  flex-direction: column;
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  overflow: hidden;
  transition: box-shadow 0.18s, border-color 0.18s, transform 0.18s;
}

.wishlist-toggle {
  position: absolute;
  top: 6px;
  right: 6px;
  z-index: 1;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.9);
  box-shadow: var(--shadow-sm);
  color: var(--text-secondary);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}

.wishlist-toggle:hover {
  color: var(--shopee-orange);
}

.wishlist-toggle.active {
  color: var(--shopee-orange);
}

.product-card:hover {
  box-shadow: var(--shadow-md);
  border-color: var(--border);
  transform: translateY(-2px);
}

.product-card img,
.product-card__placeholder {
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

.product-card h3 {
  font-size: 13px;
  font-weight: 400;
  margin: 8px 8px 4px;
  min-height: 2.6em;
}

.product-card .price {
  margin: 0 8px 4px;
  color: var(--shopee-orange);
  font-weight: 600;
  font-size: 15px;
}

.product-card__meta {
  margin: 0 8px 10px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 6px;
  font-size: 11px;
}

.product-card__rating .star {
  color: var(--border);
  font-size: 10px;
}

.product-card__rating .star--filled {
  color: var(--shopee-orange);
}

.product-card__sold {
  color: var(--text-secondary);
}

.product-card .price--flash {
  display: flex;
  align-items: baseline;
  gap: 6px;
}

.product-card .price--flash .sale {
  color: var(--shopee-orange);
  font-weight: 600;
  font-size: 15px;
}

.product-card .price--flash .original {
  color: var(--text-secondary);
  text-decoration: line-through;
  font-size: 12px;
  font-weight: 400;
}

.skeleton-card {
  background: #fff;
  display: flex;
  flex-direction: column;
  padding-bottom: 10px;
}

.skeleton-card__image {
  width: 100%;
  aspect-ratio: 1;
  border-radius: 0;
}

.skeleton-card__line {
  height: 10px;
  margin: 8px 8px 0;
}

.skeleton-card__line--title {
  width: 90%;
}

.skeleton-card__line--title-short {
  width: 55%;
}

.skeleton-card__line--price {
  width: 40%;
  height: 14px;
  margin-top: 10px;
}

.load-more-error,
.end-message {
  text-align: center;
  padding: 16px 0;
  font-size: 13px;
  color: var(--text-secondary);
}

.scroll-sentinel {
  height: 1px;
}
</style>
