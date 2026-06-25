export interface Conversation {
  id: string
  customerId: string
  customerName: string
  lastMessagePreview: string | null
  lastMessageAt: string
  unreadCountForAdmin: number
  unreadCountForCustomer: number
}

export interface ChatMessage {
  id: string
  conversationId: string
  senderId: string
  senderName: string
  isFromAdmin: boolean
  content: string
  createdAt: string
}

export interface SendMessageRequest {
  content: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
