const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5039/api'
const API_ORIGIN = API_BASE_URL.replace(/\/api\/?$/, '')

export function resolveImageUrl(imageUrl: string | null): string | null {
  if (!imageUrl) {
    return null
  }
  return `${API_ORIGIN}${imageUrl}`
}

export function resolveChatHubUrl(): string {
  return `${API_ORIGIN}/hubs/chat`
}

export function resolveNotificationHubUrl(): string {
  return `${API_ORIGIN}/hubs/notifications`
}
