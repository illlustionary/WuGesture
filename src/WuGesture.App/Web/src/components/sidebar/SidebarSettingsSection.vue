<script setup>
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import AppIcon from '@/components/ui/AppIcon.vue'

defineProps({
  isSidebarHidden: { type: Boolean, default: false },
  isSidebarVisible: { type: Boolean, default: true }
})

const route = useRoute()
const expanded = ref(false)
const isActive = computed(() => route.path.startsWith('/settings'))
const items = [
  { to: '/settings/config', label: '配置', icon: 'database' },
  { to: '/settings/mouse-trail', label: '轨迹线', icon: 'spline' },
  { to: '/settings/gesture-hint', label: '手势提示', icon: 'message-square' },
  { to: '/settings/level-osd', label: '音量/亮度提示', icon: 'monitor' },
  { to: '/settings/sensitivity', label: '手势灵敏度', icon: 'gauge' },
  { to: '/settings/app-behavior', label: '应用行为', icon: 'toggle-left' },
  { to: '/settings/update', label: '更新', icon: 'refresh-cw' },
  { to: '/settings/webdav', label: 'WebDAV', icon: 'cloud' }
]

function toggle() {
  expanded.value = !expanded.value
}

watch(
  isActive,
  active => {
    if (active) expanded.value = true
  },
  { immediate: true }
)
</script>

<template>
  <section
    class="sidebar-group"
    :class="{ 'is-expanded': expanded, 'is-active': isActive }"
  >
    <button
      type="button"
      class="sidebar-group__head"
      :aria-expanded="isSidebarVisible && expanded"
      :aria-label="isSidebarHidden ? '设置' : undefined"
      @click="toggle"
    >
      <AppIcon
        name="setting"
        class="app-sidebar__item-icon"
        aria-hidden="true"
      />
      <span class="sidebar-group__title">设置</span>
      <span
        class="sidebar-group__chevron"
        aria-hidden="true"
      />
    </button>
    <div
      v-show="expanded"
      class="sidebar-group__body"
    >
      <div class="sidebar-group__children">
        <div
          v-for="item in items"
          :key="item.to"
          class="sidebar-child"
        >
          <RouterLink
            :to="item.to"
            class="sidebar-child__link"
            exact-active-class="is-active"
          >
            <AppIcon
              :name="item.icon"
              class="sidebar-child__icon sidebar-child__svg-icon"
              aria-hidden="true"
            />
            <span class="sidebar-child__copy"
              ><span class="sidebar-child__label">{{ item.label }}</span></span
            >
          </RouterLink>
        </div>
      </div>
    </div>
  </section>
</template>
