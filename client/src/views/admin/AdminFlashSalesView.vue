<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import {
  addFlashSaleItem,
  createFlashSale,
  deleteFlashSale,
  deleteFlashSaleItem,
  getFlashSaleById,
  getFlashSales,
  updateFlashSaleItem,
} from '../../api/flashSales'
import { getProducts } from '../../api/products'
import type { CreateFlashSaleRequest, FlashSale, FlashSaleItem } from '../../types/flashSale'
import type { Product } from '../../types/product'

const flashSales = ref<FlashSale[]>([])
const page = ref(1)
const totalPages = ref(1)
const isLoading = ref(true)
const errorMessage = ref('')
const pageSize = 10

const isFormOpen = ref(false)
const formErrorMessage = ref('')
const isSaving = ref(false)
const form = ref<CreateFlashSaleRequest>(emptyForm())

function pad(value: number) {
  return String(value).padStart(2, '0')
}

function toLocalInput(date: Date) {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function emptyForm(): CreateFlashSaleRequest {
  const now = Date.now()
  return {
    name: '',
    startsAt: toLocalInput(new Date(now + 60 * 60 * 1000)),
    endsAt: toLocalInput(new Date(now + 24 * 60 * 60 * 1000)),
    isActive: true,
  }
}

async function load() {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const result = await getFlashSales(page.value, pageSize)
    flashSales.value = result.items
    totalPages.value = result.totalPages || 1
  } catch {
    errorMessage.value = 'Không thể tải danh sách flash sale.'
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

function statusOf(flashSale: FlashSale) {
  const now = Date.now()
  const starts = new Date(flashSale.startsAt).getTime()
  const ends = new Date(flashSale.endsAt).getTime()
  if (!flashSale.isActive) {
    return { label: 'Đã tắt', cls: 'status-badge--cancelled' }
  }
  if (now < starts) {
    return { label: 'Sắp diễn ra', cls: 'status-badge--pending' }
  }
  if (now > ends) {
    return { label: 'Đã kết thúc', cls: 'status-badge--delivered' }
  }
  return { label: 'Đang diễn ra', cls: 'status-badge--confirmed' }
}

function openCreateForm() {
  form.value = emptyForm()
  formErrorMessage.value = ''
  isFormOpen.value = true
}

function closeForm() {
  isFormOpen.value = false
}

async function submitForm() {
  if (!form.value.name.trim()) {
    formErrorMessage.value = 'Vui lòng nhập tên chương trình.'
    return
  }

  isSaving.value = true
  formErrorMessage.value = ''
  try {
    await createFlashSale({
      ...form.value,
      name: form.value.name.trim(),
      startsAt: new Date(form.value.startsAt).toISOString(),
      endsAt: new Date(form.value.endsAt).toISOString(),
    })
    isFormOpen.value = false
    await load()
  } catch (err: any) {
    formErrorMessage.value = err?.response?.data?.[0] ?? 'Không thể tạo flash sale.'
  } finally {
    isSaving.value = false
  }
}

async function handleDelete(flashSale: FlashSale) {
  if (!confirm(`Xóa chương trình "${flashSale.name}"?`)) {
    return
  }
  try {
    await deleteFlashSale(flashSale.id)
    await load()
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể xóa.')
  }
}

const isManageOpen = ref(false)
const managingFlashSale = ref<FlashSale | null>(null)
const manageErrorMessage = ref('')
const allProducts = ref<Product[]>([])

const newItemProductId = ref('')
const newItemSalePrice = ref(0)
const newItemQuantityLimit = ref(10)
const isAddingItem = ref(false)

const editingItemId = ref<string | null>(null)
const editItemSalePrice = ref(0)
const editItemQuantityLimit = ref(0)

const availableProducts = computed(() => {
  const usedIds = new Set(managingFlashSale.value?.items.map((i) => i.productId) ?? [])
  return allProducts.value.filter((p) => !usedIds.has(p.id))
})

async function openManage(flashSale: FlashSale) {
  managingFlashSale.value = flashSale
  manageErrorMessage.value = ''
  newItemProductId.value = ''
  newItemSalePrice.value = 0
  newItemQuantityLimit.value = 10
  editingItemId.value = null
  isManageOpen.value = true

  if (allProducts.value.length === 0) {
    try {
      const result = await getProducts({ page: 1, pageSize: 100 })
      allProducts.value = result.items
    } catch {
      allProducts.value = []
    }
  }
}

function closeManage() {
  isManageOpen.value = false
  managingFlashSale.value = null
}

async function reloadManaging() {
  if (!managingFlashSale.value) {
    return
  }
  const updated = await getFlashSaleById(managingFlashSale.value.id)
  managingFlashSale.value = updated
  const index = flashSales.value.findIndex((f) => f.id === updated.id)
  if (index !== -1) {
    flashSales.value[index] = updated
  }
}

async function submitAddItem() {
  if (!managingFlashSale.value || !newItemProductId.value) {
    manageErrorMessage.value = 'Vui lòng chọn sản phẩm.'
    return
  }

  isAddingItem.value = true
  manageErrorMessage.value = ''
  try {
    await addFlashSaleItem(managingFlashSale.value.id, {
      productId: newItemProductId.value,
      salePrice: newItemSalePrice.value,
      quantityLimit: newItemQuantityLimit.value,
    })
    newItemProductId.value = ''
    newItemSalePrice.value = 0
    newItemQuantityLimit.value = 10
    await reloadManaging()
  } catch (err: any) {
    manageErrorMessage.value = err?.response?.data?.[0] ?? 'Không thể thêm sản phẩm.'
  } finally {
    isAddingItem.value = false
  }
}

function startEditItem(item: FlashSaleItem) {
  editingItemId.value = item.id
  editItemSalePrice.value = item.salePrice
  editItemQuantityLimit.value = item.quantityLimit
}

function cancelEditItem() {
  editingItemId.value = null
}

async function submitEditItem() {
  if (!managingFlashSale.value || !editingItemId.value) {
    return
  }
  try {
    await updateFlashSaleItem(managingFlashSale.value.id, editingItemId.value, {
      salePrice: editItemSalePrice.value,
      quantityLimit: editItemQuantityLimit.value,
    })
    editingItemId.value = null
    await reloadManaging()
  } catch (err: any) {
    manageErrorMessage.value = err?.response?.data?.[0] ?? 'Không thể cập nhật.'
  }
}

async function handleDeleteItem(item: FlashSaleItem) {
  if (!managingFlashSale.value || !confirm(`Xóa "${item.productName}" khỏi flash sale?`)) {
    return
  }
  try {
    await deleteFlashSaleItem(managingFlashSale.value.id, item.id)
    await reloadManaging()
  } catch (err: any) {
    manageErrorMessage.value = err?.response?.data?.[0] ?? 'Không thể xóa.'
  }
}

onMounted(load)
</script>

<template>
  <div class="admin-page">
    <div class="admin-page__header">
      <h1>Flash Sale</h1>
      <button type="button" class="btn-primary" @click="openCreateForm">+ Tạo chương trình</button>
    </div>

    <p v-if="isLoading" class="state-message">Đang tải...</p>
    <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>
    <template v-else>
      <table class="admin-table">
        <thead>
          <tr>
            <th>Tên chương trình</th>
            <th>Bắt đầu</th>
            <th>Kết thúc</th>
            <th>Sản phẩm</th>
            <th>Trạng thái</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="flashSale in flashSales" :key="flashSale.id">
            <td>{{ flashSale.name }}</td>
            <td>{{ new Date(flashSale.startsAt).toLocaleString('vi-VN') }}</td>
            <td>{{ new Date(flashSale.endsAt).toLocaleString('vi-VN') }}</td>
            <td>{{ flashSale.items.length }}</td>
            <td><span class="status-badge" :class="statusOf(flashSale).cls">{{ statusOf(flashSale).label }}</span></td>
            <td class="admin-table__actions">
              <button type="button" class="btn-link" @click="openManage(flashSale)">Quản lý sản phẩm</button>
              <button type="button" class="btn-link btn-link--danger" @click="handleDelete(flashSale)">Xóa</button>
            </td>
          </tr>
          <tr v-if="flashSales.length === 0">
            <td colspan="6" class="state-message">Chưa có chương trình flash sale nào.</td>
          </tr>
        </tbody>
      </table>

      <div v-if="totalPages > 1" class="pagination">
        <button :disabled="page === 1" @click="goToPage(page - 1)">‹</button>
        <span>{{ page }}/{{ totalPages }}</span>
        <button :disabled="page === totalPages" @click="goToPage(page + 1)">›</button>
      </div>
    </template>

    <div v-if="isFormOpen" class="modal-overlay" @click.self="closeForm">
      <div class="modal-box">
        <h2>Tạo chương trình flash sale</h2>
        <p v-if="formErrorMessage" class="form-error">{{ formErrorMessage }}</p>
        <label class="form-field">
          <span>Tên chương trình</span>
          <input v-model="form.name" type="text" placeholder="VD: Flash Sale Giữa Tháng" />
        </label>
        <label class="form-field">
          <span>Thời gian bắt đầu</span>
          <input v-model="form.startsAt" type="datetime-local" />
        </label>
        <label class="form-field">
          <span>Thời gian kết thúc</span>
          <input v-model="form.endsAt" type="datetime-local" />
        </label>
        <label class="form-field form-field--checkbox">
          <input v-model="form.isActive" type="checkbox" />
          <span>Đang hoạt động</span>
        </label>
        <div class="modal-actions">
          <button type="button" class="btn-secondary" @click="closeForm">Hủy</button>
          <button type="button" class="btn-primary" :disabled="isSaving" @click="submitForm">Lưu</button>
        </div>
      </div>
    </div>

    <div v-if="isManageOpen && managingFlashSale" class="modal-overlay" @click.self="closeManage">
      <div class="modal-box modal-box--wide">
        <h2>Sản phẩm trong "{{ managingFlashSale.name }}"</h2>
        <p v-if="manageErrorMessage" class="form-error">{{ manageErrorMessage }}</p>

        <table class="admin-table">
          <thead>
            <tr>
              <th>Sản phẩm</th>
              <th>Giá gốc</th>
              <th>Giá sale</th>
              <th>Đã bán / Giới hạn</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in managingFlashSale.items" :key="item.id">
              <td>{{ item.productName }}</td>
              <td>₫{{ item.originalPrice.toLocaleString('vi-VN') }}</td>
              <td>
                <template v-if="editingItemId === item.id">
                  <input v-model.number="editItemSalePrice" type="number" min="0" class="inline-input" />
                </template>
                <template v-else>₫{{ item.salePrice.toLocaleString('vi-VN') }}</template>
              </td>
              <td>
                <template v-if="editingItemId === item.id">
                  <input v-model.number="editItemQuantityLimit" type="number" min="1" class="inline-input" />
                </template>
                <template v-else>{{ item.quantitySold }} / {{ item.quantityLimit }}</template>
              </td>
              <td class="admin-table__actions">
                <template v-if="editingItemId === item.id">
                  <button type="button" class="btn-link" @click="submitEditItem">Lưu</button>
                  <button type="button" class="btn-link" @click="cancelEditItem">Hủy</button>
                </template>
                <template v-else>
                  <button type="button" class="btn-link" @click="startEditItem(item)">Sửa</button>
                  <button type="button" class="btn-link btn-link--danger" @click="handleDeleteItem(item)">Xóa</button>
                </template>
              </td>
            </tr>
            <tr v-if="managingFlashSale.items.length === 0">
              <td colspan="5" class="state-message">Chưa có sản phẩm nào.</td>
            </tr>
          </tbody>
        </table>

        <div class="add-item-form">
          <label class="form-field">
            <span>Sản phẩm</span>
            <select v-model="newItemProductId">
              <option value="">-- Chọn sản phẩm --</option>
              <option v-for="p in availableProducts" :key="p.id" :value="p.id">{{ p.name }} (₫{{ p.price.toLocaleString('vi-VN') }})</option>
            </select>
          </label>
          <label class="form-field">
            <span>Giá sale</span>
            <input v-model.number="newItemSalePrice" type="number" min="0" />
          </label>
          <label class="form-field">
            <span>Giới hạn số lượng</span>
            <input v-model.number="newItemQuantityLimit" type="number" min="1" />
          </label>
          <button type="button" class="btn-primary" :disabled="isAddingItem" @click="submitAddItem">+ Thêm vào flash sale</button>
        </div>

        <div class="modal-actions">
          <button type="button" class="btn-secondary" @click="closeManage">Đóng</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.form-field--checkbox {
  flex-direction: row;
  align-items: center;
  gap: 8px;
}

.modal-box--wide {
  width: 720px;
  max-width: 90vw;
}

.inline-input {
  width: 90px;
  padding: 4px 6px;
}

.add-item-form {
  display: flex;
  align-items: flex-end;
  gap: 12px;
  margin-top: 16px;
  flex-wrap: wrap;
}

.add-item-form .form-field {
  flex: 1;
  min-width: 160px;
}
</style>
