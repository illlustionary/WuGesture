<script setup>
import { computed } from 'vue'
import AppIcon from '@/components/AppIcon.vue'
import HoverBubble from '@/components/HoverBubble.vue'

const props = defineProps({
  appInfo: {
    type: Object,
    default: () => ({ version: '预览版', icon: '' })
  },
  isMaximized: {
    type: Boolean,
    default: false
  },
  statusState: {
    type: String,
    required: true
  },
  isWindowResizing: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits([
  'close-window',
  'start-window-drag',
  'toggle-gesture-paused',
  'toggle-window-maximize',
  'minimize-window'
])

const statusLabel = computed(() => (props.statusState === 'paused' ? '恢复 WuGesture' : '暂停 WuGesture'))
const maximizeLabel = computed(() => (props.isMaximized ? '还原窗口' : '最大化窗口'))

function startWindowDrag(event) {
  if (event.button === 0 && event.detail < 2) {
    emit('start-window-drag')
  }
}

function toggleWindowMaximize() {
  emit('toggle-window-maximize')
}
</script>

<template>
  <header class="app-titlebar">
    <div class="app-titlebar__brand-area">
      <HoverBubble :text="statusLabel" :disabled="isWindowResizing">
        <button
          type="button"
          class="app-titlebar__brand"
          :data-state="statusState"
          :aria-label="statusLabel"
          :aria-pressed="statusState === 'paused'"
          tabindex="-1"
          @click="emit('toggle-gesture-paused')"
        >
          <span class="app-titlebar__brand-icon">
            <img
              v-if="appInfo.icon"
              :src="appInfo.icon"
              alt=""
            />
            <AppIcon
              v-else
              name="mouse"
              aria-hidden="true"
            />
          </span>
          <span class="app-titlebar__brand-version">{{ appInfo.version }}</span>
        </button>
      </HoverBubble>
    </div>

    <div
      class="app-titlebar__drag-region"
      aria-hidden="true"
      @pointerdown="startWindowDrag"
      @dblclick="toggleWindowMaximize"
    />

    <div class="app-titlebar__window-actions">
      <button
        type="button"
        class="app-titlebar__window-button"
        aria-label="最小化"
        @click="emit('minimize-window')"
      >
        <AppIcon name="minimize" aria-hidden="true" />
      </button>
      <button
        type="button"
        class="app-titlebar__window-button"
        :aria-label="maximizeLabel"
        @click="toggleWindowMaximize"
      >
        <AppIcon
          v-if="isMaximized"
          name="restore"
          aria-hidden="true"
        />
        <AppIcon
          v-else
          name="maximize"
          aria-hidden="true"
        />
      </button>
      <button
        type="button"
        class="app-titlebar__window-button app-titlebar__window-button--close"
        aria-label="关闭"
        @click="emit('close-window')"
      >
        <AppIcon name="close" aria-hidden="true" />
      </button>
    </div>
  </header>
</template>

<style scoped lang="scss">
.app-titlebar {
  display: flex;
  flex: 0 0 52px;
  min-height: 52px;
  border-bottom: 1px solid var(--border);
  background: var(--panel);
}

.app-titlebar__brand-area {
  display: flex;
  width: 220px;
  min-width: 220px;
  padding: 6px 10px;
}

.app-titlebar__brand-area > :deep(.hover-bubble-trigger) {
  display: flex;
  width: 100%;
}

.app-titlebar__brand {
  display: flex;
  align-items: center;
  gap: 10px;
  height: 100%;
  width: 100%;
  min-width: 0;
  padding: 0 8px;
  border: 0;
  border-radius: 10px;
  background: transparent;
  color: var(--text);
  cursor: pointer;
  text-align: left;
  transition:
    background-color 120ms ease,
    color 120ms ease,
    opacity 120ms ease;
  outline: none;
  &[data-state='paused'] {
    opacity: 0.8;
  }

  &:hover {
    background: var(--interactive-hover-bg);
  }

  // &:focus-visible {
  //   outline: 2px solid var(--focus-ring);
  //   outline-offset: 2px;
  // }
}

.app-titlebar__brand-icon {
  display: grid;
  width: 34px;
  height: 34px;
  flex: 0 0 auto;
  place-items: center;
  overflow: hidden;
  border-radius: 10px;
  background: var(--interactive-icon-bg);
  color: var(--accent-strong);

  img,
  :deep(svg) {
    width: 24px;
    height: 24px;
    object-fit: contain;
  }

  :deep(svg) {
    fill: currentColor;
  }
}

.app-titlebar__brand[data-state='paused'] .app-titlebar__brand-icon {
  color: var(--muted);
  background: var(--interactive-hover-bg);

  img,
  :deep(svg) {
    opacity: 0.42;
    filter: grayscale(1);
  }
}

.app-titlebar__brand-version {
  overflow: hidden;
  color: var(--muted);
  font-size: 13px;
  font-weight: 700;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.app-titlebar__drag-region {
  min-width: 0;
  flex: 1 1 auto;
  cursor: default;
}

.app-titlebar__window-actions {
  display: flex;
  align-items: stretch;
}

.app-titlebar__window-button {
  display: grid;
  width: 48px;
  height: 51px;
  place-items: center;
  border: 0;
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
    outline-offset: -2px;
  }

  :deep(svg) {
    width: 18px;
    height: 18px;
    fill: none;
    stroke: currentColor;
  }
}

.app-titlebar__window-button--close {
  &:hover {
    background: var(--danger);
    color: var(--text-inverse);
  }
}
</style>
