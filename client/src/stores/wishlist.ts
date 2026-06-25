import { defineStore } from 'pinia'
import { addWishlistItem, getWishlist, removeWishlistItem } from '../api/wishlist'
import type { Wishlist } from '../types/wishlist'

interface WishlistState {
  wishlist: Wishlist | null
}

export const useWishlistStore = defineStore('wishlist', {
  state: (): WishlistState => ({
    wishlist: null,
  }),
  getters: {
    itemCount: (state) => state.wishlist?.items.length ?? 0,
    has: (state) => (productId: string) => state.wishlist?.items.some((i) => i.productId === productId) ?? false,
  },
  actions: {
    async fetchWishlist() {
      this.wishlist = await getWishlist()
    },
    async toggle(productId: string) {
      if (this.has(productId)) {
        this.wishlist = await removeWishlistItem(productId)
      } else {
        this.wishlist = await addWishlistItem(productId)
      }
    },
    reset() {
      this.wishlist = null
    },
  },
})
