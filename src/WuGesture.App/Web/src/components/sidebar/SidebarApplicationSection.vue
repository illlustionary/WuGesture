<script setup>
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import AppIcon from '@/components/ui/AppIcon.vue'
import HoverBubble from '@/components/ui/HoverBubble.vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'

defineProps({
  isSidebarHidden: { type: Boolean, default: false },
  isSidebarVisible: { type: Boolean, default: true }
})

const emit = defineEmits(['open-picker', 'open-rename', 'delete'])
const route = useRoute()
const rulesStore = useGestureEditorContext()
const expanded = ref(false)
const searchValue = ref('')
const items = computed(() => rulesStore.appItems)
const isActive = computed(() => route.path.startsWith('/app'))
const filteredItems = computed(() => {
  const query = searchValue.value.trim().toLowerCase()
  return query
    ? items.value.filter(item => [item.displayName, item.name, item.path].join(' ').toLowerCase().includes(query))
    : items.value
})

function label(item) {
  return item.displayName || item.name || item.path
}
function meta(item) {
  return item.displayName && item.name !== item.displayName ? item.name : ''
}
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
      :aria-label="isSidebarHidden ? '程序' : undefined"
      @click="toggle"
    >
      <AppIcon
        name="briefcase"
        class="app-sidebar__item-icon"
        aria-hidden="true"
      />
      <span class="sidebar-group__title">程序</span>
      <span
        class="sidebar-group__chevron"
        aria-hidden="true"
      />
    </button>
    <div
      v-show="expanded"
      class="sidebar-group__body"
    >
      <div class="sidebar-group__toolbar">
        <div class="sidebar-group__search">
          <AppIcon
            name="search"
            aria-hidden="true"
          /><input
            v-model="searchValue"
            type="search"
            aria-label="搜索程序"
            placeholder="搜索程序"
          />
        </div>
        <HoverBubble text="新增"
          ><IconActionButton
            icon="add"
            label="新增"
            :show-tooltip="false"
            class="sidebar-group__action"
            @click="emit('open-picker')"
        /></HoverBubble>
      </div>
      <div
        v-if="items.length"
        class="sidebar-group__children"
      >
        <template v-if="filteredItems.length">
          <div
            v-for="item in filteredItems"
            :key="item.name"
            class="sidebar-child"
            @dblclick.prevent="emit('open-rename', item)"
          >
            <RouterLink
              :to="{ name: 'app-scope', params: { name: item.name } }"
              class="sidebar-child__link"
              exact-active-class="is-active"
            >
              <img
                v-if="item.icon"
                :src="item.icon"
                class="sidebar-child__icon app-icon"
                alt=""
              />
              <span
                v-else
                class="sidebar-child__icon sidebar-child__icon--fallback"
                aria-hidden="true"
                >{{ label(item).slice(0, 1).toUpperCase() }}</span
              >
              <span class="sidebar-child__copy"
                ><span class="sidebar-child__label">{{ label(item) }}</span
                ><span
                  v-if="meta(item)"
                  class="sidebar-child__meta"
                  >{{ meta(item) }}</span
                ></span
              >
            </RouterLink>
            <IconActionButton
              icon="delete"
              label="删除程序"
              class="sidebar-child__delete"
              @click="emit('delete', item.name)"
            />
          </div>
        </template>
        <span
          v-else
          class="sidebar-group__empty"
          >没有匹配项</span
        >
      </div>
      <span
        v-else
        class="sidebar-group__empty"
        >暂无程序</span
      >
    </div>
  </section>
</template>
