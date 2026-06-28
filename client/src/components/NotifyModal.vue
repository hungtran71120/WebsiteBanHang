<script setup lang="ts">
import AppIcon from './icons/AppIcon.vue'
import { useNotifyStore } from '../stores/notify'

const notifyStore = useNotifyStore()
</script>

<template>
  <Transition name="notify-fade">
    <div v-if="notifyStore.isVisible" class="notify-overlay" @click.self="notifyStore.hide()">
      <div class="notify-box">
        <div class="notify-box__icon">
          <AppIcon name="check" :size="22" :filled="true" />
        </div>
        <p class="notify-box__message">{{ notifyStore.message }}</p>
        <button type="button" class="notify-box__close" @click="notifyStore.hide()">Đã hiểu</button>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.notify-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 16px;
}

.notify-box {
  background: #fff;
  border-radius: 12px;
  padding: 28px 24px 20px;
  width: 320px;
  max-width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  box-shadow: 0 8px 30px rgba(0, 0, 0, 0.2);
}

.notify-box__icon {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: var(--shopee-orange-light);
  color: var(--shopee-orange);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 14px;
}

.notify-box__message {
  font-size: 15px;
  color: var(--text);
  line-height: 1.5;
  margin-bottom: 18px;
}

.notify-box__close {
  background: var(--shopee-orange);
  color: #fff;
  border: none;
  padding: 10px 32px;
  font-size: 14px;
  border-radius: var(--radius-sm);
  cursor: pointer;
}

.notify-box__close:hover {
  opacity: 0.9;
}

.notify-fade-enter-active,
.notify-fade-leave-active {
  transition: opacity 0.15s ease;
}

.notify-fade-enter-from,
.notify-fade-leave-to {
  opacity: 0;
}
</style>
