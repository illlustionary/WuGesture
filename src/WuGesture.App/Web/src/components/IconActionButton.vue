<script setup>
import AppIcon from '@/components/AppIcon.vue'
import HoverBubble from '@/components/HoverBubble.vue'

defineOptions({
  inheritAttrs: false
})

const props = defineProps({
  icon: {
    type: String,
    required: true
  },
  label: {
    type: String,
    required: true
  },
  tone: {
    type: String,
    default: 'neutral'
  },
  color: {
    type: String,
    default: ''
  },
  showTooltip: {
    type: Boolean,
    default: true
  }
})
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
.icon-action-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0;
  min-width: 34px;
  min-height: 34px;
  padding: 0;
  line-height: 0;
  color: currentColor;
  border: none;
  background: transparent;
  box-shadow: none;
  outline: none;
  cursor: pointer;

  &:hover {
    background: transparent;
    box-shadow: none;
  }

  &.primary-button {
    min-width: 34px;
    min-height: 34px;
  }

  &.ghost-button,
  &.secondary-button {
    color: var(--muted);
  }

  &.add {
    color: var(--accent-strong);
  }
  &.delete {
    color: var(--danger);
  }
  &.close {
    color: var(--text);
  }
}

.icon-action-button__icon {
  display: inline-block;
  width: 20px;
  height: 20px;
  flex: 0 0 auto;
  color: inherit;

  :deep(svg) {
    width: 100%;
    height: 100%;
    display: block;
    fill: currentColor;
  }
}
</style>
