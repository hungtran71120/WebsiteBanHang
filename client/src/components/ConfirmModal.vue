<script setup lang="ts">
import AppIcon from './icons/AppIcon.vue'
import { useConfirmStore } from '../stores/confirm'

const confirmStore = useConfirmStore()
</script>

<template>
  <Transition name="confirm-fade">
    <div v-if="confirmStore.isVisible" class="confirm-overlay" @click.self="confirmStore.cancel()">
      <div class="confirm-box">
        <div class="confirm-box__icon">
          <AppIcon name="x" :size="22" :filled="true" />
        </div>
        <p class="confirm-box__message">{{ confirmStore.message }}</p>
        <div class="confirm-box__actions">
          <button type="button" class="confirm-box__cancel" @click="confirmStore.cancel()">Hủy</button>
          <button type="button" class="confirm-box__confirm" @click="confirmStore.confirm()">Xóa</button>
        </div>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.confirm-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 16px;
}

.confirm-box {
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

.confirm-box__icon {
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

.confirm-box__message {
  font-size: 15px;
  color: var(--text);
  line-height: 1.5;
  margin-bottom: 18px;
}

.confirm-box__actions {
  display: flex;
  gap: 10px;
  width: 100%;
}

.confirm-box__cancel,
.confirm-box__confirm {
  flex: 1;
  padding: 10px 0;
  font-size: 14px;
  border-radius: var(--radius-sm);
  cursor: pointer;
}

.confirm-box__cancel {
  background: #fff;
  border: 1px solid var(--border);
  color: var(--text);
}

.confirm-box__cancel:hover {
  border-color: var(--shopee-orange);
  color: var(--shopee-orange);
}

.confirm-box__confirm {
  background: var(--shopee-orange);
  border: none;
  color: #fff;
}

.confirm-box__confirm:hover {
  opacity: 0.9;
}

.confirm-fade-enter-active,
.confirm-fade-leave-active {
  transition: opacity 0.15s ease;
}

.confirm-fade-enter-from,
.confirm-fade-leave-to {
  opacity: 0;
}
</style>
