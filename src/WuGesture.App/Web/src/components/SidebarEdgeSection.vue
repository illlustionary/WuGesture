<script setup>
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import AppIcon from '@/components/AppIcon.vue'

defineProps({
  isSidebarHidden: { type: Boolean, default: false },
  isSidebarVisible: { type: Boolean, default: true }
})

const route = useRoute()
const expanded = ref(false)
const isActive = computed(() => route.path.startsWith('/edge'))
const items = [
  { to: '/edge/corner', label: '触发角', icon: 'circle-dashed' },
  { to: '/edge/friction', label: '摩擦边', icon: 'circle-dashed' },
  { to: '/edge/wheel', label: '边缘滚动', icon: 'mouse' }
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
      :aria-label="isSidebarHidden ? '边缘操作' : undefined"
      @click="toggle"
    >
      <AppIcon
        name="circle-dashed"
        class="app-sidebar__item-icon"
        aria-hidden="true"
      />
      <span class="sidebar-group__title">边缘操作</span>
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
