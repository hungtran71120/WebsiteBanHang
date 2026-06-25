import apiClient from './axios'
import type { CreateOrderRequest, Order, OrderStatus, OrderSummary, PagedResult } from '../types/order'
import type { AdminOrderSummary } from '../types/admin'

export function createOrder(request: CreateOrderRequest) {
  return apiClient.post<Order>('/orders', request).then((response) => response.data)
}

export function getMyOrders(page: number, pageSize: number, status?: OrderStatus) {
  return apiClient
    .get<PagedResult<OrderSummary>>('/orders', { params: { page, pageSize, status } })
    .then((response) => response.data)
}

export function getOrderById(id: string) {
  return apiClient.get<Order>(`/orders/${id}`).then((response) => response.data)
}

export function getAllOrders(page: number, pageSize: number, status?: OrderStatus) {
  return apiClient
    .get<PagedResult<AdminOrderSummary>>('/orders/all', { params: { page, pageSize, status } })
    .then((response) => response.data)
}

export function updateOrderStatus(id: string, status: OrderStatus) {
  return apiClient.put<Order>(`/orders/${id}/status`, { status }).then((response) => response.data)
}

export function confirmDelivery(id: string) {
  return apiClient.post<Order>(`/orders/${id}/confirm-delivery`).then((response) => response.data)
}
