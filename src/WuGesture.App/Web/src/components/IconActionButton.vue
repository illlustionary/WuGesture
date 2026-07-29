<script setup>
import { computed } from 'vue'
import addIcon from '@/assets/actions/add.svg'
import closeIcon from '@/assets/window/close.svg'
import cloudDownloadIcon from '@/assets/actions/cloud-download.svg'
import cloudUploadIcon from '@/assets/actions/cloud-upload.svg'
import deleteIcon from '@/assets/actions/delete.svg'
import downloadIcon from '@/assets/actions/download.svg'
import resetIcon from '@/assets/actions/reset.svg'
import testIcon from '@/assets/actions/test.svg'
import uploadIcon from '@/assets/actions/upload.svg'

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
  nativeTooltip: {
    type: Boolean,
    default: true
  }
})

const iconMap = {
  add: addIcon,
  close: closeIcon,
  'cloud-download': cloudDownloadIcon,
  'cloud-upload': cloudUploadIcon,
  delete: deleteIcon,
  download: downloadIcon,
  reset: resetIcon,
  test: testIcon,
  upload: uploadIcon
}

const iconComponent = computed(() => iconMap[props.icon] ?? addIcon)
</script>

<template>
  <button
    type="button"
    class="icon-action-button"
    :aria-label="label"
    :title="nativeTooltip ? label : undefined"
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
