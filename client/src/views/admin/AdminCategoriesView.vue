<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { createCategory, deleteCategory, getCategories, updateCategory } from '../../api/categories'
import type { Category } from '../../types/product'

const categories = ref<Category[]>([])
const isLoading = ref(true)
const errorMessage = ref('')

const isFormOpen = ref(false)
const editingId = ref<string | null>(null)
const formName = ref('')
const formParentId = ref('')
const formErrorMessage = ref('')
const isSaving = ref(false)

const parentOptions = computed(() => categories.value.filter((c) => c.id !== editingId.value))

async function load() {
  isLoading.value = true
  errorMessage.value = ''
  try {
    categories.value = await getCategories()
  } catch {
    errorMessage.value = 'Không thể tải danh mục.'
  } finally {
    isLoading.value = false
  }
}

function openCreateForm() {
  editingId.value = null
  formName.value = ''
  formParentId.value = ''
  formErrorMessage.value = ''
  isFormOpen.value = true
}

function openEditForm(category: Category) {
  editingId.value = category.id
  formName.value = category.name
  formParentId.value = category.parentCategoryId ?? ''
  formErrorMessage.value = ''
  isFormOpen.value = true
}

function closeForm() {
  isFormOpen.value = false
}

async function submitForm() {
  if (!formName.value.trim()) {
    formErrorMessage.value = 'Vui lòng nhập tên danh mục.'
    return
  }

  isSaving.value = true
  formErrorMessage.value = ''
  try {
    const request = { name: formName.value.trim(), parentCategoryId: formParentId.value || null }
    if (editingId.value) {
      await updateCategory(editingId.value, request)
    } else {
      await createCategory(request)
    }
    isFormOpen.value = false
    await load()
  } catch (err: any) {
    formErrorMessage.value = err?.response?.data?.[0] ?? 'Không thể lưu danh mục.'
  } finally {
    isSaving.value = false
  }
}

async function handleDelete(category: Category) {
  if (!confirm(`Xóa danh mục "${category.name}"?`)) {
    return
  }
  try {
    await deleteCategory(category.id)
    await load()
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể xóa danh mục.')
  }
}

onMounted(load)
</script>

<template>
  <div class="admin-page">
    <div class="admin-page__header">
      <h1>Danh mục</h1>
      <button type="button" class="btn-primary" @click="openCreateForm">+ Thêm danh mục</button>
    </div>

    <p v-if="isLoading" class="state-message">Đang tải...</p>
    <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>
    <table v-else class="admin-table">
      <thead>
        <tr>
          <th>Tên danh mục</th>
          <th>Danh mục cha</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="category in categories" :key="category.id">
          <td>{{ category.name }}</td>
          <td>{{ category.parentCategoryName ?? '—' }}</td>
          <td class="admin-table__actions">
            <button type="button" class="btn-link" @click="openEditForm(category)">Sửa</button>
            <button type="button" class="btn-link btn-link--danger" @click="handleDelete(category)">Xóa</button>
          </td>
        </tr>
        <tr v-if="categories.length === 0">
          <td colspan="3" class="state-message">Chưa có danh mục nào.</td>
        </tr>
      </tbody>
    </table>

    <div v-if="isFormOpen" class="modal-overlay" @click.self="closeForm">
      <div class="modal-box">
        <h2>{{ editingId ? 'Sửa danh mục' : 'Thêm danh mục' }}</h2>
        <p v-if="formErrorMessage" class="form-error">{{ formErrorMessage }}</p>
        <label class="form-field">
          <span>Tên danh mục</span>
          <input v-model="formName" type="text" />
        </label>
        <label class="form-field">
          <span>Danh mục cha</span>
          <select v-model="formParentId">
            <option value="">(Không có)</option>
            <option v-for="parent in parentOptions" :key="parent.id" :value="parent.id">{{ parent.name }}</option>
          </select>
        </label>
        <div class="modal-actions">
          <button type="button" class="btn-secondary" @click="closeForm">Hủy</button>
          <button type="button" class="btn-primary" :disabled="isSaving" @click="submitForm">Lưu</button>
        </div>
      </div>
    </div>
  </div>
</template>

