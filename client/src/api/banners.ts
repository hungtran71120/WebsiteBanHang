import apiClient from './axios'
import type { Banner, CreateBannerRequest, UpdateBannerRequest } from '../types/banner'

export function getActiveBanners() {
  return apiClient.get<Banner[]>('/banners/active').then((response) => response.data)
}

export function getBanners() {
  return apiClient.get<Banner[]>('/banners').then((response) => response.data)
}

export function createBanner(request: CreateBannerRequest) {
  return apiClient.post<Banner>('/banners', request).then((response) => response.data)
}

export function updateBanner(id: string, request: UpdateBannerRequest) {
  return apiClient.put<Banner>(`/banners/${id}`, request).then((response) => response.data)
}

export function deleteBanner(id: string) {
  return apiClient.delete(`/banners/${id}`)
}

export function uploadBannerImage(id: string, file: File) {
  const formData = new FormData()
  formData.append('file', file)
  return apiClient
    .post<Banner>(`/banners/${id}/image`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    .then((response) => response.data)
}
