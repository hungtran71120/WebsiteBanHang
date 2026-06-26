<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import AppIcon from './icons/AppIcon.vue'
import { useChatStore } from '../stores/chat'

const chatStore = useChatStore()
const isOpen = computed(() => chatStore.isWidgetOpen)
const isLoading = ref(false)
const draft = ref('')
const messagesEl = ref<HTMLElement | null>(null)

const conversation = computed(() => chatStore.myConversation)
const messages = computed(() => (conversation.value ? chatStore.messagesByConversation[conversation.value.id] ?? [] : []))
const unreadCount = computed(() => conversation.value?.unreadCountForCustomer ?? 0)

onMounted(async () => {
  chatStore.connect()
  await chatStore.loadMyConversationMeta()
})

async function toggleOpen() {
  chatStore.isWidgetOpen = !chatStore.isWidgetOpen
  if (isOpen.value && conversation.value) {
    isLoading.value = true
    try {
      await chatStore.loadMessages(conversation.value.id)
      await chatStore.markAsRead(conversation.value.id)
      await scrollToBottom()
    } finally {
      isLoading.value = false
    }
  }
}

async function handleSend() {
  const content = draft.value.trim()
  if (!content || !conversation.value) {
    return
  }
  draft.value = ''
  await chatStore.sendChatMessage(conversation.value.id, content)
}

async function scrollToBottom() {
  await nextTick()
  if (messagesEl.value) {
    messagesEl.value.scrollTop = messagesEl.value.scrollHeight
  }
}

watch(messages, () => {
  scrollToBottom()
})

function formatTime(value: string) {
  return new Date(value).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}
</script>

<template>
  <div class="chat-widget">
    <div v-if="isOpen" class="chat-panel">
      <header class="chat-panel-header">
        <span>Chat với Shop</span>
        <button type="button" class="chat-close-btn" @click="toggleOpen"><AppIcon name="x" :size="16" /></button>
      </header>
      <div ref="messagesEl" class="chat-messages">
        <p v-if="isLoading" class="chat-hint">Đang tải...</p>
        <p v-else-if="messages.length === 0" class="chat-hint">Gửi tin nhắn cho Shop để được hỗ trợ.</p>
        <div
          v-for="message in messages"
          :key="message.id"
          class="chat-message"
          :class="{ 'chat-message--mine': !message.isFromAdmin }"
        >
          <div class="chat-bubble">{{ message.content }}</div>
          <span class="chat-time">{{ formatTime(message.createdAt) }}</span>
        </div>
      </div>
      <form class="chat-input-row" @submit.prevent="handleSend">
        <input v-model="draft" type="text" placeholder="Nhập tin nhắn..." maxlength="2000" />
        <button type="submit" class="chat-send-btn">Gửi</button>
      </form>
    </div>
    <button type="button" class="chat-bubble-btn" @click="toggleOpen">
      <span v-if="unreadCount > 0 && !isOpen" class="chat-unread-badge">{{ unreadCount > 9 ? '9+' : unreadCount }}</span>
      <AppIcon name="chat" :size="24" />
    </button>
  </div>
</template>

<style scoped>
.chat-widget {
  position: fixed;
  right: 24px;
  bottom: 24px;
  z-index: 1000;
}

.chat-bubble-btn {
  position: relative;
  width: 56px;
  height: 56px;
  border-radius: 50%;
  border: none;
  background: var(--shopee-orange);
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: var(--shadow-lg);
  cursor: pointer;
}

.chat-bubble-btn:hover {
  background: var(--shopee-orange-dark);
}

.chat-unread-badge {
  position: absolute;
  top: -4px;
  right: -4px;
  min-width: 18px;
  height: 18px;
  padding: 0 4px;
  border-radius: 9px;
  background: #ff4d4f;
  border: 2px solid #fff;
  color: #fff;
  font-size: 11px;
  font-weight: 600;
  line-height: 1;
  display: flex;
  align-items: center;
  justify-content: center;
}

.chat-panel {
  position: absolute;
  right: 0;
  bottom: 68px;
  width: 320px;
  height: 420px;
  background: var(--surface);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.chat-panel-header {
  background: var(--shopee-orange);
  color: #fff;
  padding: 12px 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 14px;
  font-weight: 600;
}

.chat-close-btn {
  background: none;
  border: none;
  color: #fff;
  font-size: 18px;
  cursor: pointer;
  line-height: 1;
}

.chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.chat-hint {
  color: var(--text-secondary);
  font-size: 13px;
  text-align: center;
  margin-top: 16px;
}

.chat-message {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
}

.chat-message--mine {
  align-items: flex-end;
}

.chat-bubble {
  max-width: 80%;
  padding: 8px 12px;
  border-radius: 12px;
  background: var(--bg-page);
  font-size: 13px;
  word-break: break-word;
}

.chat-message--mine .chat-bubble {
  background: var(--shopee-orange-light);
}

.chat-time {
  font-size: 11px;
  color: var(--text-secondary);
  margin-top: 2px;
}

.chat-input-row {
  display: flex;
  border-top: 1px solid var(--border);
}

.chat-input-row input {
  flex: 1;
  border: none;
  padding: 10px 12px;
  font-size: 13px;
  outline: none;
}

.chat-send-btn {
  border: none;
  background: var(--shopee-orange);
  color: #fff;
  padding: 0 16px;
  font-size: 13px;
  cursor: pointer;
}
</style>
