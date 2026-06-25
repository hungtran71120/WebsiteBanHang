export interface WishlistItem {
  productId: string
  productName: string
  productImageUrl: string | null
  price: number
  inStock: boolean
}

export interface Wishlist {
  items: WishlistItem[]
}
