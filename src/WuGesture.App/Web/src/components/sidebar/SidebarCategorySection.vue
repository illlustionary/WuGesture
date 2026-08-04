<script setup>
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import AppIcon from '@/components/ui/AppIcon.vue'
import HoverBubble from '@/components/ui/HoverBubble.vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'
import { getCategoryIcon } from '@/pages/category/composables/useCategoryPage'

defineProps({
  isSidebarHidden: { type: Boolean, default: false },
  isSidebarVisible: { type: Boolean, default: true }
})

const emit = defineEmits(['open-create', 'open-rename', 'delete'])
const route = useRoute()
const rulesStore = useGestureEditorContext()
const expanded = ref(false)
const searchValue = ref('')
const items = computed(() => rulesStore.categoryItems)
const isActive = computed(() => route.path.startsWith('/category'))
const filteredItems = computed(() => {
  const query = searchValue.value.trim().toLowerCase()
  return query ? items.value.filter(item => item.name.toLowerCase().includes(query)) : items.value
})

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
      :aria-label="isSidebarHidden ? '分类' : undefined"
      @click="toggle"
    >
      <AppIcon
        name="folder"
        class="app-sidebar__item-icon"
        aria-hidden="true"
      />
      <span class="sidebar-group__title">分类</span>
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
            aria-label="搜索分类"
            placeholder="搜索分类"
          />
        </div>
        <HoverBubble text="新增"
          ><IconActionButton
            icon="add"
            label="新增"
            :show-tooltip="false"
            class="sidebar-group__action"
            @click="emit('open-create')"
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
            class="sidebar-child sidebar-category-child"
            @dblclick.prevent="emit('open-rename', item)"
          >
            <RouterLink
              :to="{ name: 'category-scope', params: { name: item.name } }"
              class="sidebar-child__link"
              exact-active-class="is-active"
            >
              <AppIcon
                :name="getCategoryIcon(item.name)"
                class="sidebar-child__icon sidebar-child__svg-icon"
                aria-hidden="true"
              />
              <span class="sidebar-child__copy"
                ><span class="sidebar-child__label">{{ item.name }}</span></span
              >
            </RouterLink>
            <IconActionButton
              icon="delete"
              label="删除分类"
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
        >暂无分类</span
      >
    </div>
  </section>
</template>
