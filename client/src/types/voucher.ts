export type VoucherDiscountType = 'Percentage' | 'FixedAmount'

export interface Voucher {
  id: string
  code: string
  discountType: VoucherDiscountType
  discountValue: number
  minOrderAmount: number
  maxUsageCount: number | null
  usedCount: number
  maxUsagePerUser: number
  expiresAt: string
  isActive: boolean
}

export interface CreateVoucherRequest {
  code: string
  discountType: VoucherDiscountType
  discountValue: number
  minOrderAmount: number
  maxUsageCount?: number | null
  maxUsagePerUser: number
  expiresAt: string
  isActive: boolean
}

export type UpdateVoucherRequest = CreateVoucherRequest

export interface VoucherValidationResult {
  voucherId: string
  code: string
  subtotal: number
  discountAmount: number
  finalTotal: number
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
