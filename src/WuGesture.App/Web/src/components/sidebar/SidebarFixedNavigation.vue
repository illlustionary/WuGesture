<script setup>
import { RouterLink } from 'vue-router'
import AppIcon from '@/components/ui/AppIcon.vue'
import HoverBubble from '@/components/ui/HoverBubble.vue'

defineProps({
  isSidebarHidden: { type: Boolean, default: false },
  isSidebarVisible: { type: Boolean, default: true }
})

const items = [
  { to: '/global', label: '全局', icon: 'mouse' },
  { to: '/exclusions', label: '排除项', icon: 'close-alt' }
]
</script>

<template>
  <HoverBubble
    v-for="item in items"
    :key="item.to"
    :text="isSidebarHidden ? item.label : ''"
  >
    <RouterLink
      :to="item.to"
      class="app-sidebar__item"
      exact-active-class="is-active"
      :aria-label="isSidebarHidden ? item.label : undefined"
    >
      <AppIcon
        :name="item.icon"
        class="app-sidebar__item-icon"
        aria-hidden="true"
      />
      <span>{{ item.label }}</span>
    </RouterLink>
  </HoverBubble>
</template>
