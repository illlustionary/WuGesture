<script setup>
import SidebarAppStatus from '@/components/SidebarAppStatus.vue'
import SidebarApplicationSection from '@/components/SidebarApplicationSection.vue'
import SidebarCategorySection from '@/components/SidebarCategorySection.vue'
import SidebarEdgeSection from '@/components/SidebarEdgeSection.vue'
import SidebarFixedNavigation from '@/components/SidebarFixedNavigation.vue'
import SidebarFooter from '@/components/SidebarFooter.vue'
import SidebarSettingsSection from '@/components/SidebarSettingsSection.vue'
import { useSidebarOverlay } from '@/composables/useSidebarOverlay'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'

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
const {
  isCollapsed,
  isInLayout,
  isCollapsing,
  isOverlayMounted,
  isOverlayVisible,
  isSidebarVisible,
  isSidebarHidden,
  showOverlay,
  cancelPendingOverlay,
  hideOverlay,
  toggleSidebar,
  completeTransition
} = useSidebarOverlay(rulesStore)
</script>

<template>
  <div
    v-show="isCollapsed"
    class="app-sidebar__hotzone"
    aria-hidden="true"
    @pointerenter="showOverlay"
    @pointerleave="cancelPendingOverlay"
  />
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
        @export-config="emit('export-config')"
        @import-config="emit('import-config')"
        @reset-settings="emit('reset-settings')"
      />
    </nav>
    <SidebarFooter
      :is-collapsed="isCollapsed"
      :is-dark-theme="props.isDarkTheme"
      @open-help="emit('open-help')"
      @open-search="emit('open-search')"
      @toggle-theme="emit('toggle-theme')"
      @toggle-sidebar="toggleSidebar"
    />
  </aside>
</template>

<style lang="scss">
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
    opacity 180ms ease,
    transform 180ms ease;

  &.is-collapsing {
    opacity: 0;
    pointer-events: none;
    transform: translateX(-20px);
  }

  &.is-detached {
    position: fixed;
    z-index: 30;
    inset: 0 auto 0 0;
    width: 238px;
    min-width: 238px;
    visibility: hidden;
    opacity: 0;
    pointer-events: none;
    transform: translateX(-20px);
  }

  &.is-overlay-mounted {
    visibility: visible;
    box-shadow: var(--shadow-float);
  }

  &.is-overlay-visible {
    opacity: 1;
    pointer-events: auto;
    transform: translateX(0);
  }
}

.app-sidebar__hotzone {
  position: fixed;
  z-index: 29;
  inset: 0 auto 0 0;
  width: 15px;
}

.app-sidebar__nav {
  display: flex;
  min-height: 0;
  flex: 1 1 auto;
  flex-direction: column;
  gap: 4px;
  overflow-y: auto;
  scrollbar-width: thin;

  > .hover-bubble-trigger {
    display: flex;
    width: 100%;
  }
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

  svg {
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
  background: transparent;

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
.sidebar-child:focus-within .sidebar-child__delete,
.sidebar-child:has(.sidebar-child__link.is-active) .sidebar-child__delete {
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
  svg {
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
.app-sidebar.is-collapsed .app-sidebar__collapse svg {
  transform: rotate(180deg);
}
</style>
