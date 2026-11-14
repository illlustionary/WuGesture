<script setup lang="ts">
import SidebarAppStatus from '@/components/sidebar/SidebarAppStatus.vue'
import SidebarAboutLink from '@/components/sidebar/SidebarAboutLink.vue'
import SidebarApplicationSection from '@/components/sidebar/SidebarApplicationSection.vue'
import SidebarCategorySection from '@/components/sidebar/SidebarCategorySection.vue'
import SidebarEdgeSection from '@/components/sidebar/SidebarEdgeSection.vue'
import SidebarFixedNavigation from '@/components/sidebar/SidebarFixedNavigation.vue'
import SidebarFooter from '@/components/sidebar/SidebarFooter.vue'
import SidebarSettingsSection from '@/components/sidebar/SidebarSettingsSection.vue'
import { useSidebarOverlay } from '@/composables/useSidebarOverlay'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import type { ThemeMode } from '@/theme/scheme'

const props = withDefaults(
  defineProps<{
    isDarkTheme?: boolean
    themeMode?: ThemeMode
  }>(),
  {
    isDarkTheme: false,
    themeMode: 'system'
  }
)

const emit = defineEmits<{
  'open-help': []
  'open-search': []
  'open-category-dialog': []
  'open-category-rename': [category: unknown]
  'delete-category': [name: string]
  'open-app-picker': []
  'open-app-rename': [app: unknown]
  'delete-app': [name: string]
  'cycle-theme': []
}>()

const rulesStore = useGestureEditorContext()
const {
  isCollapsed,
  isInLayout,
  isCollapsing,
  isOverlayMounted,
  isOverlayVisible,
  isSidebarVisible,
  isSidebarHidden,
  showOverlay,
  hideOverlay,
  toggleSidebar,
  completeTransition
} = useSidebarOverlay(rulesStore)
</script>

<template>
  <aside
    class="app-sidebar"
    :class="{
      'is-collapsed': isCollapsed,
      'is-collapsing': isCollapsing,
      'is-detached': !isInLayout,
      'is-overlay-mounted': isOverlayMounted,
      'is-overlay-visible': isOverlayVisible
    }"
    :aria-hidden="isSidebarHidden"
    :inert="isSidebarHidden"
    @pointerenter="showOverlay"
    @pointerleave="hideOverlay"
    @transitionend.self="completeTransition"
  >
    <SidebarAppStatus />
    <nav
      class="app-sidebar__nav"
      aria-label="应用页面"
    >
      <SidebarFixedNavigation
        :is-sidebar-hidden="isSidebarHidden"
        :is-sidebar-visible="isSidebarVisible"
      />
      <SidebarCategorySection
        :is-sidebar-hidden="isSidebarHidden"
        :is-sidebar-visible="isSidebarVisible"
        @open-create="emit('open-category-dialog')"
        @open-rename="emit('open-category-rename', $event)"
        @delete="emit('delete-category', $event)"
      />
      <SidebarApplicationSection
        :is-sidebar-hidden="isSidebarHidden"
        :is-sidebar-visible="isSidebarVisible"
        @open-picker="emit('open-app-picker')"
        @open-rename="emit('open-app-rename', $event)"
        @delete="emit('delete-app', $event)"
      />
      <SidebarEdgeSection
        :is-sidebar-hidden="isSidebarHidden"
        :is-sidebar-visible="isSidebarVisible"
      />
      <SidebarSettingsSection
        :is-sidebar-hidden="isSidebarHidden"
        :is-sidebar-visible="isSidebarVisible"
      />
      <SidebarAboutLink :is-sidebar-hidden="isSidebarHidden" />
    </nav>
    <SidebarFooter
      :is-collapsed="isCollapsed"
      :is-dark-theme="props.isDarkTheme"
      :theme-mode="props.themeMode"
      @open-help="emit('open-help')"
      @open-search="emit('open-search')"
      @cycle-theme="emit('cycle-theme')"
      @toggle-sidebar="toggleSidebar"
    />
  </aside>
</template>

<style lang="scss">
.app-sidebar {
  display: flex;
  flex: 0 0 var(--md-sys-size-sidebar-width);
  flex-direction: column;
  min-width: var(--md-sys-size-sidebar-width);
  height: 100%;
  padding: var(--md-sys-spacing-md);
  border-right: 1px solid var(--md-sys-color-outline-variant);
  background: var(--md-sys-color-surface-container-low);
  transition:
    opacity 160ms ease,
    transform 160ms ease;

  &.is-collapsing {
    opacity: 0;
    pointer-events: none;
    transform: translateX(-20px);
  }

  &.is-detached {
    position: fixed;
    z-index: 30;
    inset: 0 auto 0 0;
    width: var(--md-sys-size-sidebar-width);
    min-width: var(--md-sys-size-sidebar-width);
    visibility: hidden;
    opacity: 0;
    pointer-events: none;
    transform: translateX(-20px);
    will-change: transform, opacity;
    transition:
      opacity 130ms ease-out,
      transform 130ms cubic-bezier(0.22, 1, 0.36, 1);
  }

  &.is-overlay-mounted {
    visibility: visible;
  }

  &.is-overlay-visible {
    opacity: 1;
    pointer-events: auto;
    transform: translateX(0);
  }
}

.app-sidebar__nav {
  display: flex;
  min-height: 0;
  flex: 1 1 auto;
  flex-direction: column;
  gap: var(--md-sys-spacing-xs);
  padding: 0 var(--md-sys-spacing-xs);
  overflow-y: auto;
  scrollbar-width: thin;

  /* Only the navigation itself scrolls: every section keeps its natural height instead of being
     squeezed by the flex container, so an expanded program list never gets its own scrollbar. */
  > * {
    flex: 0 0 auto;
  }

  > .hover-bubble-trigger {
    display: flex;
    width: 100%;
  }
}

/* NavigationDrawer 一级项：48 高、full 圆角、激活 secondaryContainer 胶囊。 */
.app-sidebar__item,
.sidebar-group__head {
  position: relative;
  display: flex;
  align-items: center;
  gap: var(--md-sys-spacing-md);
  width: 100%;
  min-height: var(--md-sys-size-sidebar-item);
  padding: 0 var(--md-sys-spacing-lg);
  border: 0;
  border-radius: var(--md-sys-shape-corner-full);
  color: var(--md-sys-color-on-surface-variant);
  font-family: inherit;
  font-size: var(--md-sys-typescale-label-large-size);
  line-height: var(--md-sys-typescale-label-large-line-height);
  font-weight: var(--md-sys-typescale-label-large-weight);
  text-decoration: none;
  background: transparent;
  cursor: pointer;
  transition:
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover {
    color: var(--md-sys-color-on-surface);
    background: color-mix(in srgb, var(--md-sys-color-on-surface) 8%, transparent);
  }

  &:focus-visible {
    outline-offset: -2px;
  }

  &.is-active {
    color: var(--md-sys-color-on-secondary-container);
    background: var(--md-sys-color-secondary-container);
  }
}

.app-sidebar__item-icon {
  width: 20px;
  height: 20px;
  flex: 0 0 auto;
  fill: none;
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
  margin-right: 4px;
  border-right: 1.5px solid currentColor;
  border-bottom: 1.5px solid currentColor;
  transform: rotate(45deg) translateY(-2px);
  transition: transform var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);
}

.sidebar-group.is-expanded .sidebar-group__chevron {
  transform: rotate(225deg) translate(-1px, -1px);
}

.sidebar-group.is-active .sidebar-group__head {
  color: var(--md-sys-color-on-surface);
  background: transparent;

  &:hover {
    background: color-mix(in srgb, var(--md-sys-color-on-surface) 8%, transparent);
  }
}

.sidebar-group__body {
  display: grid;
  gap: 2px;
  padding: 2px 0 6px var(--md-sys-spacing-lg);
}

.sidebar-group__toolbar {
  display: flex;
  align-items: center;
  gap: var(--md-sys-spacing-xs);
  padding: 2px 4px 4px 0;
}

.sidebar-group__search {
  display: flex;
  min-width: 0;
  flex: 1;
  align-items: center;
  gap: var(--md-sys-spacing-sm);
  min-height: 36px;
  padding: 0 var(--md-sys-spacing-md);
  border: 1px solid transparent;
  border-radius: var(--md-sys-shape-corner-full);
  background: var(--md-sys-color-surface-container-highest);
  color: var(--md-sys-color-on-surface-variant);
  transition:
    border-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:focus-within {
    border-color: var(--md-sys-color-primary);
    background: var(--md-sys-color-surface-container-high);
  }

  svg {
    width: 16px;
    height: 16px;
    flex: 0 0 auto;
    fill: none;
    stroke: currentColor;
  }

  input {
    width: 100%;
    min-width: 0;
    min-height: 32px;
    padding: 0;
    border: 0;
    outline: 0;
    background: transparent;
    color: var(--md-sys-color-on-surface);
    font-size: var(--md-sys-typescale-body-medium-size);
  }
}

.sidebar-group__action {
  min-width: 36px;
  min-height: 36px;
}

.sidebar-group__children {
  display: grid;
  gap: 2px;
}

.sidebar-child {
  display: flex;
  align-items: center;
  min-width: 0;
  border-radius: var(--md-sys-shape-corner-full);
  transition: background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover,
  &:focus-within {
    background: color-mix(in srgb, var(--md-sys-color-on-surface) 6%, transparent);
  }
}

.sidebar-child__link {
  display: flex;
  align-items: center;
  gap: var(--md-sys-spacing-sm);
  min-width: 0;
  flex: 1;
  min-height: 40px;
  padding: 0 var(--md-sys-spacing-md);
  border-radius: var(--md-sys-shape-corner-full);
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-medium-size);
  line-height: var(--md-sys-typescale-body-medium-line-height);
  text-decoration: none;
  transition:
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover {
    color: var(--md-sys-color-on-surface);
  }

  &:focus-visible {
    outline-offset: -2px;
  }

  &.is-active {
    color: var(--md-sys-color-on-secondary-container);
    background: var(--md-sys-color-secondary-container);
    font-weight: var(--md-sys-typescale-label-large-weight);
  }
}

.sidebar-child__icon {
  display: grid;
  width: 24px;
  height: 24px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: var(--md-sys-shape-corner-small);
  background: color-mix(in srgb, var(--md-sys-color-primary) 14%, transparent);
  color: var(--md-sys-color-primary);
  font-size: 11px;
  font-weight: 600;
}

.sidebar-child__svg-icon {
  padding: 4px;
  fill: none;
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
  color: var(--md-sys-color-outline);
  font-size: var(--md-sys-typescale-label-small-size);
}

.sidebar-child__delete {
  min-width: 28px;
  min-height: 28px;
  margin-right: 4px;
  color: var(--md-sys-color-error);
  opacity: 0;
  transition: opacity var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);
}

.sidebar-child:hover .sidebar-child__delete,
.sidebar-child:focus-within .sidebar-child__delete,
.sidebar-child:has(.sidebar-child__link.is-active) .sidebar-child__delete {
  opacity: 1;
}

.sidebar-group__empty {
  padding: 6px var(--md-sys-spacing-md);
  color: var(--md-sys-color-outline);
  font-size: var(--md-sys-typescale-body-small-size);
}

.app-sidebar__footer {
  display: flex;
  justify-content: space-between;
  margin-top: var(--md-sys-spacing-sm);
  padding: var(--md-sys-spacing-xs) 0 0;
}

.app-sidebar__footer-actions,
.app-sidebar__footer-controls {
  display: flex;
  gap: 2px;
}

.app-sidebar__collapse,
.app-sidebar__theme,
.app-sidebar__search,
.app-sidebar__help {
  display: grid;
  width: 40px;
  height: 40px;
  place-items: center;
  border: 0;
  border-radius: var(--md-sys-shape-corner-full);
  background: transparent;
  color: var(--md-sys-color-on-surface-variant);
  cursor: pointer;
  transition:
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover {
    color: var(--md-sys-color-on-surface);
    background: color-mix(in srgb, var(--md-sys-color-on-surface) 8%, transparent);
  }

  &:focus-visible {
    outline-offset: -2px;
  }

  svg {
    width: 20px;
    height: 20px;
    fill: none;
    stroke: currentColor;
  }
}

.app-sidebar__help {
  font-size: 17px;
  font-weight: 600;
  line-height: 1;
}

.app-sidebar.is-collapsed .app-sidebar__collapse svg {
  transform: rotate(180deg);
}
</style>
