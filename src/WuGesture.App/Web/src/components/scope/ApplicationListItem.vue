<script setup>
import { computed, useSlots } from 'vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'

const props = defineProps({
  app: { type: Object, required: true },
  removable: { type: Boolean, default: false },
  removeLabel: { type: String, default: '移除' },
  missingPathText: { type: String, default: '未设置路径' }
})

const emit = defineEmits(['remove', 'open-path'])
const slots = useSlots()

const displayName = computed(() => props.app.displayName || props.app.name)
const pathText = computed(() => props.app.path || props.missingPathText)
const fallbackGlyph = computed(() => (displayName.value || '?').slice(0, 1).toUpperCase())
const missingPath = computed(() => !String(props.app.path ?? '').trim())
const pathMissing = computed(() => Boolean(props.app.pathMissing))
const hasActions = computed(() => props.removable || Boolean(slots.actions))
</script>

<template>
  <div
    class="app-list__item"
    :class="{ 'app-list__item--with-actions': hasActions }"
  >
    <span
      class="app-list__icon"
      :class="{ 'app-list__icon--missing': missingPath || pathMissing }"
      aria-hidden="true"
    >
      <img
        v-if="app.icon"
        class="app-icon app-icon--small"
        :src="app.icon"
        alt=""
      />
      <span
        v-else
        class="app-list__fallback"
      >
        {{ fallbackGlyph }}
      </span>
    </span>
    <span class="app-list__content">
      <span class="app-list__name">{{ displayName }}</span>
      <button
        v-if="!missingPath"
        type="button"
        class="app-list__path"
        :title="app.path"
        @click.stop="emit('open-path', app.path)"
      >
        {{ pathText }}
      </button>
      <span
        v-else
        class="app-list__path app-list__path--missing"
      >
        {{ pathText }}
      </span>
      <span
        v-if="pathMissing"
        class="app-list__missing-state"
      >
        程序不存在
      </span>
    </span>
    <span
      v-if="hasActions"
      class="app-list__status"
    >
      <slot name="actions">
        <IconActionButton
          v-if="removable"
          icon="delete"
          :label="removeLabel"
          class="app-list__delete"
          @click.stop="emit('remove', app)"
        />
      </slot>
    </span>
  </div>
</template>

<style scoped lang="scss">
.app-list__item {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  gap: 12px;
  min-height: 64px;
  padding: 12px 14px;
  align-items: center;
  border-radius: 0;
  background: var(--interactive-bg);
  transition:
    background-color 120ms ease,
    border-color 120ms ease;

  &--with-actions {
    grid-template-columns: auto minmax(0, 1fr) auto;
  }
}

.app-list__item:hover {
  background: var(--interactive-hover-bg);
}

.app-list__icon {
  position: relative;
  display: grid;
  place-items: center;
  width: 32px;
  height: 32px;
  border-radius: 12px;
  background: var(--interactive-icon-bg);
  color: var(--accent-strong);
  overflow: hidden;

  &--missing::after {
    content: '!';
    position: absolute;
    top: -4px;
    right: -4px;
    display: grid;
    place-items: center;
    width: 14px;
    height: 14px;
    border-radius: 999px;
    background: var(--warning-bg);
    color: var(--warning-text);
    font-size: 10px;
    font-weight: 800;
    border: 1px solid var(--border-inverse-strong);
    box-shadow: var(--shadow-inline);
  }
}

.app-list__fallback {
  font-size: 14px;
  font-weight: 700;
}

.app-list__content {
  display: grid;
  min-width: 0;
  gap: 4px;
}

.app-list__name {
  min-width: 0;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
  font-weight: 600;
}

.app-list__path {
  min-width: 0;
  overflow: hidden;
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--accent-strong);
  font: inherit;
  font-size: 12px;
  line-height: inherit;
  text-align: left;
  text-decoration: underline;
  text-overflow: ellipsis;
  white-space: nowrap;

  &:where(button) {
    cursor: pointer;
  }

  &:where(button):hover {
    color: var(--accent);
  }

  &:where(button):focus-visible {
    outline: 2px solid var(--accent-strong);
    outline-offset: 2px;
  }
}

.app-list__path--missing {
  color: var(--text-subtle);
  text-decoration: none;
}

.app-list__missing-state {
  color: var(--warning-text);
  font-size: 12px;
}

.app-list__status {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  flex: 0 0 auto;
}

:deep(.app-list__delete) {
  opacity: 0;
  transform: scale(0.92);
  transition:
    opacity 120ms ease,
    transform 120ms ease;
}

.app-list__item:hover :deep(.app-list__delete),
.app-list__item:focus-within :deep(.app-list__delete) {
  opacity: 1;
  transform: scale(1);
}

@media (max-width: 720px) {
  .app-list__item,
  .app-list__item--with-actions {
    grid-template-columns: 1fr;
  }

  .app-list__status {
    margin-left: auto;
    justify-content: flex-end;
  }
}
</style>
