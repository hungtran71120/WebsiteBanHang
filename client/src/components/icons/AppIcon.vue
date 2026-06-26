<script setup lang="ts">
type IconName =
  | 'search'
  | 'cart'
  | 'heart'
  | 'bell'
  | 'chat'
  | 'box'
  | 'home'
  | 'pin'
  | 'x'
  | 'check'
  | 'chevron-left'
  | 'log-out'
  | 'user'
  | 'star'
  | 'grid'
  | 'tag'
  | 'percent'
  | 'zap'
  | 'users'
  | 'truck'
  | 'menu'
  | 'edit'
  | 'image'

const props = withDefaults(
  defineProps<{
    name: IconName
    size?: number
    filled?: boolean
  }>(),
  {
    size: 18,
    filled: false,
  },
)
</script>

<template>
  <svg
    :width="props.size"
    :height="props.size"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    stroke-width="1.8"
    stroke-linecap="round"
    stroke-linejoin="round"
    class="app-icon"
    aria-hidden="true"
  >
    <template v-if="name === 'search'">
      <circle cx="11" cy="11" r="7" />
      <line x1="21" y1="21" x2="16.3" y2="16.3" />
    </template>
    <template v-else-if="name === 'cart'">
      <circle cx="9" cy="20" r="1.4" :fill="filled ? 'currentColor' : 'none'" />
      <circle cx="18" cy="20" r="1.4" :fill="filled ? 'currentColor' : 'none'" />
      <path d="M3 4h2l2.2 11.2a2 2 0 0 0 2 1.6h7.6a2 2 0 0 0 2-1.6L20.5 8H6.1" />
    </template>
    <template v-else-if="name === 'heart'">
      <path
        d="M12 20.5s-7.5-4.6-9.8-9A5.5 5.5 0 0 1 12 5.6 5.5 5.5 0 0 1 21.8 11.5c-2.3 4.4-9.8 9-9.8 9z"
        :fill="filled ? 'currentColor' : 'none'"
      />
    </template>
    <template v-else-if="name === 'bell'">
      <path d="M6 9a6 6 0 1 1 12 0c0 3 1 5 1.6 5.8H4.4C5 14 6 12 6 9z" />
      <path d="M9.5 18a2.5 2.5 0 0 0 5 0" />
    </template>
    <template v-else-if="name === 'chat'">
      <path
        d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z"
      />
    </template>
    <template v-else-if="name === 'box'">
      <path d="M21 8l-9-5-9 5 9 5 9-5z" />
      <path d="M3 8v8l9 5 9-5V8" />
      <path d="M12 13v8" />
    </template>
    <template v-else-if="name === 'home'">
      <path d="M3 11.5 12 4l9 7.5" />
      <path d="M5 10v10h14V10" />
      <path d="M9 20v-6h6v6" />
    </template>
    <template v-else-if="name === 'pin'">
      <path d="M12 21s-7-7.5-7-12a7 7 0 1 1 14 0c0 4.5-7 12-7 12z" />
      <circle cx="12" cy="9" r="2.5" />
    </template>
    <template v-else-if="name === 'x'">
      <line x1="6" y1="6" x2="18" y2="18" />
      <line x1="18" y1="6" x2="6" y2="18" />
    </template>
    <template v-else-if="name === 'check'">
      <path d="M5 13l4 4L19 7" />
    </template>
    <template v-else-if="name === 'chevron-left'">
      <path d="M15 6l-6 6 6 6" />
    </template>
    <template v-else-if="name === 'log-out'">
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
      <path d="M16 17l5-5-5-5" />
      <path d="M21 12H9" />
    </template>
    <template v-else-if="name === 'user'">
      <circle cx="12" cy="8" r="4" />
      <path d="M4 20c0-4 4-6 8-6s8 2 8 6" />
    </template>
    <template v-else-if="name === 'star'">
      <path
        d="M12 3l2.9 6 6.6.6-5 4.4 1.5 6.5L12 17.3 5.9 20.5l1.5-6.5-5-4.4 6.6-.6L12 3z"
        :fill="filled ? 'currentColor' : 'none'"
      />
    </template>
    <template v-else-if="name === 'grid'">
      <rect x="3" y="3" width="7" height="7" rx="1.5" />
      <rect x="14" y="3" width="7" height="7" rx="1.5" />
      <rect x="3" y="14" width="7" height="7" rx="1.5" />
      <rect x="14" y="14" width="7" height="7" rx="1.5" />
    </template>
    <template v-else-if="name === 'tag'">
      <path
        d="M3 11.5V5a2 2 0 0 1 2-2h6.5a2 2 0 0 1 1.4.6l8 8a2 2 0 0 1 0 2.8l-6.5 6.5a2 2 0 0 1-2.8 0l-8-8a2 2 0 0 1-.6-1.4z"
      />
      <circle cx="7.5" cy="7.5" r="1.1" fill="currentColor" />
    </template>
    <template v-else-if="name === 'percent'">
      <line x1="19" y1="5" x2="5" y2="19" />
      <circle cx="6.5" cy="6.5" r="2.5" />
      <circle cx="17.5" cy="17.5" r="2.5" />
    </template>
    <template v-else-if="name === 'zap'">
      <path d="M13 2 4 14h6l-1 8 9-12h-6l1-8z" />
    </template>
    <template v-else-if="name === 'users'">
      <circle cx="9" cy="8" r="3.2" />
      <path d="M3 20c0-3.4 3-5.2 6-5.2s6 1.8 6 5.2" />
      <path d="M16.5 5.2a3 3 0 0 1 0 5.8" />
      <path d="M21 20c0-2.7-1.8-4.4-4-5" />
    </template>
    <template v-else-if="name === 'truck'">
      <path d="M2 7h11v9H2z" />
      <path d="M13 10h4l3 3v3h-7z" />
      <circle cx="6.5" cy="18" r="1.6" />
      <circle cx="16.5" cy="18" r="1.6" />
    </template>
    <template v-else-if="name === 'menu'">
      <line x1="4" y1="6" x2="20" y2="6" />
      <line x1="4" y1="12" x2="20" y2="12" />
      <line x1="4" y1="18" x2="20" y2="18" />
    </template>
    <template v-else-if="name === 'edit'">
      <path d="M12 20h9" />
      <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z" />
    </template>
    <template v-else-if="name === 'image'">
      <rect x="3" y="4" width="18" height="16" rx="2" />
      <circle cx="8.5" cy="9.5" r="1.5" :fill="filled ? 'currentColor' : 'none'" />
      <path d="M21 16l-5-5-4 4-2-2-5 5" />
    </template>
  </svg>
</template>

<style scoped>
.app-icon {
  display: inline-block;
  vertical-align: -3px;
  flex-shrink: 0;
}
</style>
