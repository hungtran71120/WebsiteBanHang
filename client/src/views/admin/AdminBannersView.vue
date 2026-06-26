<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { createBanner, deleteBanner, getBanners, updateBanner, uploadBannerImage } from '../../api/banners'
import type { Banner } from '../../types/banner'
import { resolveImageUrl } from '../../utils/url'

const banners = ref<Banner[]>([])
const isLoading = ref(true)
const errorMessage = ref('')

const isFormOpen = ref(false)
const editingBanner = ref<Banner | null>(null)
const formTitle = ref('')
const formSubtitle = ref('')
const formLinkUrl = ref('')
const formDisplayOrder = ref(0)
const formIsActive = ref(true)
const formErrorMessage = ref('')
const isSaving = ref(false)
const isUploadingImage = ref(false)

async function load() {
  isLoading.value = true
  errorMessage.value = ''
  try {
    banners.value = await getBanners()
  } catch {
    errorMessage.value = 'Không thể tải danh sách banner.'
  } finally {
    isLoading.value = false
  }
}

function openCreateForm() {
  editingBanner.value = null
  formTitle.value = ''
  formSubtitle.value = ''
  formLinkUrl.value = '/products'
  formDisplayOrder.value = banners.value.length
  formIsActive.value = true
  formErrorMessage.value = ''
  isFormOpen.value = true
}

function openEditForm(banner: Banner) {
  editingBanner.value = banner
  formTitle.value = banner.title
  formSubtitle.value = banner.subtitle ?? ''
  formLinkUrl.value = banner.linkUrl
  formDisplayOrder.value = banner.displayOrder
  formIsActive.value = banner.isActive
  formErrorMessage.value = ''
  isFormOpen.value = true
}

function closeForm() {
  isFormOpen.value = false
}

async function submitForm() {
  if (!formTitle.value.trim() || !formLinkUrl.value.trim()) {
    formErrorMessage.value = 'Vui lòng nhập đầy đủ tiêu đề và link.'
    return
  }

  isSaving.value = true
  formErrorMessage.value = ''
  try {
    const request = {
      title: formTitle.value.trim(),
      subtitle: formSubtitle.value.trim() || null,
      linkUrl: formLinkUrl.value.trim(),
      displayOrder: formDisplayOrder.value,
      isActive: formIsActive.value,
    }
    if (editingBanner.value) {
      const updated = await updateBanner(editingBanner.value.id, request)
      editingBanner.value = updated
    } else {
      editingBanner.value = await createBanner(request)
    }
    await load()
  } catch (err: any) {
    formErrorMessage.value = err?.response?.data?.[0] ?? 'Không thể lưu banner.'
  } finally {
    isSaving.value = false
  }
}

async function handleImageChange(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file || !editingBanner.value) {
    return
  }
  isUploadingImage.value = true
  try {
    editingBanner.value = await uploadBannerImage(editingBanner.value.id, file)
    await load()
  } catch {
    alert('Không thể upload ảnh.')
  } finally {
    isUploadingImage.value = false
  }
}

async function handleDelete(banner: Banner) {
  if (!confirm(`Xóa banner "${banner.title}"?`)) {
    return
  }
  try {
    await deleteBanner(banner.id)
    await load()
  } catch (err: any) {
    alert(err?.response?.data?.[0] ?? 'Không thể xóa banner.')
  }
}

onMounted(load)
</script>

<template>
  <div class="admin-page">
    <div class="admin-page__header">
      <h1>Banner trang chủ</h1>
      <button type="button" class="btn-primary" @click="openCreateForm">+ Thêm banner</button>
    </div>

    <p v-if="isLoading" class="state-message">Đang tải...</p>
    <p v-else-if="errorMessage" class="state-message">{{ errorMessage }}</p>
    <table v-else class="admin-table">
      <thead>
        <tr>
          <th>Ảnh</th>
          <th>Tiêu đề</th>
          <th>Link</th>
          <th>Thứ tự</th>
          <th>Trạng thái</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="banner in banners" :key="banner.id">
          <td>
            <img v-if="resolveImageUrl(banner.imageUrl)" :src="resolveImageUrl(banner.imageUrl)!" class="admin-table__thumb" alt="" />
            <div v-else class="admin-table__thumb"></div>
          </td>
          <td>{{ banner.title }}</td>
          <td>{{ banner.linkUrl }}</td>
          <td>{{ banner.displayOrder }}</td>
          <td>
            <span class="status-badge" :class="banner.isActive ? 'status-badge--delivered' : 'status-badge--cancelled'">
              {{ banner.isActive ? 'Đang hiển thị' : 'Đã ẩn' }}
            </span>
          </td>
          <td class="admin-table__actions">
            <button type="button" class="btn-link" @click="openEditForm(banner)">Sửa</button>
            <button type="button" class="btn-link btn-link--danger" @click="handleDelete(banner)">Xóa</button>
          </td>
        </tr>
        <tr v-if="banners.length === 0">
          <td colspan="6" class="state-message">Chưa có banner nào.</td>
        </tr>
      </tbody>
    </table>

    <div v-if="isFormOpen" class="modal-overlay" @click.self="closeForm">
      <div class="modal-box">
        <h2>{{ editingBanner ? 'Sửa banner' : 'Thêm banner' }}</h2>
        <p v-if="formErrorMessage" class="form-error">{{ formErrorMessage }}</p>
        <label class="form-field">
          <span>Tiêu đề</span>
          <input v-model="formTitle" type="text" />
        </label>
        <label class="form-field">
          <span>Mô tả ngắn (không bắt buộc)</span>
          <input v-model="formSubtitle" type="text" />
        </label>
        <label class="form-field">
          <span>Link điều hướng (đường dẫn nội bộ, VD: /products)</span>
          <input v-model="formLinkUrl" type="text" placeholder="/products" />
        </label>
        <label class="form-field">
          <span>Thứ tự hiển thị</span>
          <input v-model.number="formDisplayOrder" type="number" min="0" />
        </label>
        <label class="form-field">
          <span><input v-model="formIsActive" type="checkbox" /> Hiển thị banner này</span>
        </label>

        <div v-if="editingBanner" class="admin-section">
          <h2>Ảnh banner</h2>
          <img v-if="resolveImageUrl(editingBanner.imageUrl)" :src="resolveImageUrl(editingBanner.imageUrl)!" class="banner-image-preview" alt="" />
          <input type="file" accept="image/*" :disabled="isUploadingImage" @change="handleImageChange" />
        </div>
        <p v-else class="admin-table__hint">Lưu banner trước, sau đó bạn có thể upload ảnh.</p>

        <div class="modal-actions">
          <button type="button" class="btn-secondary" @click="closeForm">Đóng</button>
          <button type="button" class="btn-primary" :disabled="isSaving" @click="submitForm">Lưu</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.banner-image-preview {
  display: block;
  width: 100%;
  max-height: 160px;
  object-fit: cover;
  border-radius: var(--radius-sm);
  margin-bottom: 10px;
  background: var(--bg-page);
}
</style>
