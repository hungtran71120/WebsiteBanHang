import { defineStore } from 'pinia'

interface NotifyState {
  message: string
  isVisible: boolean
  autoHideTimer: ReturnType<typeof setTimeout> | null
}

const AUTO_HIDE_MS = 3000

export const useNotifyStore = defineStore('notify', {
  state: (): NotifyState => ({
    message: '',
    isVisible: false,
    autoHideTimer: null,
  }),
  actions: {
    show(message: string) {
      if (this.autoHideTimer) {
        clearTimeout(this.autoHideTimer)
      }
      this.message = message
      this.isVisible = true
      this.autoHideTimer = setTimeout(() => this.hide(), AUTO_HIDE_MS)
    },
    hide() {
      if (this.autoHideTimer) {
        clearTimeout(this.autoHideTimer)
        this.autoHideTimer = null
      }
      this.isVisible = false
    },
  },
})
