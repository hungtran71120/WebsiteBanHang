import apiClient from './axios'
import type {
  CreateVoucherRequest,
  PagedResult,
  UpdateVoucherRequest,
  Voucher,
  VoucherValidationResult,
} from '../types/voucher'

export function validateVoucher(code: string) {
  return apiClient
    .post<VoucherValidationResult>('/vouchers/validate', { code })
    .then((response) => response.data)
}

export function getAvailableVouchers() {
  return apiClient.get<Voucher[]>('/vouchers/available').then((response) => response.data)
}

export function getVouchers(page: number, pageSize: number) {
  return apiClient
    .get<PagedResult<Voucher>>('/vouchers', { params: { page, pageSize } })
    .then((response) => response.data)
}

export function createVoucher(request: CreateVoucherRequest) {
  return apiClient.post<Voucher>('/vouchers', request).then((response) => response.data)
}

export function updateVoucher(id: string, request: UpdateVoucherRequest) {
  return apiClient.put<Voucher>(`/vouchers/${id}`, request).then((response) => response.data)
}

export function deleteVoucher(id: string) {
  return apiClient.delete(`/vouchers/${id}`)
}
