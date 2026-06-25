import apiClient from './axios'
import type { Product } from '../types/product'

export function getRecommendations() {
  return apiClient.get<Product[]>('/recommendations').then((response) => response.data)
}
