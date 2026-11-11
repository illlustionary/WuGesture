<script setup lang="ts">
withDefaults(
  defineProps<{
    selected?: boolean
    disabled?: boolean
    removable?: boolean
  }>(),
  {
    selected: false,
    disabled: false,
    removable: false
  }
)

const emit = defineEmits<{
  remove: []
}>()
</script>

<template>
  <span
    class="app-chip"
    :class="{ 'is-selected': selected, 'is-disabled': disabled }"
  >
    <span class="app-chip__label"><slot /></span>
    <button
      v-if="removable"
      type="button"
      class="app-chip__remove"
      aria-label="移除"
      :disabled="disabled"
      @click.stop="emit('remove')"
    >
      <svg
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <path
          fill="currentColor"
          d="M19 6.41 17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"
        />
      </svg>
    </button>
  </span>
</template>

<style scoped lang="scss">
@use '../../styles/tokens/state' as *;

.app-chip {
  @include state-layer(currentColor);

  display: inline-flex;
  align-items: center;
  gap: 6px;
  max-width: 100%;
  height: var(--md-sys-size-chip);
  padding: 0 12px;
  overflow: hidden;
  border: 1px solid var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-small);
  background: transparent;
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-label-medium-size);
  line-height: var(--md-sys-typescale-label-medium-line-height);
  font-weight: var(--md-sys-typescale-label-medium-weight);

  &.is-selected {
    border-color: transparent;
    background: var(--md-sys-color-secondary-container);
    color: var(--md-sys-color-on-secondary-container);
  }

  &.is-disabled {
    cursor: not-allowed;
    opacity: var(--md-sys-state-disabled-content-opacity);
  }
}

.app-chip__label {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.app-chip__remove {
  display: inline-flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  margin-right: -4px;
  padding: 0;
  border: 0;
  border-radius: var(--md-sys-shape-corner-full);
  background: transparent;
  color: inherit;
  cursor: pointer;

  &:hover {
    background: color-mix(in srgb, currentColor 12%, transparent);
  }

  svg {
    width: 14px;
    height: 14px;
  }
}
</style>
