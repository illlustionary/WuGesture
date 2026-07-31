<script setup>
import { computed, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import AppIcon from '@/components/AppIcon.vue'
import HoverBubble from '@/components/HoverBubble.vue'
import IconActionButton from '@/components/IconActionButton.vue'
import { getCategoryIcon } from '@/pages/category/composables/useCategoryPage'
import appIconUrl from '../../../Resources/wu.jpg'

const props = defineProps({
  isDarkTheme: { type: Boolean, default: false }
})

const emit = defineEmits([
  'open-help',
  'open-search',
  'open-category-dialog',
  'open-category-rename',
  'delete-category',
  'open-app-picker',
  'open-app-rename',
  'delete-app',
  'export-config',
  'import-config',
  'reset-settings',
  'toggle-theme'
])

const rulesStore = useGestureEditorContext()
const route = useRoute()
const isCollapsed = ref(false)
const searchValues = reactive({ category: '', app: '' })
const expanded = reactive({
  category: false,
  app: false,
  edge: false,
  settings: false
})

const directItems = [
  { to: '/global', label: '全局', icon: 'mouse' },
  { to: '/exclusions', label: '排除项', icon: 'close-alt' }
]

const fixedGroups = [
  {
    key: 'edge',
    label: '边缘操作',
    icon: 'circle-dashed',
    children: [
      { to: '/edge/corner', label: '触发角', icon: 'circle-dashed' },
      { to: '/edge/friction', label: '摩擦边', icon: 'circle-dashed' },
      { to: '/edge/wheel', label: '边缘滚动', icon: 'mouse' }
    ]
  },
  {
    key: 'settings',
    label: '设置',
    icon: 'setting',
    children: [
      { to: '/settings/mouse-trail', label: '轨迹线', icon: 'circle-dashed' },
      { to: '/settings/gesture-hint', label: '手势提示', icon: 'mouse' },
      { to: '/settings/level-osd', label: '音量/亮度提示', icon: 'setting' },
      { to: '/settings/sensitivity', label: '手势灵敏度', icon: 'setting' },
      { to: '/settings/app-behavior', label: '应用行为', icon: 'briefcase' },
      { to: '/settings/webdav', label: 'WebDAV', icon: 'setting' }
    ]
  }
]

const categoryItems = computed(() => rulesStore.categoryItems)
const appItems = computed(() => rulesStore.appItems)

const dynamicGroups = computed(() => [
  {
    key: 'category',
    label: '分类',
    icon: 'folder',
    searchable: true,
    children: categoryItems.value
  },
  {
    key: 'app',
    label: '程序',
    icon: 'briefcase',
    searchable: true,
    children: appItems.value
  }
])

const allGroups = computed(() => [...dynamicGroups.value, ...fixedGroups])

const collapseLabel = computed(() => (isCollapsed.value ? '展开侧栏' : '收起侧栏'))
const themeLabel = computed(() => (props.isDarkTheme ? '切换到浅色模式' : '切换到深色模式'))
const isPaused = computed(() => rulesStore.state.statusState === 'paused')
const pauseLabel = computed(() => (isPaused.value ? '恢复 WuGesture' : '暂停 WuGesture'))

function routeGroup(path) {
  if (path.startsWith('/category')) return 'category'
  if (path.startsWith('/app')) return 'app'
  if (path.startsWith('/edge')) return 'edge'
  if (path.startsWith('/settings')) return 'settings'
  return ''
}

function isGroupActive(key) {
  return routeGroup(route.path) === key
}

function toggleGroup(key) {
  expanded[key] = !expanded[key]
}

function filteredChildren(group) {
  const value = String(searchValues[group.key] ?? '')
    .trim()
    .toLowerCase()
  if (!value) return group.children

  return group.children.filter(item => {
    const text = group.key === 'app' ? [item.displayName, item.name, item.path].join(' ') : item.name
    return text.toLowerCase().includes(value)
  })
}

function childLabel(group, item) {
  if (!group.searchable) {
    return item.label
  }

  if (group.key === 'app') {
    return item.displayName || item.name || item.path
  }

  return item.name
}

function childMeta(group, item) {
  if (group.key === 'app' && item.displayName && item.name !== item.displayName) {
    return item.name
  }

  return ''
}

function childTo(group, item) {
  if (!group.searchable) {
    return item.to
  }

  return {
    name: group.key === 'category' ? 'category-scope' : 'app-scope',
    params: { name: item.name }
  }
}

function childKey(group, item) {
  return group.searchable ? item.name : item.to
}

function handleChildDblClick(group, item) {
  if (group.key === 'category') {
    emit('open-category-rename', item)
  } else if (group.key === 'app') {
    emit('open-app-rename', item)
  }
}

function openGroupAction(key) {
  if (key === 'category') {
    emit('open-category-dialog')
  } else if (key === 'app') {
    emit('open-app-picker')
  }
}

function syncExpanded(path) {
  const key = routeGroup(path)
  if (key) expanded[key] = true
}

watch(() => route.path, syncExpanded, { immediate: true })
</script>

<template>
  <aside
    class="app-sidebar"
    :class="{ 'is-collapsed': isCollapsed }"
  >
    <HoverBubble
      :text="pauseLabel"
      class="app-sidebar__pause-trigger"
    >
      <button
        type="button"
        class="app-sidebar__pause"
        :class="{ 'is-paused': isPaused }"
        :aria-label="pauseLabel"
        :aria-pressed="isPaused"
        @click="rulesStore.toggleUserPaused"
      >
        <img
          :src="appIconUrl"
          class="app-sidebar__pause-icon"
          alt=""
          aria-hidden="true"
        />
        <span>WuGesture</span>
      </button>
    </HoverBubble>

    <nav
      class="app-sidebar__nav"
      aria-label="应用页面"
    >
      <HoverBubble
        v-for="item in directItems"
        :key="item.to"
        :text="isCollapsed ? item.label : ''"
      >
        <RouterLink
          :to="item.to"
          class="app-sidebar__item"
          exact-active-class="is-active"
          :aria-label="isCollapsed ? item.label : undefined"
        >
          <AppIcon
            :name="item.icon"
            class="app-sidebar__item-icon"
            aria-hidden="true"
          />
          <span v-if="!isCollapsed">{{ item.label }}</span>
        </RouterLink>
      </HoverBubble>

      <section
        v-for="group in allGroups"
        :key="group.key"
        class="sidebar-group"
        :class="{ 'is-expanded': expanded[group.key], 'is-active': isGroupActive(group.key) }"
      >
        <button
          type="button"
          class="sidebar-group__head"
          :aria-expanded="!isCollapsed && expanded[group.key]"
          :aria-label="isCollapsed ? group.label : undefined"
          @click="toggleGroup(group.key)"
        >
          <AppIcon
            :name="group.icon"
            class="app-sidebar__item-icon"
            aria-hidden="true"
          />
          <span
            v-if="!isCollapsed"
            class="sidebar-group__title"
            >{{ group.label }}</span
          >
          <span
            v-if="!isCollapsed"
            class="sidebar-group__chevron"
            aria-hidden="true"
          />
        </button>

        <div
          v-if="!isCollapsed && expanded[group.key]"
          class="sidebar-group__body"
        >
          <div class="sidebar-group__toolbar">
            <div
              v-if="group.searchable"
              class="sidebar-group__search"
            >
              <AppIcon
                name="search"
                aria-hidden="true"
              />
              <input
                v-model="searchValues[group.key]"
                type="search"
                :aria-label="`搜索${group.label}`"
                :placeholder="`搜索${group.label}`"
              />
            </div>
            <HoverBubble
              v-if="group.searchable"
              text="新增"
            >
              <IconActionButton
                icon="add"
                label="新增"
                :show-tooltip="false"
                class="sidebar-group__action"
                @click="openGroupAction(group.key)"
              />
            </HoverBubble>
            <template v-if="group.key === 'settings'">
              <HoverBubble text="导出配置">
                <IconActionButton
                  icon="download"
                  label="导出配置"
                  :show-tooltip="false"
                  class="sidebar-group__action"
                  @click="emit('export-config')"
                />
              </HoverBubble>
              <HoverBubble text="导入配置">
                <IconActionButton
                  icon="upload"
                  label="导入配置"
                  :show-tooltip="false"
                  class="sidebar-group__action"
                  @click="emit('import-config')"
                />
              </HoverBubble>
              <HoverBubble text="恢复默认设置">
                <IconActionButton
                  icon="reset"
                  label="恢复默认设置"
                  :show-tooltip="false"
                  color="var(--danger)"
                  class="sidebar-group__action"
                  @click="emit('reset-settings')"
                />
              </HoverBubble>
            </template>
          </div>

          <div
            v-if="group.children.length"
            class="sidebar-group__children"
          >
            <template v-if="filteredChildren(group).length">
              <div
                v-for="item in filteredChildren(group)"
                :key="childKey(group, item)"
                class="sidebar-child"
                @dblclick.prevent="handleChildDblClick(group, item)"
              >
                <RouterLink
                  :to="childTo(group, item)"
                  class="sidebar-child__link"
                  exact-active-class="is-active"
                >
                  <img
                    v-if="group.key === 'app' && item.icon"
                    :src="item.icon"
                    class="sidebar-child__icon app-icon"
                    alt=""
                  />
                  <AppIcon
                    :name="group.key === 'category' ? getCategoryIcon(item.name) : item.icon"
                    v-else-if="group.key === 'category' || item.icon"
                    class="sidebar-child__icon sidebar-child__svg-icon"
                    aria-hidden="true"
                  />
                  <span
                    v-else
                    class="sidebar-child__icon sidebar-child__icon--fallback"
                    aria-hidden="true"
                  >
                    {{ group.key === 'app' ? childLabel(group, item).slice(0, 1).toUpperCase() : '·' }}
                  </span>
                  <span class="sidebar-child__copy">
                    <span class="sidebar-child__label">{{ childLabel(group, item) }}</span>
                    <span
                      v-if="childMeta(group, item)"
                      class="sidebar-child__meta"
                      >{{ childMeta(group, item) }}</span
                    >
                  </span>
                </RouterLink>
                <IconActionButton
                  v-if="group.searchable"
                  icon="delete"
                  :label="group.key === 'category' ? '删除分类' : '删除程序'"
                  class="sidebar-child__delete"
                  @click="group.key === 'category' ? emit('delete-category', item.name) : emit('delete-app', item.name)"
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
            >暂无{{ group.label }}</span
          >
        </div>
      </section>
    </nav>

    <div class="app-sidebar__footer">
      <div class="app-sidebar__footer-controls">
        <HoverBubble :text="collapseLabel">
          <button
            type="button"
            class="app-sidebar__collapse"
            :aria-label="collapseLabel"
            :aria-pressed="isCollapsed"
            @click="isCollapsed = !isCollapsed"
          >
            <AppIcon
              name="panel-left"
              aria-hidden="true"
            />
          </button>
        </HoverBubble>
        <HoverBubble :text="themeLabel">
          <button
            type="button"
            class="app-sidebar__theme"
            :aria-label="themeLabel"
            :aria-pressed="props.isDarkTheme"
            @click="emit('toggle-theme')"
          >
            <AppIcon
              v-if="props.isDarkTheme"
              name="sun"
              aria-hidden="true"
            />
            <AppIcon
              v-else
              name="moon"
              aria-hidden="true"
            />
          </button>
        </HoverBubble>
      </div>
      <div class="app-sidebar__footer-actions">
        <HoverBubble text="搜索">
          <button
            type="button"
            class="app-sidebar__search"
            aria-label="搜索"
            @click="emit('open-search')"
          >
            <AppIcon
              name="search"
              aria-hidden="true"
            />
          </button>
        </HoverBubble>
        <HoverBubble text="规则生效顺序">
          <button
            type="button"
            class="app-sidebar__help"
            aria-label="规则生效顺序"
            @click="emit('open-help')"
          >
            ?
          </button>
        </HoverBubble>
      </div>
    </div>
  </aside>
</template>

<style scoped lang="scss">
.app-sidebar {
  display: flex;
  flex: 0 0 238px;
  flex-direction: column;
  min-width: 238px;
  height: 100%;
  padding: 14px 10px;
  border-right: 1px solid var(--border);
  background: var(--panel);
  transition:
    flex-basis 180ms ease,
    min-width 180ms ease,
    padding 180ms ease;

  &.is-collapsed {
    flex-basis: 68px;
    min-width: 68px;
    padding-right: 10px;
    padding-left: 10px;
  }
}

.app-sidebar__nav {
  display: flex;
  min-height: 0;
  flex: 1 1 auto;
  flex-direction: column;
  gap: 4px;
  overflow-y: auto;
  scrollbar-width: thin;

  > :deep(.hover-bubble-trigger) {
    display: flex;
    width: 100%;
  }
}

.app-sidebar__pause {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  width: 100%;
  height: 40px;
  padding: 0;
  border: 0;
  border-radius: 8px;
  background: transparent;
  color: var(--text);
  cursor: pointer;
  transition: background-color 120ms ease;

  &:hover {
    background: var(--accent-soft);
  }

  &:focus-visible {
    outline: 2px solid var(--focus-ring);
    outline-offset: 2px;
  }

  &.is-paused {
    color: var(--muted);

    .app-sidebar__pause-icon,
    span {
      filter: grayscale(1);
      opacity: 0.54;
    }
  }
}

.app-sidebar__pause-trigger {
  display: flex;
  width: 100%;
  justify-content: center;
  margin-bottom: 8px;
}

.app-sidebar__pause-icon {
  display: block;
  width: 24px;
  height: 24px;
  object-fit: contain;
  transition:
    filter 120ms ease,
    opacity 120ms ease;
  border-radius: 50%;
}

.app-sidebar__pause span {
  font-size: 13px;
}

.app-sidebar__item,
.sidebar-group__head {
  position: relative;
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  min-height: 40px;
  padding: 0 12px;
  border: 0;
  border-radius: 10px;
  color: var(--muted);
  font-size: 14px;
  font-weight: 600;
  text-decoration: none;
  background: transparent;
  cursor: pointer;
  transition:
    background-color 120ms ease,
    color 120ms ease;

  &:hover {
    color: var(--text);
    background: var(--interactive-hover-bg);
  }

  &:focus-visible {
    outline: 2px solid var(--focus-ring);
    outline-offset: 2px;
  }

  &.is-active {
    color: var(--accent-strong);
    background: var(--accent-soft);

    &::after {
      transform: scaleY(1);
    }
  }

  &::after {
    content: '';
    position: absolute;
    top: 8px;
    bottom: 8px;
    left: 0;
    width: 3px;
    border-radius: 0 3px 3px 0;
    background: var(--accent-strong);
    transform: scaleY(0);
    transform-origin: center;
    transition: transform 180ms ease-in-out;
  }
}

.app-sidebar__item-icon {
  width: 18px;
  height: 18px;
  flex: 0 0 auto;
  fill: currentColor;

  :deep(svg) {
    width: 100%;
    height: 100%;
    fill: currentColor;
  }
}

.sidebar-group {
  display: grid;
  gap: 2px;
}

.sidebar-group__title {
  min-width: 0;
  flex: 1;
  text-align: left;
}

.sidebar-group__chevron {
  width: 7px;
  height: 7px;
  margin-right: 3px;
  border-right: 1.5px solid currentColor;
  border-bottom: 1.5px solid currentColor;
  transform: rotate(45deg) translateY(-2px);
  transition: transform 150ms ease;
}

.sidebar-group.is-expanded .sidebar-group__chevron {
  transform: rotate(225deg) translate(-1px, -1px);
}

.sidebar-group.is-active .sidebar-group__head {
  color: var(--accent-strong);
  background: var(--accent-soft);

  &::after {
    transform: scaleY(1);
  }
}

.sidebar-group__body {
  display: grid;
  gap: 5px;
  padding: 1px 0 4px 14px;
}

.sidebar-group__toolbar {
  display: flex;
  align-items: center;
  gap: 4px;
}

.sidebar-group__search {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
  flex: 1;
  min-height: 30px;
  padding: 0 8px;
  border: 1px solid var(--border-subtle);
  border-radius: 8px;
  background: var(--panel-control);
  color: var(--muted);

  svg {
    width: 14px;
    height: 14px;
    flex: 0 0 auto;
    fill: none;
    stroke: currentColor;
  }

  input {
    width: 100%;
    min-width: 0;
    min-height: 28px;
    padding: 0;
    border: 0;
    outline: 0;
    background: transparent;
    color: var(--text);
    font-size: 12px;
  }
}

.sidebar-group__action {
  min-width: 30px;
  min-height: 30px;
}

.sidebar-group__children {
  display: grid;
  gap: 2px;
  max-height: min(38vh, 360px);
  overflow-y: auto;
  scrollbar-width: thin;
}

.sidebar-child {
  display: flex;
  align-items: center;
  min-width: 0;
  border-radius: 8px;

  &:hover,
  &:focus-within {
    background: var(--interactive-hover-bg);
  }
}

.sidebar-child__link {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
  flex: 1;
  min-height: 34px;
  padding: 4px 5px;
  border-radius: 8px;
  color: var(--muted);
  font-size: 12px;
  text-decoration: none;

  &:hover {
    color: var(--text);
  }

  &.is-active {
    color: var(--accent-strong);
    font-weight: 700;
  }
}

.sidebar-child__icon {
  display: grid;
  width: 22px;
  height: 22px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 6px;
  background: var(--interactive-icon-bg);
  color: var(--accent-strong);
  font-size: 11px;
  font-weight: 700;
}

.sidebar-child__svg-icon {
  padding: 4px;
  fill: currentColor;
}

.sidebar-child__copy {
  display: grid;
  min-width: 0;
  gap: 1px;
}

.sidebar-child__label,
.sidebar-child__meta {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sidebar-child__meta {
  color: var(--text-subtle);
  font-size: 10px;
}

.sidebar-child__delete {
  min-width: 28px;
  min-height: 28px;
  margin-right: 2px;
  color: var(--danger);
  opacity: 0;
  transition: opacity 120ms ease;
}

.sidebar-child:hover .sidebar-child__delete,
.sidebar-child:focus-within .sidebar-child__delete {
  opacity: 1;
}

.sidebar-group__empty {
  padding: 5px;
  color: var(--text-subtle);
  font-size: 11px;
}

.app-sidebar__footer {
  display: flex;
  justify-content: space-between;
  margin-top: 6px;
  padding: 4px 2px 0;
}

.app-sidebar__footer-actions {
  display: flex;
  gap: 2px;
}

.app-sidebar__footer-controls {
  display: flex;
  gap: 2px;
}

.app-sidebar__collapse,
.app-sidebar__theme,
.app-sidebar__search,
.app-sidebar__help {
  display: grid;
  width: 36px;
  height: 36px;
  place-items: center;
  border: 0;
  border-radius: 10px;
  background: transparent;
  color: var(--muted);
  cursor: pointer;
  transition:
    background-color 120ms ease,
    color 120ms ease;

  &:hover {
    color: var(--text);
    background: var(--interactive-hover-bg);
  }

  &:focus-visible {
    outline: 2px solid var(--focus-ring);
    outline-offset: 2px;
  }

  :deep(svg) {
    width: 20px;
    height: 20px;
    fill: none;
    stroke: currentColor;
  }
}

.app-sidebar__help {
  font-size: 17px;
  font-weight: 700;
  line-height: 1;
}

.app-sidebar.is-collapsed {
  .app-sidebar__pause span {
    display: none;
  }

  .app-sidebar__collapse :deep(svg) {
    transform: rotate(180deg);
  }

  .app-sidebar__item,
  .sidebar-group__head {
    justify-content: center;
    padding: 0;
  }

  .app-sidebar__footer {
    flex-direction: column;
    align-items: center;
    gap: 2px;
  }

  .app-sidebar__footer-actions {
    flex-direction: column;
  }

  .app-sidebar__footer-controls {
    flex-direction: column;
  }
}
</style>
