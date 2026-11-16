<script setup lang="ts">
import { computed, useSlots } from 'vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'

interface ApplicationItem {
  name?: string
  displayName?: string
  path?: string
  pathMissing?: boolean
  icon?: string
}

const props = withDefaults(
  defineProps<{
    app: ApplicationItem
    removable?: boolean
    removeLabel?: string
    missingPathText?: string
  }>(),
  {
    removable: false,
    removeLabel: '移除',
    missingPathText: '未设置路径'
  }
)

const emit = defineEmits<{
  remove: [app: ApplicationItem]
  'open-path': [path: string]
}>()

const slots = useSlots()

const displayName = computed(() => props.app.displayName || props.app.name || '')
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
        @click.stop="emit('open-path', app.path ?? '')"
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
@use '../../styles/tokens/state' as *;

.app-list__item {
  @include state-layer(var(--md-sys-color-on-surface));

  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  gap: var(--md-sys-spacing-md);
  min-height: var(--md-sys-size-row-single);
  padding: var(--md-sys-spacing-sm) var(--md-sys-spacing-lg);
  align-items: center;
  background: transparent;

  &--with-actions {
    grid-template-columns: auto minmax(0, 1fr) auto;
  }
}

.app-list__icon {
  position: relative;
  z-index: 1;
  display: grid;
  place-items: center;
  width: 32px;
  height: 32px;
  overflow: hidden;
  border-radius: var(--md-sys-shape-corner-small);
  background: var(--md-sys-color-surface-container-high);
  color: var(--md-sys-color-primary);

  &--missing::after {
    content: '!';
    position: absolute;
    top: -4px;
    right: -4px;
    display: grid;
    place-items: center;
    width: 14px;
    height: 14px;
    border: 1px solid var(--md-sys-color-surface-container-lowest);
    border-radius: var(--md-sys-shape-corner-full);
    background: var(--md-sys-color-warning-container);
    color: var(--md-sys-color-on-warning-container);
    font-size: 10px;
    font-weight: 600;
  }
}

.app-list__fallback {
  font-size: var(--md-sys-typescale-title-small-size);
  font-weight: var(--md-sys-typescale-title-small-weight);
}

.app-list__content {
  position: relative;
  z-index: 1;
  display: grid;
  min-width: 0;
  gap: 2px;
}

.app-list__name {
  min-width: 0;
  overflow: hidden;
  color: var(--md-sys-color-on-surface);
  font-size: var(--md-sys-typescale-body-medium-size);
  line-height: var(--md-sys-typescale-body-medium-line-height);
  font-weight: 500;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.app-list__path {
  min-width: 0;
  overflow: hidden;
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--md-sys-color-primary);
  font: inherit;
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
  text-align: left;
  text-overflow: ellipsis;
  white-space: nowrap;

  &:where(button) {
    cursor: pointer;
  }

  &:where(button):focus-visible {
    outline: 2px solid var(--md-sys-color-primary);
    outline-offset: 2px;
  }
}

.app-list__path--missing {
  color: var(--md-sys-color-on-surface-variant);
}

.app-list__missing-state {
  color: var(--md-sys-color-warning);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
}

.app-list__status {
  position: relative;
  z-index: 1;
  display: inline-flex;
  flex: 0 0 auto;
  align-items: center;
  gap: var(--md-sys-spacing-sm);
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
