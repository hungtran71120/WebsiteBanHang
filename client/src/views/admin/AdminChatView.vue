<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { useChatStore } from '../../stores/chat'

const chatStore = useChatStore()
const activeConversationId = ref<string | null>(null)
const isLoadingConversations = ref(true)
const isLoadingMessages = ref(false)
const draft = ref('')
const messagesEl = ref<HTMLElement | null>(null)

const conversations = computed(() => chatStore.conversations)
const activeConversation = computed(() => conversations.value.find((c) => c.id === activeConversationId.value) ?? null)
const messages = computed(() =>
  activeConversationId.value ? chatStore.messagesByConversation[activeConversationId.value] ?? [] : [],
)

async function selectConversation(conversationId: string) {
  if (activeConversationId.value === conversationId) {
    return
  }
  if (activeConversationId.value) {
    await chatStore.leaveConversation(activeConversationId.value)
  }

  activeConversationId.value = conversationId
  isLoadingMessages.value = true
  try {
    await chatStore.loadMessages(conversationId)
    await chatStore.joinConversation(conversationId)
    await chatStore.markAsRead(conversationId)
    await scrollToBottom()
  } finally {
    isLoadingMessages.value = false
  }
}

async function handleSend() {
  const content = draft.value.trim()
  if (!content || !activeConversationId.value) {
    return
  }
  draft.value = ''
  await chatStore.sendChatMessage(activeConversationId.value, content)
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
  return new Date(value).toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' })
}

onMounted(async () => {
  chatStore.connect()
  isLoadingConversations.value = true
  try {
    await chatStore.loadConversationsForAdmin()
  } finally {
    isLoadingConversations.value = false
  }
})

onUnmounted(() => {
  if (activeConversationId.value) {
    chatStore.leaveConversation(activeConversationId.value)
  }
})
</script>

<template>
  <div class="admin-page admin-chat-page">
    <div class="admin-page__header">
      <h1>Chat với khách hàng</h1>
    </div>

    <div class="admin-chat-layout">
      <aside class="admin-chat-list">
        <p v-if="isLoadingConversations" class="state-message">Đang tải...</p>
        <p v-else-if="conversations.length === 0" class="state-message">Chưa có hội thoại nào.</p>
        <button
          v-for="conversation in conversations"
          :key="conversation.id"
          type="button"
          class="admin-chat-list-item"
          :class="{ 'admin-chat-list-item--active': conversation.id === activeConversationId }"
          @click="selectConversation(conversation.id)"
        >
          <span class="admin-chat-list-item__name">{{ conversation.customerName }}</span>
          <span class="admin-chat-list-item__preview">{{ conversation.lastMessagePreview ?? 'Chưa có tin nhắn' }}</span>
          <span v-if="conversation.unreadCountForAdmin > 0" class="admin-chat-unread-badge">
            {{ conversation.unreadCountForAdmin > 9 ? '9+' : conversation.unreadCountForAdmin }}
          </span>
        </button>
      </aside>

      <section class="admin-chat-panel">
        <template v-if="activeConversation">
          <header class="admin-chat-panel__header">{{ activeConversation.customerName }}</header>
          <div ref="messagesEl" class="admin-chat-messages">
            <p v-if="isLoadingMessages" class="state-message">Đang tải...</p>
            <div
              v-for="message in messages"
              :key="message.id"
              class="admin-chat-message"
              :class="{ 'admin-chat-message--mine': message.isFromAdmin }"
            >
              <div class="admin-chat-bubble">{{ message.content }}</div>
              <span class="admin-chat-time">{{ formatTime(message.createdAt) }}</span>
            </div>
          </div>
          <form class="admin-chat-input-row" @submit.prevent="handleSend">
            <input v-model="draft" type="text" placeholder="Nhập tin nhắn trả lời..." maxlength="2000" />
            <button type="submit">Gửi</button>
          </form>
        </template>
        <p v-else class="state-message admin-chat-placeholder">Chọn một hội thoại để xem tin nhắn.</p>
      </section>
    </div>
  </div>
</template>

<style scoped>
.admin-chat-layout {
  display: flex;
  height: calc(100vh - 160px);
  border: 1px solid var(--border);
  border-radius: 4px;
  overflow: hidden;
  background: #fff;
}

.admin-chat-list {
  width: 260px;
  flex-shrink: 0;
  border-right: 1px solid var(--border);
  overflow-y: auto;
  display: flex;
  flex-direction: column;
}

.admin-chat-list-item {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 2px;
  padding: 12px 16px;
  border: none;
  border-bottom: 1px solid var(--border);
  background: #fff;
  text-align: left;
  cursor: pointer;
}

.admin-chat-list-item:hover {
  background: var(--bg-page);
}

.admin-chat-list-item--active {
  background: var(--shopee-orange-light);
}

.admin-chat-list-item__name {
  font-size: 14px;
  font-weight: 600;
}

.admin-chat-list-item__preview {
  font-size: 12px;
  color: var(--text-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
}

.admin-chat-unread-badge {
  position: absolute;
  top: 12px;
  right: 14px;
  min-width: 18px;
  height: 18px;
  padding: 0 4px;
  border-radius: 9px;
  background: var(--shopee-orange);
  color: #fff;
  font-size: 11px;
  font-weight: 600;
  line-height: 1;
  display: flex;
  align-items: center;
  justify-content: center;
}

.admin-chat-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.admin-chat-panel__header {
  padding: 12px 16px;
  border-bottom: 1px solid var(--border);
  font-weight: 600;
}

.admin-chat-placeholder {
  margin: auto;
}

.admin-chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.admin-chat-message {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
}

.admin-chat-message--mine {
  align-items: flex-end;
}

.admin-chat-bubble {
  max-width: 60%;
  padding: 8px 12px;
  border-radius: 12px;
  background: var(--bg-page);
  font-size: 14px;
  word-break: break-word;
}

.admin-chat-message--mine .admin-chat-bubble {
  background: var(--shopee-orange-light);
}

.admin-chat-time {
  font-size: 11px;
  color: var(--text-secondary);
  margin-top: 2px;
}

.admin-chat-input-row {
  display: flex;
  border-top: 1px solid var(--border);
}

.admin-chat-input-row input {
  flex: 1;
  border: none;
  padding: 12px 16px;
  font-size: 14px;
  outline: none;
}

.admin-chat-input-row button {
  border: none;
  background: var(--shopee-orange);
  color: #fff;
  padding: 0 20px;
  font-size: 14px;
  cursor: pointer;
}

@media (max-width: 700px) {
  .admin-chat-layout {
    flex-direction: column;
    height: calc(100vh - 200px);
  }

  .admin-chat-list {
    width: 100%;
    max-height: 160px;
    border-right: none;
    border-bottom: 1px solid var(--border);
  }
}
</style>
