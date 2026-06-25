import apiClient from './axios'
import type { Category, CreateCategoryRequest, UpdateCategoryRequest } from '../types/product'

export function getCategories() {
  return apiClient.get<Category[]>('/categories').then((response) => response.data)
}

export function createCategory(request: CreateCategoryRequest) {
  return apiClient.post<Category>('/categories', request).then((response) => response.data)
}

export function updateCategory(id: string, request: UpdateCategoryRequest) {
  return apiClient.put<Category>(`/categories/${id}`, request).then((response) => response.data)
}

export function deleteCategory(id: string) {
  return apiClient.delete(`/categories/${id}`)
}
