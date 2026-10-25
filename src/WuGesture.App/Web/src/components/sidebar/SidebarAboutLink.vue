<script setup>
import { RouterLink } from 'vue-router'
import AppIcon from '@/components/ui/AppIcon.vue'
import HoverBubble from '@/components/ui/HoverBubble.vue'

defineProps({
  isSidebarHidden: { type: Boolean, default: false }
})
</script>

<template>
  <HoverBubble :text="isSidebarHidden ? '关于&更新' : ''">
    <RouterLink
      to="/about"
      class="app-sidebar__item"
      exact-active-class="is-active"
      :aria-label="isSidebarHidden ? '关于&更新' : undefined"
    >
      <AppIcon
        name="sparkle"
        class="app-sidebar__item-icon"
        aria-hidden="true"
      />
      <span>关于&更新</span>
    </RouterLink>
  </HoverBubble>
</template>
