export interface Category {
  id: string
  name: string
  parentCategoryId: string | null
  parentCategoryName: string | null
}

export interface ProductVariantOptionValue {
  id: string
  value: string
  imageUrl: string | null
}

export interface ProductVariantOption {
  id: string
  name: string
  displayOrder: number
  values: ProductVariantOptionValue[]
}

export interface ProductVariant {
  id: string
  optionValue1Id: string
  optionValue1Text: string
  optionValue2Id: string | null
  optionValue2Text: string | null
  stock: number
}

export interface Product {
  id: string
  name: string
  description: string
  price: number
  stock: number
  imageUrl: string | null
  categoryId: string
  categoryName: string
  averageRating: number
  reviewCount: number
  soldCount: number
  flashSalePrice: number | null
  flashSaleQuantityRemaining: number | null
  flashSaleEndsAt: string | null
  variants: ProductVariant[]
  variantOptions: ProductVariantOption[]
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export type ProductSortBy = 'Default' | 'Newest' | 'PriceAsc' | 'PriceDesc'

export interface ProductFilter {
  keyword?: string
  categoryId?: string
  minPrice?: number
  maxPrice?: number
  sortBy?: ProductSortBy
  page: number
  pageSize: number
}

export interface CreateCategoryRequest {
  name: string
  parentCategoryId?: string | null
}

export interface UpdateCategoryRequest {
  name: string
  parentCategoryId?: string | null
}

export interface CreateProductRequest {
  name: string
  description: string
  price: number
  stock: number
  categoryId: string
}

export type UpdateProductRequest = CreateProductRequest

export interface CreateVariantOptionRequest {
  name: string
  values: { value: string }[]
}

export interface AddVariantOptionValueRequest {
  value: string
}

export interface CreateProductVariantRequest {
  optionValue1Id: string
  optionValue2Id?: string | null
  stock: number
}

export interface UpdateProductVariantRequest {
  stock: number
}
