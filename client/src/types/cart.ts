export interface CartItem {
  id: string
  productId: string
  productName: string
  productImageUrl: string | null
  productVariantId: string | null
  variantDescription: string | null
  unitPrice: number
  stock: number
  quantity: number
  lineTotal: number
}

export interface Cart {
  items: CartItem[]
  totalAmount: number
}

export interface AddCartItemRequest {
  productId: string
  productVariantId?: string | null
  quantity: number
}

export interface UpdateCartItemRequest {
  quantity: number
}
