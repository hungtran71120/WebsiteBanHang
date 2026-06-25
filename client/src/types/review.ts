export interface Review {
  id: string
  productId: string
  userId: string
  userName: string
  rating: number
  comment: string
  createdAt: string
}

export interface CreateReviewRequest {
  rating: number
  comment: string
}

export interface UpdateReviewRequest {
  rating: number
  comment: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
