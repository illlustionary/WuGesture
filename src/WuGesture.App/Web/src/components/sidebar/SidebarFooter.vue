<script setup lang="ts">
import { computed } from 'vue'
import AppIcon from '@/components/ui/AppIcon.vue'
import HoverBubble from '@/components/ui/HoverBubble.vue'
import type { ThemeMode } from '@/theme/scheme'

const props = withDefaults(
  defineProps<{
    isCollapsed?: boolean
    themeMode?: ThemeMode
  }>(),
  {
    isCollapsed: false,
    themeMode: 'system'
  }
)

const emit = defineEmits<{
  'open-help': []
  'open-search': []
  'cycle-theme': []
  'toggle-sidebar': []
}>()

const THEME_LABELS: Record<ThemeMode, { icon: string; label: string }> = {
  system: { icon: 'monitor', label: '跟随系统（点击切换到浅色）' },
  light: { icon: 'sun', label: '浅色（点击切换到深色）' },
  dark: { icon: 'moon', label: '深色（点击切换到跟随系统）' }
}

const collapseLabel = computed(() => (props.isCollapsed ? '展开侧栏' : '收起侧栏'))
const themeState = computed(() => THEME_LABELS[props.themeMode] ?? THEME_LABELS.system)
</script>

<template>
  <div class="app-sidebar__footer">
    <div class="app-sidebar__footer-controls">
      <HoverBubble text="帮助"
        ><button
          type="button"
          class="app-sidebar__help"
          aria-label="帮助"
          @click="emit('open-help')"
        >
          ?
        </button></HoverBubble
      >
      <HoverBubble :text="themeState.label">
        <button
          type="button"
          class="app-sidebar__theme"
          :aria-label="themeState.label"
          @click="emit('cycle-theme')"
        >
          <AppIcon
            :name="themeState.icon"
            aria-hidden="true"
          />
        </button>
      </HoverBubble>
    </div>
    <div class="app-sidebar__footer-actions">
      <HoverBubble text="搜索"
        ><button
          type="button"
          class="app-sidebar__search"
          aria-label="搜索"
          @click="emit('open-search')"
        >
          <AppIcon
            name="search"
            aria-hidden="true"
          /></button
      ></HoverBubble>
      <HoverBubble :text="collapseLabel"
        ><button
          type="button"
          class="app-sidebar__collapse"
          :aria-label="collapseLabel"
          :aria-pressed="isCollapsed"
          @click="emit('toggle-sidebar')"
        >
          <AppIcon
            name="panel-left"
            aria-hidden="true"
          /></button
      ></HoverBubble>
    </div>
  </div>
</template>
