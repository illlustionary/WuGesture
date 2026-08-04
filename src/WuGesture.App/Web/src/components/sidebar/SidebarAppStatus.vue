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
  margin-bottom: 8px;
}

.sidebar-app-status__button {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  min-height: 52px;
  padding: 8px 10px;
  border: 0;
  border-radius: 8px;
  background: transparent;
  color: var(--text);
  cursor: pointer;
  text-align: left;
  transition:
    background-color 120ms ease,
    color 120ms ease;

  &:hover {
    background: var(--interactive-hover-bg);
  }

  &:focus-visible {
    outline: 2px solid var(--focus-ring);
    outline-offset: 2px;
  }

  &.is-paused {
    color: var(--muted);

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
  border-radius: 50%;
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
  border-radius: 50%;
  background: #62c98d;
  box-shadow: 0 0 0 3px rgba(98, 201, 141, 0.12);

  &.is-paused {
    box-shadow: 0 0 0 3px rgba(214, 165, 71, 0.12);
  }
}

.sidebar-app-status__name,
.sidebar-app-status__version {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sidebar-app-status__name {
  font-size: 14px;
  font-weight: 700;
  line-height: 1.2;
}

.sidebar-app-status__version {
  color: var(--text-subtle);
  font-size: 11px;
  font-weight: 400;
  line-height: 1.2;
}
</style>
