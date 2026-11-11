<script setup lang="ts">
import type { ButtonSize, ButtonVariant } from '@/types/ui'

withDefaults(
  defineProps<{
    variant?: ButtonVariant
    size?: ButtonSize
    type?: 'button' | 'submit' | 'reset'
    disabled?: boolean
  }>(),
  {
    variant: 'filled',
    size: 'md',
    type: 'button',
    disabled: false
  }
)
</script>

<template>
  <button
    :type="type"
    class="app-button"
    :class="[`app-button--${variant}`, `app-button--${size}`]"
    :disabled="disabled"
  >
    <span
      v-if="$slots.leading"
      class="app-button__icon"
    >
      <slot name="leading" />
    </span>
    <span class="app-button__label"><slot /></span>
  </button>
</template>

<style scoped lang="scss">
@use '../../styles/tokens/state' as *;

.app-button {
  @include state-layer(currentColor);

  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-width: 64px;
  padding: 0 24px;
  overflow: hidden;
  border: 1px solid transparent;
  border-radius: var(--md-sys-shape-corner-full);
  background: transparent;
  color: var(--md-sys-color-primary);
  font-family: inherit;
  font-size: var(--md-sys-typescale-label-large-size);
  line-height: var(--md-sys-typescale-label-large-line-height);
  font-weight: var(--md-sys-typescale-label-large-weight);
  white-space: nowrap;
  cursor: pointer;
  transition:
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    box-shadow var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:disabled {
    cursor: not-allowed;
    opacity: var(--md-sys-state-disabled-content-opacity);
  }
}

.app-button--sm {
  min-width: 48px;
  height: 32px;
  padding: 0 16px;
  font-size: var(--md-sys-typescale-label-medium-size);
}

.app-button--md {
  height: 40px;
}

.app-button--filled {
  background: var(--md-sys-color-primary);
  color: var(--md-sys-color-on-primary);
}

.app-button--tonal {
  background: var(--md-sys-color-secondary-container);
  color: var(--md-sys-color-on-secondary-container);
}

.app-button--outlined {
  border-color: var(--md-sys-color-outline);
  color: var(--md-sys-color-primary);
}

.app-button--text {
  min-width: 48px;
  padding: 0 12px;
}

.app-button--danger {
  background: var(--md-sys-color-error);
  color: var(--md-sys-color-on-error);
}

.app-button__icon {
  display: inline-flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;

  :deep(svg) {
    width: 100%;
    height: 100%;
  }
}

.app-button__label {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>
