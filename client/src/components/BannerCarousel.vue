<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import AppIcon from './icons/AppIcon.vue'
import { getActiveBanners } from '../api/banners'
import type { Banner } from '../types/banner'
import { resolveImageUrl } from '../utils/url'

const banners = ref<Banner[]>([])
const activeIndex = ref(0)
let timer: ReturnType<typeof setInterval> | null = null

function start() {
  stop()
  if (banners.value.length > 1) {
    timer = setInterval(next, 4500)
  }
}

function stop() {
  if (timer) {
    clearInterval(timer)
    timer = null
  }
}

function next() {
  activeIndex.value = (activeIndex.value + 1) % banners.value.length
}

function prev() {
  activeIndex.value = (activeIndex.value - 1 + banners.value.length) % banners.value.length
}

function goTo(index: number) {
  activeIndex.value = index
}

function slideStyle(banner: Banner) {
  const imageUrl = resolveImageUrl(banner.imageUrl)
  if (imageUrl) {
    return { backgroundImage: `linear-gradient(90deg, rgba(0,0,0,0.55), rgba(0,0,0,0.1) 60%), url('${imageUrl}')` }
  }
  return { background: 'linear-gradient(135deg, #ea580c, #fb923c)' }
}

onMounted(async () => {
  try {
    banners.value = await getActiveBanners()
  } catch {
    banners.value = []
  }
  start()
})
onBeforeUnmount(stop)
</script>

<template>
  <div v-if="banners.length > 0" class="banner-carousel" @mouseenter="stop" @mouseleave="start">
    <div class="banner-carousel__track" :style="{ transform: `translateX(-${activeIndex * 100}%)` }">
      <RouterLink
        v-for="banner in banners"
        :key="banner.id"
        :to="banner.linkUrl"
        class="banner-slide"
        :style="slideStyle(banner)"
      >
        <div class="banner-slide__text">
          <h2>{{ banner.title }}</h2>
          <p v-if="banner.subtitle">{{ banner.subtitle }}</p>
          <span class="banner-slide__cta">Xem ngay <AppIcon name="chevron-left" :size="14" style="transform: rotate(180deg)" /></span>
        </div>
      </RouterLink>
    </div>

    <template v-if="banners.length > 1">
      <button type="button" class="banner-carousel__arrow banner-carousel__arrow--prev" aria-label="Trước" @click="prev">
        <AppIcon name="chevron-left" :size="20" />
      </button>
      <button type="button" class="banner-carousel__arrow banner-carousel__arrow--next" aria-label="Tiếp" @click="next">
        <AppIcon name="chevron-left" :size="20" style="transform: rotate(180deg)" />
      </button>

      <div class="banner-carousel__dots">
        <button
          v-for="(banner, index) in banners"
          :key="banner.id"
          type="button"
          class="banner-carousel__dot"
          :class="{ active: index === activeIndex }"
          :aria-label="`Slide ${index + 1}`"
          @click="goTo(index)"
        ></button>
      </div>
    </template>
  </div>
</template>

<style scoped>
.banner-carousel {
  position: relative;
  width: 100%;
  height: 220px;
  border-radius: var(--radius-md);
  overflow: hidden;
  margin-bottom: 12px;
  box-shadow: var(--shadow-sm);
}

.banner-carousel__track {
  display: flex;
  height: 100%;
  transition: transform 0.5s ease;
}

.banner-slide {
  flex: 0 0 100%;
  display: flex;
  align-items: center;
  padding: 0 40px;
  color: #fff;
  text-decoration: none;
  background-size: cover;
  background-position: center;
}

.banner-slide__text h2 {
  font-size: 24px;
  font-weight: 700;
  margin-bottom: 8px;
  max-width: 560px;
}

.banner-slide__text p {
  font-size: 14px;
  opacity: 0.92;
  margin-bottom: 14px;
  max-width: 480px;
}

.banner-slide__cta {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: rgba(255, 255, 255, 0.95);
  color: #18181b;
  font-size: 13px;
  font-weight: 600;
  padding: 8px 18px;
  border-radius: var(--radius-sm);
}

.banner-carousel__arrow {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  width: 36px;
  height: 36px;
  border-radius: 50%;
  border: none;
  background: rgba(0, 0, 0, 0.25);
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  opacity: 0;
  transition: opacity 0.15s, background-color 0.15s;
}

.banner-carousel:hover .banner-carousel__arrow {
  opacity: 1;
}

.banner-carousel__arrow:hover {
  background: rgba(0, 0, 0, 0.45);
}

.banner-carousel__arrow--prev {
  left: 14px;
}

.banner-carousel__arrow--next {
  right: 14px;
}

.banner-carousel__dots {
  position: absolute;
  bottom: 14px;
  left: 0;
  right: 0;
  display: flex;
  justify-content: center;
  gap: 8px;
}

.banner-carousel__dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  border: none;
  background: rgba(255, 255, 255, 0.5);
  cursor: pointer;
  padding: 0;
}

.banner-carousel__dot.active {
  background: #fff;
  width: 20px;
  border-radius: 4px;
}

@media (max-width: 768px) {
  .banner-carousel {
    height: 170px;
  }

  .banner-slide {
    padding: 0 20px;
  }

  .banner-slide__text h2 {
    font-size: 18px;
    margin-bottom: 4px;
  }

  .banner-slide__text p {
    font-size: 12.5px;
    margin-bottom: 10px;
  }
}
</style>
