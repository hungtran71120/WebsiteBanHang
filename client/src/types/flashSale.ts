export interface FlashSaleItem {
  id: string
  productId: string
  productName: string
  imageUrl: string | null
  originalPrice: number
  salePrice: number
  quantityLimit: number
  quantitySold: number
  soldPercentage: number
  averageRating: number
  reviewCount: number
}

export interface FlashSale {
  id: string
  name: string
  startsAt: string
  endsAt: string
  isActive: boolean
  items: FlashSaleItem[]
}

export interface CreateFlashSaleRequest {
  name: string
  startsAt: string
  endsAt: string
  isActive: boolean
}

export interface AddFlashSaleItemRequest {
  productId: string
  salePrice: number
  quantityLimit: number
}

export interface UpdateFlashSaleItemRequest {
  salePrice: number
  quantityLimit: number
}
