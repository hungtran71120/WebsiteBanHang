import apiClient from './axios'
import type { PagedResult } from '../types/voucher'
import type {
  AddFlashSaleItemRequest,
  CreateFlashSaleRequest,
  FlashSale,
  UpdateFlashSaleItemRequest,
} from '../types/flashSale'

export function getActiveFlashSale() {
  return apiClient.get<FlashSale | null>('/flash-sales/active').then((response) => response.data)
}

export function getFlashSales(page: number, pageSize: number) {
  return apiClient
    .get<PagedResult<FlashSale>>('/flash-sales', { params: { page, pageSize } })
    .then((response) => response.data)
}

export function getFlashSaleById(id: string) {
  return apiClient.get<FlashSale>(`/flash-sales/${id}`).then((response) => response.data)
}

export function createFlashSale(request: CreateFlashSaleRequest) {
  return apiClient.post<FlashSale>('/flash-sales', request).then((response) => response.data)
}

export function deleteFlashSale(id: string) {
  return apiClient.delete(`/flash-sales/${id}`)
}

export function addFlashSaleItem(flashSaleId: string, request: AddFlashSaleItemRequest) {
  return apiClient.post<FlashSale>(`/flash-sales/${flashSaleId}/items`, request).then((response) => response.data)
}

export function updateFlashSaleItem(flashSaleId: string, itemId: string, request: UpdateFlashSaleItemRequest) {
  return apiClient
    .put<FlashSale>(`/flash-sales/${flashSaleId}/items/${itemId}`, request)
    .then((response) => response.data)
}

export function deleteFlashSaleItem(flashSaleId: string, itemId: string) {
  return apiClient.delete<FlashSale>(`/flash-sales/${flashSaleId}/items/${itemId}`).then((response) => response.data)
}
