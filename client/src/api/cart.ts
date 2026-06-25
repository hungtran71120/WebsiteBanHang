import apiClient from './axios'
import type { AddCartItemRequest, Cart, UpdateCartItemRequest } from '../types/cart'

export function getCart() {
  return apiClient.get<Cart>('/cart').then((response) => response.data)
}

export function addCartItem(request: AddCartItemRequest) {
  return apiClient.post<Cart>('/cart/items', request).then((response) => response.data)
}

export function updateCartItem(cartItemId: string, request: UpdateCartItemRequest) {
  return apiClient.put<Cart>(`/cart/items/${cartItemId}`, request).then((response) => response.data)
}

export function removeCartItem(cartItemId: string) {
  return apiClient.delete<Cart>(`/cart/items/${cartItemId}`).then((response) => response.data)
}
