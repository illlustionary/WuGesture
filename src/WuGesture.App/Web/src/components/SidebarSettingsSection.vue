<script setup>
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import AppIcon from '@/components/AppIcon.vue'
import HoverBubble from '@/components/HoverBubble.vue'
import IconActionButton from '@/components/IconActionButton.vue'

defineProps({
  isSidebarHidden: { type: Boolean, default: false },
  isSidebarVisible: { type: Boolean, default: true }
})

const emit = defineEmits(['export-config', 'import-config', 'reset-settings'])
const route = useRoute()
const expanded = ref(false)
const isActive = computed(() => route.path.startsWith('/settings'))
const items = [
  { to: '/settings/mouse-trail', label: '轨迹线', icon: 'circle-dashed' },
  { to: '/settings/gesture-hint', label: '手势提示', icon: 'mouse' },
  { to: '/settings/level-osd', label: '音量/亮度提示', icon: 'setting' },
  { to: '/settings/sensitivity', label: '手势灵敏度', icon: 'setting' },
  { to: '/settings/app-behavior', label: '应用行为', icon: 'briefcase' },
  { to: '/settings/webdav', label: 'WebDAV', icon: 'setting' }
]

function toggle() { expanded.value = !expanded.value }

watch(isActive, active => { if (active) expanded.value = true }, { immediate: true })
</script>

<template>
  <section class="sidebar-group" :class="{ 'is-expanded': expanded, 'is-active': isActive }">
    <button type="button" class="sidebar-group__head" :aria-expanded="isSidebarVisible && expanded" :aria-label="isSidebarHidden ? '设置' : undefined" @click="toggle">
      <AppIcon name="setting" class="app-sidebar__item-icon" aria-hidden="true" />
      <span v-if="isSidebarVisible" class="sidebar-group__title">设置</span>
      <span v-if="isSidebarVisible" class="sidebar-group__chevron" aria-hidden="true" />
    </button>
    <div v-if="isSidebarVisible && expanded" class="sidebar-group__body">
      <div class="sidebar-group__toolbar">
        <HoverBubble text="导出配置"><IconActionButton icon="download" label="导出配置" :show-tooltip="false" class="sidebar-group__action" @click="emit('export-config')" /></HoverBubble>
        <HoverBubble text="导入配置"><IconActionButton icon="upload" label="导入配置" :show-tooltip="false" class="sidebar-group__action" @click="emit('import-config')" /></HoverBubble>
        <HoverBubble text="恢复默认设置"><IconActionButton icon="reset" label="恢复默认设置" :show-tooltip="false" color="var(--danger)" class="sidebar-group__action" @click="emit('reset-settings')" /></HoverBubble>
      </div>
      <div class="sidebar-group__children">
        <div v-for="item in items" :key="item.to" class="sidebar-child">
          <RouterLink :to="item.to" class="sidebar-child__link" exact-active-class="is-active">
            <AppIcon :name="item.icon" class="sidebar-child__icon sidebar-child__svg-icon" aria-hidden="true" />
            <span class="sidebar-child__copy"><span class="sidebar-child__label">{{ item.label }}</span></span>
          </RouterLink>
        </div>
      </div>
    </div>
  </section>
</template>
