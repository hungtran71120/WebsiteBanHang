import { HubConnectionBuilder, type HubConnection } from '@microsoft/signalr'
import { defineStore } from 'pinia'
import { getConversations, getMessages, getMyConversation, markConversationAsRead, sendMessage } from '../api/chat'
import { resolveChatHubUrl } from '../utils/url'
import type { ChatMessage, Conversation } from '../types/chat'
import { useAuthStore } from './auth'

interface ChatState {
  connection: HubConnection | null
  isConnected: boolean
  isWidgetOpen: boolean
  myConversation: Conversation | null
  conversations: Conversation[]
  messagesByConversation: Record<string, ChatMessage[]>
}

export const useChatStore = defineStore('chat', {
  state: (): ChatState => ({
    connection: null,
    isConnected: false,
    isWidgetOpen: false,
    myConversation: null,
    conversations: [],
    messagesByConversation: {},
  }),
  getters: {
    totalUnreadMessagesForAdmin: (state) => state.conversations.reduce((sum, c) => sum + c.unreadCountForAdmin, 0),
  },
  actions: {
    openWidget() {
      this.isWidgetOpen = true
    },
    connect() {
      if (this.connection) {
        return
      }

      const authStore = useAuthStore()
      const connection = new HubConnectionBuilder()
        .withUrl(resolveChatHubUrl(), { accessTokenFactory: () => authStore.accessToken ?? '' })
        .withAutomaticReconnect()
        .build()

      connection.on('ReceiveMessage', (message: ChatMessage) => {
        this.appendMessage(message)
      })

      connection.on('ConversationUpdated', (conversation: Conversation) => {
        this.upsertConversation(conversation)
      })

      connection.onreconnected(() => {
        this.isConnected = true
      })
      connection.onclose(() => {
        this.isConnected = false
      })

      this.connection = connection
      connection
        .start()
        .then(() => {
          this.isConnected = true
        })
        .catch(() => {
          this.isConnected = false
        })
    },
    disconnect() {
      this.connection?.stop()
      this.connection = null
      this.isConnected = false
    },
    appendMessage(message: ChatMessage) {
      const list = this.messagesByConversation[message.conversationId] ?? []
      if (list.some((m) => m.id === message.id)) {
        return
      }
      this.messagesByConversation[message.conversationId] = [...list, message]
    },
    upsertConversation(conversation: Conversation) {
      if (this.myConversation?.id === conversation.id) {
        this.myConversation = conversation
      }

      const index = this.conversations.findIndex((c) => c.id === conversation.id)
      if (index === -1) {
        this.conversations = [conversation, ...this.conversations]
      } else {
        this.conversations = this.conversations
          .map((c) => (c.id === conversation.id ? conversation : c))
          .sort((a, b) => new Date(b.lastMessageAt).getTime() - new Date(a.lastMessageAt).getTime())
      }
    },
    async joinConversation(conversationId: string) {
      await this.connection?.invoke('JoinConversation', conversationId)
    },
    async leaveConversation(conversationId: string) {
      await this.connection?.invoke('LeaveConversation', conversationId)
    },
    async loadMyConversationMeta() {
      this.myConversation = await getMyConversation()
      return this.myConversation
    },
    async loadMessages(conversationId: string) {
      const result = await getMessages(conversationId, 1, 50)
      this.messagesByConversation[conversationId] = [...result.items].reverse()
    },
    async loadConversationsForAdmin() {
      const result = await getConversations(1, 50)
      this.conversations = result.items
    },
    async sendChatMessage(conversationId: string, content: string) {
      await sendMessage(conversationId, { content })
    },
    async markAsRead(conversationId: string) {
      await markConversationAsRead(conversationId)
    },
  },
})
