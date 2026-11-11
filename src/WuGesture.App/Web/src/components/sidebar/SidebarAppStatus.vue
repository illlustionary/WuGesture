<script setup>
import { computed } from 'vue'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import HoverBubble from '@/components/ui/HoverBubble.vue'
import appIconUrl from '@resources/wu.jpg'

const rulesStore = useGestureEditorContext()
const isPaused = computed(() => rulesStore.state.statusState === 'paused')
const pauseLabel = computed(() => (isPaused.value ? '恢复 WuGesture' : '暂停 WuGesture'))
const appVersion = computed(() => rulesStore.state.appVersion || 'v0.0.0')

function togglePause() {
  rulesStore.toggleUserPaused()
}
</script>

<template>
  <HoverBubble
    :text="pauseLabel"
    class="sidebar-app-status"
  >
    <button
      type="button"
      class="sidebar-app-status__button"
      :class="{ 'is-paused': isPaused }"
      :aria-label="pauseLabel"
      :aria-pressed="isPaused"
      @click="togglePause"
    >
      <img
        :src="appIconUrl"
        class="sidebar-app-status__icon"
        alt=""
        aria-hidden="true"
      />
      <span class="sidebar-app-status__copy">
        <span class="sidebar-app-status__name-row">
          <span
            class="sidebar-app-status__indicator"
            :class="{ 'is-paused': isPaused }"
            aria-hidden="true"
          />
          <span class="sidebar-app-status__name">WuGesture</span>
        </span>
        <span class="sidebar-app-status__version">{{ appVersion }}</span>
      </span>
    </button>
  </HoverBubble>
</template>

<style scoped lang="scss">
.sidebar-app-status {
  display: flex;
  width: 100%;
  margin-bottom: var(--md-sys-spacing-sm);
}

.sidebar-app-status__button {
  display: flex;
  align-items: center;
  gap: var(--md-sys-spacing-md);
  width: 100%;
  min-height: 56px;
  padding: var(--md-sys-spacing-sm) var(--md-sys-spacing-md);
  border: 0;
  border-radius: var(--md-sys-shape-corner-full);
  background: transparent;
  color: var(--md-sys-color-on-surface);
  cursor: pointer;
  text-align: left;
  transition:
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover {
    background: color-mix(in srgb, var(--md-sys-color-on-surface) 8%, transparent);
  }

  &:focus-visible {
    outline-offset: -2px;
  }

  &.is-paused {
    color: var(--md-sys-color-on-surface-variant);

    .sidebar-app-status__icon,
    .sidebar-app-status__copy {
      filter: grayscale(1);
      opacity: 0.54;
    }
  }
}

.sidebar-app-status__icon {
  display: block;
  width: 32px;
  height: 32px;
  flex: 0 0 auto;
  border-radius: var(--md-sys-shape-corner-full);
  object-fit: contain;
  transition:
    filter 120ms ease,
    opacity 120ms ease;
}

.sidebar-app-status__copy {
  display: grid;
  min-width: 0;
  gap: 2px;
  transition:
    filter 120ms ease,
    opacity 120ms ease;
}

.sidebar-app-status__name-row {
  display: flex;
  align-items: center;
  min-width: 0;
  gap: 6px;
}

.sidebar-app-status__indicator {
  width: 6px;
  height: 6px;
  flex: 0 0 auto;
  border-radius: var(--md-sys-shape-corner-full);
  background: var(--md-sys-color-success);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--md-sys-color-success) 18%, transparent);

  &.is-paused {
    background: var(--md-sys-color-warning);
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--md-sys-color-warning) 18%, transparent);
  }
}

.sidebar-app-status__name,
.sidebar-app-status__version {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sidebar-app-status__name {
  font-size: var(--md-sys-typescale-label-large-size);
  font-weight: var(--md-sys-typescale-label-large-weight);
  line-height: 1.2;
}

.sidebar-app-status__version {
  color: var(--md-sys-color-outline);
  font-size: var(--md-sys-typescale-label-small-size);
  font-weight: 400;
  line-height: 1.2;
}
</style>
