<script setup>
import { computed, nextTick, ref, watch } from 'vue'
import BaseDialog from '@/components/BaseDialog.vue'
import SearchIcon from '@/assets/navigation/search.svg'

const props = defineProps({
  open: { type: Boolean, required: true },
  items: { type: Array, required: true }
})

const emit = defineEmits(['close', 'select'])
const query = ref('')
const input = ref(null)
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
const groupedItems = computed(() => {
  const groups = []
  const groupMap = new Map()

  for (const item of filteredItems.value) {
    if (!groupMap.has(item.group)) {
      const group = { name: item.group, items: [] }
      groupMap.set(item.group, group)
      groups.push(group)
    }
    groupMap.get(item.group).items.push(item)
  }

  return groups
})

watch(
  () => props.open,
  open => {
    if (!open) {
      query.value = ''
      return
    }

    nextTick(() => input.value?.focus())
  }
)

function selectItem(item) {
  emit('select', item)
}

function close() {
  emit('close')
}

function handleKeydown(event) {
  if (event.key === 'Escape') {
    event.preventDefault()
    close()
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
      <SearchIcon class="quick-search-dialog__search-icon" aria-hidden="true" />
      <input
        ref="input"
        v-model="query"
        class="quick-search-dialog__input"
        type="search"
        placeholder="搜索手势、分类、程序或排除项"
        aria-label="搜索配置"
      />
    </div>
    <div class="quick-search-dialog__results">
      <template v-if="groupedItems.length">
        <section v-for="group in groupedItems" :key="group.name" class="quick-search-group">
          <h3>{{ group.name }}</h3>
          <button
            v-for="item in group.items"
            :key="item.id"
            type="button"
            class="quick-search-result"
            @click="selectItem(item)"
          >
            <strong>{{ item.title }}</strong>
            <span>{{ item.detail }}</span>
          </button>
        </section>
      </template>
      <p v-else class="quick-search-dialog__empty">没有匹配的配置。</p>
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
  padding: min(12vh, 96px) 20px 20px;
  background: rgb(20 27 39 / 36%);
}

:deep(.quick-search-dialog) {
  width: min(680px, 100%);
  min-height: 0;
  max-height: min(70vh, 620px);
  overflow: hidden;
  border-radius: 16px;
}

.quick-search-dialog__field {
  display: flex;
  align-items: center;
  min-height: 42px;
  margin: 0;
  padding: 0 12px;
  border: 1px solid var(--border);
  border-radius: 10px;
  background: var(--interactive-bg);
  color: var(--muted);
  transition:
    border-color 120ms ease,
    background-color 120ms ease,
    box-shadow 120ms ease,
    color 120ms ease;

  &:hover {
    border-color: var(--border-strong);
    background: var(--interactive-hover-bg);
  }

  &:focus-within {
    border-color: var(--accent-border-strong);
    background: var(--panel-solid);
    box-shadow: 0 0 0 4px var(--focus-ring);
    color: var(--accent-strong);
  }
}

.quick-search-dialog__search-icon {
  width: 18px;
  height: 18px;
  flex: 0 0 auto;
}

.quick-search-dialog__input {
  width: 100%;
  min-width: 0;
  min-height: 40px;
  padding: 0 0 0 10px;
  border: 0;
  outline: 0;
  background: transparent;
  color: var(--text);
  font-size: 14px;

  &::placeholder {
    color: var(--text-subtle);
  }
}

.quick-search-dialog__results {
  display: grid;
  gap: 16px;
  max-height: min(48vh, 420px);
  overflow-y: auto;
  padding: 16px 0 0;
}

.quick-search-group {
  display: grid;
  gap: 6px;

  h3 {
    margin: 0 0 2px;
    color: var(--muted);
    font-size: 12px;
    font-weight: 700;
  }
}

.quick-search-result {
  display: grid;
  gap: 4px;
  width: 100%;
  padding: 10px 12px;
  border: 1px solid var(--border-subtle);
  border-radius: 10px;
  background: var(--interactive-bg);
  color: var(--text);
  text-align: left;

  &:hover,
  &:focus-visible {
    border-color: var(--accent-border);
    background: var(--interactive-hover-bg);
  }

  strong,
  span {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  strong {
    font-size: 13px;
  }

  span {
    color: var(--muted);
    font-size: 12px;
  }
}

.quick-search-dialog__empty {
  margin: 0;
  color: var(--muted);
  font-size: 13px;
  text-align: center;
}
</style>
