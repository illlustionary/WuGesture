<script setup>
import { computed } from 'vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'

const props = defineProps({
  app: { type: Object, required: true },
  removeLabel: { type: String, default: '移除' },
  missingPathText: { type: String, default: '未设置路径' }
})

const emit = defineEmits(['remove'])

const displayName = computed(() => props.app.displayName || props.app.name)
const pathText = computed(() => props.app.path || props.missingPathText)
const fallbackGlyph = computed(() => (displayName.value || '?').slice(0, 1).toUpperCase())
const missingPath = computed(() => !String(props.app.path ?? '').trim())
</script>

<template>
  <div class="app-list__item">
    <span
      class="app-list__icon"
      :class="{ 'app-list__icon--missing': missingPath }"
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
      <span class="app-list__path">{{ pathText }}</span>
    </span>
    <span class="app-list__status">
      <IconActionButton
        icon="delete"
        :label="removeLabel"
        class="app-list__delete"
        @click.stop="emit('remove', app)"
      />
    </span>
  </div>
</template>

<style scoped lang="scss">
.app-list__item {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto;
  gap: 12px;
  min-height: 64px;
  padding: 12px 14px;
  align-items: center;
  border-radius: 0;
  background: var(--interactive-bg);
  transition:
    background-color 120ms ease,
    border-color 120ms ease;
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
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
  color: var(--text-subtle);
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
  .app-list__item {
    grid-template-columns: 1fr;
  }

  .app-list__status {
    margin-left: auto;
    justify-content: flex-end;
  }
}
</style>
