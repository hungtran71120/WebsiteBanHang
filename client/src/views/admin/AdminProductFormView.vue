<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  addVariant,
  addVariantOption,
  addVariantOptionValue,
  createProduct,
  deleteVariant,
  deleteVariantOption,
  deleteVariantOptionValue,
  getProductById,
  updateProduct,
  updateVariant,
  uploadProductImage,
  uploadVariantOptionValueImage,
} from '../../api/products'
import { getCategories } from '../../api/categories'
import type { Category, Product } from '../../types/product'
import { resolveImageUrl } from '../../utils/url'

const route = useRoute()
const router = useRouter()
const productId = computed(() => route.params.id as string | undefined)
const isEditMode = computed(() => !!productId.value)

const categories = ref<Category[]>([])
const product = ref<Product | null>(null)
const isLoading = ref(true)
const loadErrorMessage = ref('')

const form = reactive({ name: '', description: '', price: 0, stock: 0, categoryId: '' })
const isSaving = ref(false)
const saveErrorMessage = ref('')

const sortedOptions = computed(() =>
  product.value ? [...product.value.variantOptions].sort((a, b) => a.displayOrder - b.displayOrder) : [],
)
const firstOption = computed(() => sortedOptions.value[0] ?? null)
const secondOption = computed(() => sortedOptions.value[1] ?? null)

async function loadProduct() {
  if (!productId.value) {
    return
  }
  isLoading.value = true
  loadErrorMessage.value = ''
  try {
    product.value = await getProductById(productId.value)
    form.name = product.value.name
    form.description = product.value.description
    form.price = product.value.price
    form.stock = product.value.stock
    form.categoryId = product.value.categoryId
  } catch {
    loadErrorMessage.value = 'Không thể tải sản phẩm.'
  } finally {
    isLoading.value = false
  }
}

async function submitBasicInfo() {
  isSaving.value = true
  saveErrorMessage.value = ''
  try {
    const request = { ...form }
    if (isEditMode.value) {
      product.value = await updateProduct(productId.value!, request)
    } else {
      const created = await createProduct(request)
      router.replace({ name: 'admin-product-edit', params: { id: created.id } })
    }
  } catch (err: any) {
    saveErrorMessage.value = err?.response?.data?.[0] ?? 'Không thể lưu sản phẩm.'
  } finally {
    isSaving.value = false
  }
}

async function handleImageChange(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file || !productId.value) {
    return
  }
  try {
    product.value = await uploadProductImage(productId.value, file)
  } catch {
    alert('Không thể upload ảnh.')
  }
}

const newOptionName = ref('')
const newOptionValues = ref('')

async function handleAddOption() {
  if (!productId.value || !newOptionName.value.trim()) {
    return
  }
  const values = newOptionValues.value
    .split(',')
    .map((v) => v.trim())
    .filter(Boolean)
    .map((v) => ({ value: v }))
  if (values.length === 0) {
    alert('Vui lòng nhập ít nhất 1 giá trị, phân tách bằng dấu phẩy.')
    return
  }
  try {
    product.value = await addVariantOption(productId.value, { name: newOptionName.value.trim(), values })
    newOptionName.value = ''
    newOptionValues.value = ''
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể thêm phân loại.')
  }
}

async function handleDeleteOption(optionId: string) {
  if (!productId.value || !confirm('Xóa phân loại này?')) {
    return
  }
  try {
    product.value = await deleteVariantOption(productId.value, optionId)
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể xóa phân loại (có thể đang được SKU sử dụng).')
  }
}

const newValueByOption = reactive<Record<string, string>>({})

async function handleAddValue(optionId: string) {
  const value = newValueByOption[optionId]?.trim()
  if (!productId.value || !value) {
    return
  }
  try {
    product.value = await addVariantOptionValue(productId.value, optionId, { value })
    newValueByOption[optionId] = ''
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể thêm giá trị.')
  }
}

async function handleDeleteValue(optionId: string, valueId: string) {
  if (!productId.value || !confirm('Xóa giá trị này?')) {
    return
  }
  try {
    product.value = await deleteVariantOptionValue(productId.value, optionId, valueId)
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể xóa giá trị (có thể đang được SKU sử dụng).')
  }
}

async function handleValueImageChange(optionId: string, valueId: string, event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file || !productId.value) {
    return
  }
  try {
    product.value = await uploadVariantOptionValueImage(productId.value, optionId, valueId, file)
  } catch {
    alert('Không thể upload ảnh.')
  }
}

const newSkuValue1 = ref('')
const newSkuValue2 = ref('')
const newSkuStock = ref(0)

async function handleAddVariant() {
  if (!productId.value || !newSkuValue1.value) {
    alert('Vui lòng chọn phân loại.')
    return
  }
  try {
    product.value = await addVariant(productId.value, {
      optionValue1Id: newSkuValue1.value,
      optionValue2Id: newSkuValue2.value || null,
      stock: newSkuStock.value,
    })
    newSkuValue1.value = ''
    newSkuValue2.value = ''
    newSkuStock.value = 0
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể thêm SKU.')
  }
}

const variantStockEdits = reactive<Record<string, number>>({})

watch(
  product,
  (p) => {
    if (p) {
      for (const variant of p.variants) {
        variantStockEdits[variant.id] = variant.stock
      }
    }
  },
  { immediate: true },
)

async function handleUpdateVariantStock(variantId: string) {
  if (!productId.value) {
    return
  }
  try {
    product.value = await updateVariant(productId.value, variantId, { stock: variantStockEdits[variantId] })
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể cập nhật tồn kho.')
  }
}

async function handleDeleteVariant(variantId: string) {
  if (!productId.value || !confirm('Xóa SKU này?')) {
    return
  }
  try {
    product.value = await deleteVariant(productId.value, variantId)
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể xóa SKU.')
  }
}

onMounted(async () => {
  categories.value = await getCategories()
  if (productId.value) {
    await loadProduct()
  } else {
    isLoading.value = false
  }
})
</script>

<template>
  <div class="admin-page">
    <div class="admin-page__header">
      <h1>{{ isEditMode ? 'Sửa sản phẩm' : 'Thêm sản phẩm' }}</h1>
    </div>

    <p v-if="isLoading" class="state-message">Đang tải...</p>
    <p v-else-if="loadErrorMessage" class="state-message">{{ loadErrorMessage }}</p>
    <template v-else>
      <div class="admin-section">
        <h2>Thông tin cơ bản</h2>
        <p v-if="saveErrorMessage" class="form-error">{{ saveErrorMessage }}</p>
        <label class="form-field">
          <span>Tên sản phẩm</span>
          <input v-model="form.name" type="text" />
        </label>
        <label class="form-field">
          <span>Mô tả</span>
          <textarea v-model="form.description"></textarea>
        </label>
        <label class="form-field">
          <span>Giá</span>
          <input v-model.number="form.price" type="number" min="0" />
        </label>
        <label class="form-field">
          <span>Tồn kho{{ product?.variants.length ? ' (tự tính từ SKU bên dưới)' : '' }}</span>
          <input v-model.number="form.stock" type="number" min="0" :disabled="!!product?.variants.length" />
        </label>
        <label class="form-field">
          <span>Danh mục</span>
          <select v-model="form.categoryId">
            <option value="" disabled>Chọn danh mục</option>
            <option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</option>
          </select>
        </label>
        <button type="button" class="btn-primary" :disabled="isSaving" @click="submitBasicInfo">Lưu</button>
      </div>

      <template v-if="isEditMode && product">
        <div class="admin-section">
          <h2>Ảnh sản phẩm</h2>
          <img v-if="resolveImageUrl(product.imageUrl)" :src="resolveImageUrl(product.imageUrl)!" class="product-image-preview" alt="" />
          <input type="file" accept="image/*" @change="handleImageChange" />
        </div>

        <div class="admin-section">
          <h2>Phân loại sản phẩm</h2>

          <div v-for="option in sortedOptions" :key="option.id" class="variant-option-block">
            <div class="variant-option-block__header">
              <strong>{{ option.name }}</strong>
              <button type="button" class="btn-link btn-link--danger" @click="handleDeleteOption(option.id)">Xóa phân loại</button>
            </div>
            <div class="variant-value-list">
              <div v-for="value in option.values" :key="value.id" class="variant-value-chip">
                <img v-if="option.id === firstOption?.id && resolveImageUrl(value.imageUrl)" :src="resolveImageUrl(value.imageUrl)!" alt="" />
                <span>{{ value.value }}</span>
                <label v-if="option.id === firstOption?.id" class="variant-value-chip__upload">
                  Ảnh
                  <input type="file" accept="image/*" @change="handleValueImageChange(option.id, value.id, $event)" />
                </label>
                <button type="button" class="btn-link btn-link--danger" @click="handleDeleteValue(option.id, value.id)">×</button>
              </div>
            </div>
            <div class="variant-value-add">
              <input v-model="newValueByOption[option.id]" type="text" placeholder="Giá trị mới..." />
              <button type="button" class="btn-secondary" @click="handleAddValue(option.id)">Thêm giá trị</button>
            </div>
          </div>

          <div v-if="sortedOptions.length < 2" class="variant-option-add">
            <p>+ Thêm loại phân loại (ví dụ: Màu sắc, Dung lượng — tối đa 2 loại)</p>
            <input v-model="newOptionName" type="text" placeholder="Tên phân loại (VD: Màu sắc)" />
            <input v-model="newOptionValues" type="text" placeholder="Các giá trị, phân tách bằng dấu phẩy (VD: Đen, Trắng)" />
            <button type="button" class="btn-secondary" @click="handleAddOption">Thêm phân loại</button>
          </div>
        </div>

        <div v-if="firstOption" class="admin-section">
          <h2>SKU &amp; tồn kho</h2>
          <table class="admin-table">
            <thead>
              <tr>
                <th>{{ firstOption.name }}</th>
                <th v-if="secondOption">{{ secondOption.name }}</th>
                <th>Tồn kho</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="variant in product.variants" :key="variant.id">
                <td>{{ variant.optionValue1Text }}</td>
                <td v-if="secondOption">{{ variant.optionValue2Text ?? '—' }}</td>
                <td>
                  <input v-model.number="variantStockEdits[variant.id]" type="number" min="0" class="variant-stock-input" />
                </td>
                <td class="admin-table__actions">
                  <button type="button" class="btn-link" @click="handleUpdateVariantStock(variant.id)">Lưu</button>
                  <button type="button" class="btn-link btn-link--danger" @click="handleDeleteVariant(variant.id)">Xóa</button>
                </td>
              </tr>
              <tr v-if="product.variants.length === 0">
                <td :colspan="secondOption ? 4 : 3" class="state-message">Chưa có SKU nào.</td>
              </tr>
            </tbody>
          </table>

          <div class="variant-sku-add">
            <select v-model="newSkuValue1">
              <option value="" disabled>{{ firstOption.name }}</option>
              <option v-for="value in firstOption.values" :key="value.id" :value="value.id">{{ value.value }}</option>
            </select>
            <select v-if="secondOption" v-model="newSkuValue2">
              <option value="" disabled>{{ secondOption.name }}</option>
              <option v-for="value in secondOption.values" :key="value.id" :value="value.id">{{ value.value }}</option>
            </select>
            <input v-model.number="newSkuStock" type="number" min="0" placeholder="Tồn kho" class="variant-stock-input" />
            <button type="button" class="btn-secondary" @click="handleAddVariant">+ Thêm SKU</button>
          </div>
        </div>
      </template>
      <p v-else class="state-message">Lưu thông tin cơ bản trước để thêm ảnh và phân loại.</p>
    </template>
  </div>
</template>

<style scoped>
.product-image-preview {
  display: block;
  width: 120px;
  height: 120px;
  object-fit: cover;
  margin-bottom: 12px;
  border-radius: 2px;
  background: var(--bg-page);
}

.variant-option-block {
  margin-bottom: 20px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--border);
}

.variant-option-block__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
}

.variant-value-list {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-bottom: 10px;
}

.variant-value-chip {
  display: flex;
  align-items: center;
  gap: 6px;
  border: 1px solid var(--border);
  border-radius: 2px;
  padding: 6px 10px;
  font-size: 13px;
}

.variant-value-chip img {
  width: 24px;
  height: 24px;
  object-fit: cover;
  border-radius: 2px;
}

.variant-value-chip__upload {
  position: relative;
  color: var(--shopee-orange);
  cursor: pointer;
  font-size: 12px;
}

.variant-value-chip__upload input {
  position: absolute;
  inset: 0;
  opacity: 0;
  cursor: pointer;
}

.variant-value-add,
.variant-option-add,
.variant-sku-add {
  display: flex;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
}

.variant-option-add {
  flex-direction: column;
  align-items: flex-start;
}

.variant-option-add input {
  width: 320px;
  padding: 8px 10px;
  border: 1px solid var(--border);
  border-radius: 2px;
}

.variant-value-add input,
.variant-sku-add select,
.variant-sku-add input {
  padding: 8px 10px;
  border: 1px solid var(--border);
  border-radius: 2px;
}

.variant-stock-input {
  width: 90px;
}
</style>
