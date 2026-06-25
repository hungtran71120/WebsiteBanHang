import type { OrderStatus } from './order'

export interface AdminUser {
  id: string
  email: string
  fullName: string
  phoneNumber: string | null
  address: string | null
  role: string
  isLocked: boolean
}

export interface AdminOrderSummary {
  id: string
  customerEmail: string
  status: OrderStatus
  totalAmount: number
  itemCount: number
  createdAt: string
}

export interface DashboardStatistics {
  totalRevenue: number
  totalOrders: number
  totalProducts: number
  totalUsers: number
  ordersByStatus: Record<OrderStatus, number>
}
