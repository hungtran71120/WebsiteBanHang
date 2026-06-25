import apiClient from './axios'
import type { Wishlist } from '../types/wishlist'

export function getWishlist() {
  return apiClient.get<Wishlist>('/wishlist').then((response) => response.data)
}

export function addWishlistItem(productId: string) {
  return apiClient.post<Wishlist>('/wishlist/items', { productId }).then((response) => response.data)
}

export function removeWishlistItem(productId: string) {
  return apiClient.delete<Wishlist>(`/wishlist/items/${productId}`).then((response) => response.data)
}
