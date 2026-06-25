import apiClient from './axios'
import type { ChatMessage, Conversation, PagedResult, SendMessageRequest } from '../types/chat'

export function getMyConversation() {
  return apiClient.get<Conversation>('/chat/conversation').then((response) => response.data)
}

export function getConversations(page: number, pageSize: number) {
  return apiClient
    .get<PagedResult<Conversation>>('/chat/conversations', { params: { page, pageSize } })
    .then((response) => response.data)
}

export function getMessages(conversationId: string, page: number, pageSize: number) {
  return apiClient
    .get<PagedResult<ChatMessage>>(`/chat/conversations/${conversationId}/messages`, { params: { page, pageSize } })
    .then((response) => response.data)
}

export function sendMessage(conversationId: string, request: SendMessageRequest) {
  return apiClient
    .post<ChatMessage>(`/chat/conversations/${conversationId}/messages`, request)
    .then((response) => response.data)
}

export function markConversationAsRead(conversationId: string) {
  return apiClient.post(`/chat/conversations/${conversationId}/read`)
}
