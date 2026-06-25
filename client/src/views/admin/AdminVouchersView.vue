<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { createVoucher, deleteVoucher, getVouchers, updateVoucher } from '../../api/vouchers'
import type { CreateVoucherRequest, Voucher } from '../../types/voucher'

const vouchers = ref<Voucher[]>([])
const page = ref(1)
const totalPages = ref(1)
const isLoading = ref(true)
const errorMessage = ref('')
const pageSize = 10

const isFormOpen = ref(false)
const editingId = ref<string | null>(null)
const formErrorMessage = ref('')
const isSaving = ref(false)

const form = ref<CreateVoucherRequest>(emptyForm())

function emptyForm(): CreateVoucherRequest {
  return {
    code: '',
    discountType: 'Percentage',
    discountValue: 10,
    minOrderAmount: 0,
    maxUsageCount: null,
    maxUsagePerUser: 1,
    expiresAt: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10),
    isActive: true,
  }
}

async function load() {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const result = await getVouchers(page.value, pageSize)
    vouchers.value = result.items
    totalPages.value = result.totalPages || 1
  } catch {
    errorMessage.value = 'Không thể tải danh sách voucher.'
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

function openCreateForm() {
  editingId.value = null
  form.value = emptyForm()
  formErrorMessage.value = ''
  isFormOpen.value = true
}

function openEditForm(voucher: Voucher) {
  editingId.value = voucher.id
  form.value = {
    code: voucher.code,
    discountType: voucher.discountType,
    discountValue: voucher.discountValue,
    minOrderAmount: voucher.minOrderAmount,
    maxUsageCount: voucher.maxUsageCount,
    maxUsagePerUser: voucher.maxUsagePerUser,
    expiresAt: voucher.expiresAt.slice(0, 10),
    isActive: voucher.isActive,
  }
  formErrorMessage.value = ''
  isFormOpen.value = true
}

function closeForm() {
  isFormOpen.value = false
}

async function submitForm() {
  if (!form.value.code.trim()) {
    formErrorMessage.value = 'Vui lòng nhập mã giảm giá.'
    return
  }

  isSaving.value = true
  formErrorMessage.value = ''
  try {
    const request: CreateVoucherRequest = {
      ...form.value,
      code: form.value.code.trim().toUpperCase(),
      expiresAt: new Date(form.value.expiresAt).toISOString(),
      maxUsageCount: form.value.maxUsageCount || null,
    }
    if (editingId.value) {
      await updateVoucher(editingId.value, request)
    } else {
      await createVoucher(request)
    }
    isFormOpen.value = false
    await load()
  } catch (err: any) {
    formErrorMessage.value = err?.response?.data?.[0] ?? 'Không thể lưu voucher.'
  } finally {
    isSaving.value = false
  }
}

async function handleDelete(voucher: Voucher) {
  if (!confirm(`Xóa voucher "${voucher.code}"?`)) {
    return
  }
  try {
    await deleteVoucher(voucher.id)
    await load()
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể xóa voucher.')
  }
}

function discountLabel(voucher: Voucher) {
  return voucher.discountType === 'Percentage'
    ? `${voucher.discountValue}%`
    : `₫${voucher.discountValue.toLocaleString('vi-VN')}`
}

onMounted(load)
</script>

<template>
  <div class="admin-page">
    <div class="admin-page__header">
      <h1>Voucher</h1>
      <button type="button" class="btn-primary" @click="openCreateForm">+ Thêm voucher</button>
    </div>

    <p v-if="isLoading" class="state-message">Đang tải...</p>
    <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>
    <template v-else>
      <table class="admin-table">
        <thead>
          <tr>
            <th>Mã</th>
            <th>Giảm giá</th>
            <th>Đơn tối thiểu</th>
            <th>Đã dùng / Giới hạn</th>
            <th>Hết hạn</th>
            <th>Trạng thái</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="voucher in vouchers" :key="voucher.id">
            <td>{{ voucher.code }}</td>
            <td>{{ discountLabel(voucher) }}</td>
            <td>₫{{ voucher.minOrderAmount.toLocaleString('vi-VN') }}</td>
            <td>{{ voucher.usedCount }} / {{ voucher.maxUsageCount ?? '∞' }}</td>
            <td>{{ new Date(voucher.expiresAt).toLocaleDateString('vi-VN') }}</td>
            <td>{{ voucher.isActive ? 'Đang hoạt động' : 'Đã tắt' }}</td>
            <td class="admin-table__actions">
              <button type="button" class="btn-link" @click="openEditForm(voucher)">Sửa</button>
              <button type="button" class="btn-link btn-link--danger" @click="handleDelete(voucher)">Xóa</button>
            </td>
          </tr>
          <tr v-if="vouchers.length === 0">
            <td colspan="7" class="state-message">Chưa có voucher nào.</td>
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
        <h2>{{ editingId ? 'Sửa voucher' : 'Thêm voucher' }}</h2>
        <p v-if="formErrorMessage" class="form-error">{{ formErrorMessage }}</p>
        <label class="form-field">
          <span>Mã giảm giá</span>
          <input v-model="form.code" type="text" placeholder="VD: SALE10" />
        </label>
        <label class="form-field">
          <span>Loại giảm giá</span>
          <select v-model="form.discountType">
            <option value="Percentage">Theo phần trăm (%)</option>
            <option value="FixedAmount">Số tiền cố định (₫)</option>
          </select>
        </label>
        <label class="form-field">
          <span>Giá trị giảm</span>
          <input v-model.number="form.discountValue" type="number" min="0" />
        </label>
        <label class="form-field">
          <span>Giá trị đơn hàng tối thiểu</span>
          <input v-model.number="form.minOrderAmount" type="number" min="0" />
        </label>
        <label class="form-field">
          <span>Giới hạn tổng số lần dùng (để trống = không giới hạn)</span>
          <input v-model.number="form.maxUsageCount" type="number" min="1" />
        </label>
        <label class="form-field">
          <span>Giới hạn số lần dùng / khách</span>
          <input v-model.number="form.maxUsagePerUser" type="number" min="1" />
        </label>
        <label class="form-field">
          <span>Ngày hết hạn</span>
          <input v-model="form.expiresAt" type="date" />
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
  </div>
</template>

<style scoped>
.form-field--checkbox {
  flex-direction: row;
  align-items: center;
  gap: 8px;
}
</style>
