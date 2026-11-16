<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import AppIcon from '@/components/ui/AppIcon.vue'
import BaseDialog from '@/components/dialog/BaseDialog.vue'

export interface QuickSearchItem {
  id: string | number
  group: string
  title: string
  detail?: string
  searchText?: string
}

interface QuickSearchGroup {
  name: string
  items: QuickSearchItem[]
}

const props = defineProps<{
  open: boolean
  items: QuickSearchItem[]
}>()

const emit = defineEmits<{
  close: []
  select: [item: QuickSearchItem]
}>()

const query = ref('')
const input = ref<HTMLInputElement | null>(null)
const results = ref<HTMLElement | null>(null)
const highlightedIndex = ref(-1)
const normalizedQuery = computed(() => query.value.trim().toLocaleLowerCase())
const filteredItems = computed(() => {
  if (!normalizedQuery.value) {
    return props.items
  }

  return props.items.filter(item =>
    String(item.searchText || item.title)
      .toLocaleLowerCase()
      .includes(normalizedQuery.value)
  )
})
const groupedItems = computed<QuickSearchGroup[]>(() => {
  const groups: QuickSearchGroup[] = []
  const groupMap = new Map<string, QuickSearchGroup>()

  for (const item of filteredItems.value) {
    let group = groupMap.get(item.group)

    if (!group) {
      group = { name: item.group, items: [] }
      groupMap.set(item.group, group)
      groups.push(group)
    }

    group.items.push(item)
  }

  return groups
})

watch(
  () => props.open,
  open => {
    if (!open) {
      query.value = ''
      highlightedIndex.value = -1
      return
    }

    nextTick(() => input.value?.focus())
  }
)

const highlightedItem = computed(() => filteredItems.value[highlightedIndex.value] ?? null)
const activeDescendant = computed(() => (highlightedItem.value ? optionId(highlightedItem.value) : undefined))

// 结果集变化后旧的索引可能越界，先清空高亮，等用户重新按方向键。
watch(filteredItems, () => {
  highlightedIndex.value = -1
})

function itemIndex(item: QuickSearchItem) {
  return filteredItems.value.indexOf(item)
}

function optionId(item: QuickSearchItem) {
  return `quick-search-option-${itemIndex(item)}`
}

function setHighlightedIndex(index: number) {
  highlightedIndex.value = index

  nextTick(() => {
    const item = filteredItems.value[index]

    if (!item) {
      return
    }

    results.value?.querySelector(`#${optionId(item)}`)?.scrollIntoView({ block: 'nearest' })
  })
}

function moveHighlight(step: number) {
  const count = filteredItems.value.length

  if (!count) {
    highlightedIndex.value = -1
    return
  }

  const current = highlightedIndex.value
  const next = current < 0 ? (step > 0 ? 0 : count - 1) : (current + step + count) % count
  setHighlightedIndex(next)
}

function highlightItem(item: QuickSearchItem) {
  const index = itemIndex(item)

  if (index < 0 || index === highlightedIndex.value) {
    return
  }

  setHighlightedIndex(index)
}

function selectItem(item: QuickSearchItem) {
  emit('select', item)
}

function close() {
  emit('close')
}

function handleKeydown(event: KeyboardEvent) {
  // 输入法正在组词时（例如中文候选词）的回车 / 方向键交给输入法处理。
  if (event.isComposing) {
    return
  }

  if (event.key === 'Escape') {
    event.preventDefault()
    close()
    return
  }

  // 方向键和回车只在搜索框里生效，避免在关闭按钮或结果按钮上重复触发。
  if (event.target !== input.value) {
    return
  }

  if (event.key === 'ArrowDown') {
    event.preventDefault()
    moveHighlight(1)
    return
  }

  if (event.key === 'ArrowUp') {
    event.preventDefault()
    moveHighlight(-1)
    return
  }

  if (event.key === 'Enter') {
    const item = highlightedItem.value

    if (!item) {
      return
    }

    event.preventDefault()
    selectItem(item)
  }
}
</script>

<template>
  <BaseDialog
    :open="open"
    title="快速查找"
    title-id="quick-search-title"
    :show-close="true"
    :show-actions="false"
    backdrop-class="quick-search-backdrop"
    panel-class="quick-search-dialog"
    @close="close"
    @keydown="handleKeydown"
  >
    <div class="quick-search-dialog__field">
      <AppIcon
        name="search"
        class="quick-search-dialog__search-icon"
        aria-hidden="true"
      />
      <input
        ref="input"
        v-model="query"
        class="quick-search-dialog__input"
        type="search"
        placeholder="搜索手势、分类、程序或排除项"
        aria-label="搜索配置"
        role="combobox"
        aria-autocomplete="list"
        aria-controls="quick-search-results"
        aria-expanded="true"
        :aria-activedescendant="activeDescendant"
      />
    </div>
    <div
      id="quick-search-results"
      ref="results"
      class="quick-search-dialog__results"
      role="listbox"
      aria-label="搜索结果"
    >
      <template v-if="groupedItems.length">
        <section
          v-for="(group, groupIndex) in groupedItems"
          :key="group.name"
          class="quick-search-group"
          role="group"
          :aria-labelledby="`quick-search-group-${groupIndex}`"
        >
          <h3 :id="`quick-search-group-${groupIndex}`">{{ group.name }}</h3>
          <button
            v-for="item in group.items"
            :key="item.id"
            :id="optionId(item)"
            type="button"
            class="quick-search-result"
            :class="{ 'is-highlighted': highlightedIndex === itemIndex(item) }"
            role="option"
            :aria-selected="highlightedIndex === itemIndex(item)"
            @click="selectItem(item)"
            @mouseenter="highlightItem(item)"
          >
            <strong>{{ item.title }}</strong>
            <span>{{ item.detail }}</span>
          </button>
        </section>
      </template>
      <p
        v-else
        class="quick-search-dialog__empty"
      >
        没有匹配的配置。
      </p>
    </div>
  </BaseDialog>
</template>

<style scoped lang="scss">
:deep(.quick-search-backdrop) {
  position: fixed;
  inset: 0;
  z-index: 999;
  display: grid;
  place-items: start center;
  padding: min(12vh, 96px) var(--md-sys-spacing-xl) var(--md-sys-spacing-xl);
  background: var(--md-sys-color-scrim);
}

:deep(.quick-search-dialog) {
  width: min(680px, 100%);
  min-height: 0;
  max-height: min(70vh, 620px);
  overflow: hidden;
}

.quick-search-dialog__field {
  display: flex;
  align-items: center;
  min-height: var(--md-sys-size-field-height);
  margin: 0;
  padding: 0 var(--md-sys-spacing-md);
  border: 1px solid var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-small);
  background: var(--md-sys-color-surface-container-highest);
  color: var(--md-sys-color-on-surface-variant);
  transition:
    border-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    box-shadow var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:focus-within {
    border-color: var(--md-sys-color-primary);
    box-shadow: 0 0 0 2px var(--md-sys-color-primary);
    color: var(--md-sys-color-primary);
  }
}

.quick-search-dialog__search-icon {
  flex: 0 0 auto;
  width: 20px;
  height: 20px;
}

.quick-search-dialog__input {
  width: 100%;
  min-width: 0;
  min-height: 40px;
  padding: 0 0 0 var(--md-sys-spacing-sm);
  border: 0;
  outline: 0;
  background: transparent;
  color: var(--md-sys-color-on-surface);
  font-size: var(--md-sys-typescale-body-medium-size);

  &::placeholder {
    color: var(--md-sys-color-on-surface-variant);
  }
}

.quick-search-dialog__results {
  display: grid;
  gap: var(--md-sys-spacing-lg);
  max-height: min(48vh, 420px);
  overflow-y: auto;
  padding: var(--md-sys-spacing-lg) 0 0;
}

.quick-search-group {
  display: grid;
  gap: var(--md-sys-spacing-xs);

  h3 {
    margin: 0 0 var(--md-sys-spacing-xs);
    color: var(--md-sys-color-on-surface-variant);
    font-size: var(--md-sys-typescale-label-medium-size);
    line-height: var(--md-sys-typescale-label-medium-line-height);
    font-weight: var(--md-sys-typescale-label-medium-weight);
  }
}

.quick-search-result {
  display: grid;
  gap: 2px;
  width: 100%;
  padding: var(--md-sys-spacing-sm) var(--md-sys-spacing-md);
  border: 0;
  border-radius: var(--md-sys-shape-corner-small);
  background: transparent;
  color: var(--md-sys-color-on-surface);
  text-align: left;
  cursor: pointer;
  transition: background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover {
    background: var(--md-sys-color-surface-container);
  }

  &:focus-visible {
    outline: 2px solid var(--md-sys-color-primary);
    outline-offset: -2px;
  }

  &.is-highlighted {
    background: var(--md-sys-color-secondary-container);
    color: var(--md-sys-color-on-secondary-container);

    span {
      color: var(--md-sys-color-on-secondary-container);
    }
  }

  strong,
  span {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  strong {
    font-size: var(--md-sys-typescale-body-medium-size);
    line-height: var(--md-sys-typescale-body-medium-line-height);
    font-weight: 500;
  }

  span {
    color: var(--md-sys-color-on-surface-variant);
    font-size: var(--md-sys-typescale-body-small-size);
    line-height: var(--md-sys-typescale-body-small-line-height);
  }
}

.quick-search-dialog__empty {
  margin: 0;
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-medium-size);
  text-align: center;
}
</style>
