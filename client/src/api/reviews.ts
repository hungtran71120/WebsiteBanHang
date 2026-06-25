import apiClient from './axios'
import type { CreateReviewRequest, PagedResult, Review, UpdateReviewRequest } from '../types/review'

export function getProductReviews(productId: string, page: number, pageSize: number) {
  return apiClient
    .get<PagedResult<Review>>(`/products/${productId}/reviews`, { params: { page, pageSize } })
    .then((response) => response.data)
}

export function createReview(productId: string, request: CreateReviewRequest) {
  return apiClient.post<Review>(`/products/${productId}/reviews`, request).then((response) => response.data)
}

export function updateReview(reviewId: string, request: UpdateReviewRequest) {
  return apiClient.put<Review>(`/reviews/${reviewId}`, request).then((response) => response.data)
}

export function deleteReview(reviewId: string) {
  return apiClient.delete(`/reviews/${reviewId}`)
}
