import apiClient from './axios'
import type {
  AddVariantOptionValueRequest,
  CreateProductRequest,
  CreateProductVariantRequest,
  CreateVariantOptionRequest,
  PagedResult,
  Product,
  ProductFilter,
  UpdateProductRequest,
  UpdateProductVariantRequest,
} from '../types/product'

export function getProducts(filter: ProductFilter) {
  return apiClient
    .get<PagedResult<Product>>('/products', { params: filter })
    .then((response) => response.data)
}

export function getProductById(id: string) {
  return apiClient.get<Product>(`/products/${id}`).then((response) => response.data)
}

export function getRelatedProducts(id: string) {
  return apiClient.get<Product[]>(`/products/${id}/related-products`).then((response) => response.data)
}

export function createProduct(request: CreateProductRequest) {
  return apiClient.post<Product>('/products', request).then((response) => response.data)
}

export function updateProduct(id: string, request: UpdateProductRequest) {
  return apiClient.put<Product>(`/products/${id}`, request).then((response) => response.data)
}

export function deleteProduct(id: string) {
  return apiClient.delete(`/products/${id}`)
}

export function uploadProductImage(id: string, file: File) {
  const formData = new FormData()
  formData.append('file', file)
  return apiClient
    .post<Product>(`/products/${id}/image`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    .then((response) => response.data)
}

export function addVariantOption(productId: string, request: CreateVariantOptionRequest) {
  return apiClient
    .post<Product>(`/products/${productId}/variant-options`, request)
    .then((response) => response.data)
}

export function addVariantOptionValue(
  productId: string,
  optionId: string,
  request: AddVariantOptionValueRequest,
) {
  return apiClient
    .post<Product>(`/products/${productId}/variant-options/${optionId}/values`, request)
    .then((response) => response.data)
}

export function uploadVariantOptionValueImage(
  productId: string,
  optionId: string,
  valueId: string,
  file: File,
) {
  const formData = new FormData()
  formData.append('file', file)
  return apiClient
    .post<Product>(
      `/products/${productId}/variant-options/${optionId}/values/${valueId}/image`,
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    )
    .then((response) => response.data)
}

export function deleteVariantOptionValue(productId: string, optionId: string, valueId: string) {
  return apiClient
    .delete<Product>(`/products/${productId}/variant-options/${optionId}/values/${valueId}`)
    .then((response) => response.data)
}

export function deleteVariantOption(productId: string, optionId: string) {
  return apiClient
    .delete<Product>(`/products/${productId}/variant-options/${optionId}`)
    .then((response) => response.data)
}

export function addVariant(productId: string, request: CreateProductVariantRequest) {
  return apiClient
    .post<Product>(`/products/${productId}/variants`, request)
    .then((response) => response.data)
}

export function updateVariant(
  productId: string,
  variantId: string,
  request: UpdateProductVariantRequest,
) {
  return apiClient
    .put<Product>(`/products/${productId}/variants/${variantId}`, request)
    .then((response) => response.data)
}

export function deleteVariant(productId: string, variantId: string) {
  return apiClient
    .delete<Product>(`/products/${productId}/variants/${variantId}`)
    .then((response) => response.data)
}
