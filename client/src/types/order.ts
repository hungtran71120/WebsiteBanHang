export type OrderStatus = 'Pending' | 'Confirmed' | 'Shipped' | 'Delivered' | 'Cancelled'
export type PaymentMethod = 'Cod' | 'MockPaid'

export interface OrderItem {
  productId: string
  productName: string
  imageUrl: string | null
  variantDescription: string | null
  unitPrice: number
  quantity: number
  lineTotal: number
}

export interface OrderStatusHistoryEntry {
  status: OrderStatus
  createdAt: string
}

export interface Order {
  id: string
  status: OrderStatus
  paymentMethod: PaymentMethod
  isPaid: boolean
  shippingAddress: string
  subtotal: number
  discountAmount: number
  voucherCode: string | null
  totalAmount: number
  createdAt: string
  items: OrderItem[]
  statusHistory: OrderStatusHistoryEntry[]
}

export interface OrderSummary {
  id: string
  status: OrderStatus
  totalAmount: number
  itemCount: number
  createdAt: string
  items: OrderItem[]
}

export interface CreateOrderRequest {
  shippingAddress: string
  paymentMethod: PaymentMethod
  voucherCode?: string | null
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
