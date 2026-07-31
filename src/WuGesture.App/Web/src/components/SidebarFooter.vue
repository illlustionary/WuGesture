<script setup>
import { computed } from 'vue'
import AppIcon from '@/components/AppIcon.vue'
import HoverBubble from '@/components/HoverBubble.vue'

const props = defineProps({
  isCollapsed: { type: Boolean, default: false },
  isDarkTheme: { type: Boolean, default: false }
})

const emit = defineEmits(['open-help', 'open-search', 'toggle-theme', 'toggle-sidebar'])
const collapseLabel = computed(() => (props.isCollapsed ? '展开侧栏' : '收起侧栏'))
const themeLabel = computed(() => (props.isDarkTheme ? '切换到浅色模式' : '切换到深色模式'))
</script>

<template>
  <div class="app-sidebar__footer">
    <div class="app-sidebar__footer-controls">
      <HoverBubble text="规则生效顺序"><button type="button" class="app-sidebar__help" aria-label="规则生效顺序" @click="emit('open-help')">?</button></HoverBubble>
      <HoverBubble :text="themeLabel">
        <button type="button" class="app-sidebar__theme" :aria-label="themeLabel" :aria-pressed="isDarkTheme" @click="emit('toggle-theme')">
          <AppIcon v-if="isDarkTheme" name="sun" aria-hidden="true" />
          <AppIcon v-else name="moon" aria-hidden="true" />
        </button>
      </HoverBubble>
    </div>
    <div class="app-sidebar__footer-actions">
      <HoverBubble text="搜索"><button type="button" class="app-sidebar__search" aria-label="搜索" @click="emit('open-search')"><AppIcon name="search" aria-hidden="true" /></button></HoverBubble>
      <HoverBubble :text="collapseLabel"><button type="button" class="app-sidebar__collapse" :aria-label="collapseLabel" :aria-pressed="isCollapsed" @click="emit('toggle-sidebar')"><AppIcon name="panel-left" aria-hidden="true" /></button></HoverBubble>
    </div>
  </div>
</template>
