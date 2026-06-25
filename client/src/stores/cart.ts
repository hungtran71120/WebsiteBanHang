import { defineStore } from 'pinia'
import { addCartItem, getCart, removeCartItem, updateCartItem } from '../api/cart'
import type { Cart } from '../types/cart'

interface CartState {
  cart: Cart | null
}

export const useCartStore = defineStore('cart', {
  state: (): CartState => ({
    cart: null,
  }),
  getters: {
    itemCount: (state) => state.cart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0,
  },
  actions: {
    async fetchCart() {
      this.cart = await getCart()
    },
    async addItem(productId: string, quantity: number, productVariantId?: string | null) {
      this.cart = await addCartItem({ productId, productVariantId, quantity })
    },
    async updateItem(cartItemId: string, quantity: number) {
      this.cart = await updateCartItem(cartItemId, { quantity })
    },
    async removeItem(cartItemId: string) {
      this.cart = await removeCartItem(cartItemId)
    },
    reset() {
      this.cart = null
    },
  },
})
