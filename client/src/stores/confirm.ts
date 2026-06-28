import { defineStore } from 'pinia'

interface ConfirmState {
  message: string
  isVisible: boolean
}

let resolver: ((confirmed: boolean) => void) | null = null

export const useConfirmStore = defineStore('confirm', {
  state: (): ConfirmState => ({
    message: '',
    isVisible: false,
  }),
  actions: {
    ask(message: string): Promise<boolean> {
      this.message = message
      this.isVisible = true
      return new Promise((resolve) => {
        resolver = resolve
      })
    },
    confirm() {
      this.isVisible = false
      resolver?.(true)
      resolver = null
    },
    cancel() {
      this.isVisible = false
      resolver?.(false)
      resolver = null
    },
  },
})
