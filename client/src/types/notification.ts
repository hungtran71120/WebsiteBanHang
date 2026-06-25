export interface AppNotification {
  id: string
  orderId: string
  message: string
  isRead: boolean
  createdAt: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
