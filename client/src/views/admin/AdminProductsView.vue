<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { deleteProduct, getProducts } from '../../api/products'
import { getCategories } from '../../api/categories'
import type { Category, Product } from '../../types/product'
import { resolveImageUrl } from '../../utils/url'

const router = useRouter()

const products = ref<Product[]>([])
const categories = ref<Category[]>([])
const page = ref(1)
const totalPages = ref(1)
const isLoading = ref(true)
const errorMessage = ref('')
const keyword = ref('')
const categoryId = ref('')
const pageSize = 10

let searchTimeout: ReturnType<typeof setTimeout> | undefined

async function load() {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const result = await getProducts({
      keyword: keyword.value || undefined,
      categoryId: categoryId.value || undefined,
      page: page.value,
      pageSize,
    })
    products.value = result.items
    totalPages.value = result.totalPages || 1
  } catch {
    errorMessage.value = 'Không thể tải danh sách sản phẩm.'
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

async function handleDelete(product: Product) {
  if (!confirm(`Xóa sản phẩm "${product.name}"?`)) {
    return
  }
  try {
    await deleteProduct(product.id)
    await load()
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể xóa sản phẩm.')
  }
}

watch([keyword, categoryId], () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    page.value = 1
    load()
  }, 300)
})

onMounted(async () => {
  categories.value = await getCategories()
  await load()
})
</script>

<template>
  <div class="admin-page">
    <div class="admin-page__header">
      <h1>Sản phẩm</h1>
      <button type="button" class="btn-primary" @click="router.push({ name: 'admin-product-new' })">
        + Thêm sản phẩm
      </button>
    </div>

    <div class="admin-filters">
      <input v-model="keyword" type="text" placeholder="Tìm theo tên sản phẩm..." />
      <select v-model="categoryId">
        <option value="">Tất cả danh mục</option>
        <option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</option>
      </select>
    </div>

    <p v-if="isLoading" class="state-message">Đang tải...</p>
    <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>
    <template v-else>
      <table class="admin-table">
        <thead>
          <tr>
            <th></th>
            <th>Tên sản phẩm</th>
            <th>Danh mục</th>
            <th>Giá</th>
            <th>Tồn kho</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="product in products" :key="product.id">
            <td>
              <img
                v-if="resolveImageUrl(product.imageUrl)"
                :src="resolveImageUrl(product.imageUrl)!"
                class="admin-table__thumb"
                alt=""
              />
              <div v-else class="admin-table__thumb"></div>
            </td>
            <td>{{ product.name }}</td>
            <td>{{ product.categoryName }}</td>
            <td>₫{{ product.price.toLocaleString('vi-VN') }}</td>
            <td>{{ product.stock }}</td>
            <td class="admin-table__actions">
              <button
                type="button"
                class="btn-link"
                @click="router.push({ name: 'admin-product-edit', params: { id: product.id } })"
              >
                Sửa
              </button>
              <button type="button" class="btn-link btn-link--danger" @click="handleDelete(product)">Xóa</button>
            </td>
          </tr>
          <tr v-if="products.length === 0">
            <td colspan="6" class="state-message">Chưa có sản phẩm nào.</td>
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
