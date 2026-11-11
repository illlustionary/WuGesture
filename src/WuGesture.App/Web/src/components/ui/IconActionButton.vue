<script setup lang="ts">
import AppIcon from '@/components/ui/AppIcon.vue'
import HoverBubble from '@/components/ui/HoverBubble.vue'

defineOptions({
  inheritAttrs: false
})

withDefaults(
  defineProps<{
    icon: string
    label: string
    tone?: string
    color?: string
    showTooltip?: boolean
  }>(),
  {
    tone: 'neutral',
    color: '',
    showTooltip: true
  }
)
</script>

<template>
  <HoverBubble
    :text="label"
    :disabled="!showTooltip"
  >
    <button
      v-bind="$attrs"
      type="button"
      class="icon-action-button"
      :aria-label="label"
      :class="[icon]"
      :data-tone="tone"
      :style="{ color }"
    >
      <AppIcon
        :name="icon"
        class="icon-action-button__icon"
        aria-hidden="true"
      />
    </button>
  </HoverBubble>
</template>

<style scoped lang="scss">
@use '../../styles/tokens/state' as *;

.icon-action-button {
  @include state-layer(currentColor);

  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  min-width: 36px;
  min-height: 36px;
  padding: 0;
  overflow: hidden;
  border: 0;
  border-radius: var(--md-sys-shape-corner-full);
  background: transparent;
  color: var(--md-sys-color-on-surface-variant);
  line-height: 0;
  cursor: pointer;
  transition:
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:disabled {
    cursor: not-allowed;
    opacity: var(--md-sys-state-disabled-content-opacity);
  }

  /* 图标名兜底色，放在变体之前，变体样式可覆盖。 */
  &.add {
    color: var(--md-sys-color-primary);
  }

  &.delete {
    color: var(--md-sys-color-error);
  }

  &.close {
    color: var(--md-sys-color-on-surface);
  }

  &[data-tone='danger'] {
    color: var(--md-sys-color-error);
  }

  &[data-tone='muted'] {
    color: var(--md-sys-color-on-surface-variant);
  }

  &.primary-button {
    background: var(--md-sys-color-primary);
    color: var(--md-sys-color-on-primary);
  }

  &.secondary-button {
    background: var(--md-sys-color-surface-container-highest);
    color: var(--md-sys-color-on-surface);
  }

  &.ghost-button {
    color: var(--md-sys-color-on-surface-variant);
  }

  &.icon-button {
    color: var(--md-sys-color-on-surface-variant);
  }

  &.icon-button--danger {
    color: var(--md-sys-color-error);
  }
}

.icon-action-button__icon {
  display: inline-block;
  width: 20px;
  height: 20px;
  flex: 0 0 auto;
  color: inherit;

  :deep(svg) {
    display: block;
    width: 100%;
    height: 100%;
    fill: currentColor;
  }
}
</style>
