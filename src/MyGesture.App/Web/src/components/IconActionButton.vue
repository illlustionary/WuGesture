<script setup>
import { computed } from 'vue'
import addIcon from '../assets/add.svg'
import closeIcon from '../assets/close.svg'
import deleteIcon from '../assets/delete.svg'
import settingIcon from '../assets/setting.svg'
import resetIcon from '../assets/reset.svg'

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
  }
})

const iconMap = {
  add: addIcon,
  close: closeIcon,
  delete: deleteIcon,
  setting: settingIcon,
  reset: resetIcon
}

const iconComponent = computed(() => iconMap[props.icon] ?? addIcon)
</script>

<template>
  <button
    type="button"
    class="icon-action-button"
    :aria-label="label"
    :title="label"
    :class="[icon]"
    :data-tone="tone"
    :style="{ color }"
  >
    <component
      :is="iconComponent"
      class="icon-action-button__icon"
      aria-hidden="true"
    />
  </button>
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
    color: var(--accent-strong);
  }

  &.add {
    color: var(--accent-strong);
  }
  &.delete {
    color: var(--danger);
  }
  &.close {
    color: black;
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
