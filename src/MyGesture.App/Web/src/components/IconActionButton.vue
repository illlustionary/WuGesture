<script setup>
import { computed } from 'vue'
import addIcon from '../assets/add.svg'
import closeIcon from '../assets/close.svg'
import deleteIcon from '../assets/delete.svg'

const props = defineProps({
  icon: {
    type: String,
    required: true
  },
  label: {
    type: String,
    required: true
  }
})

const iconMap = {
  add: addIcon,
  close: closeIcon,
  delete: deleteIcon
}

const iconComponent = computed(() => iconMap[props.icon] ?? addIcon)
</script>

<template>
  <button
    type="button"
    class="icon-action-button"
    :aria-label="label"
    :title="label"
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
  line-height: 0;
}

.icon-action-button__icon {
  display: inline-block;
  width: 16px;
  height: 16px;
  flex: 0 0 auto;

  :deep(svg) {
    width: 100%;
    height: 100%;
    display: block;
    fill: currentColor;
  }
}
</style>
