<script setup>
import { computed } from 'vue'
import addIcon from '../assets/add.svg'
import briefcaseIcon from '../assets/briefcase.svg'
import browserIcon from '../assets/browser.svg'
import circleDashedIcon from '../assets/circle-dashed.svg'
import closeIcon from '../assets/close.svg'
import closeAltIcon from '../assets/close-alt.svg'
import codeIcon from '../assets/code.svg'
import cloudDownloadIcon from '../assets/cloud-download.svg'
import cloudUploadIcon from '../assets/cloud-upload.svg'
import crosshairIcon from '../assets/crosshair.svg'
import deleteIcon from '../assets/delete.svg'
import downloadIcon from '../assets/download.svg'
import folderIcon from '../assets/folder.svg'
import keyboardIcon from '../assets/keyboard.svg'
import mediaIcon from '../assets/media.svg'
import mouseIcon from '../assets/mouse.svg'
import recordIcon from '../assets/record.svg'
import resetIcon from '../assets/reset.svg'
import settingIcon from '../assets/setting.svg'
import sparkleIcon from '../assets/sparkle.svg'
import testIcon from '../assets/test.svg'
import uploadIcon from '../assets/upload.svg'

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
  briefcase: briefcaseIcon,
  browser: browserIcon,
  'circle-dashed': circleDashedIcon,
  close: closeIcon,
  'close-alt': closeAltIcon,
  code: codeIcon,
  'cloud-download': cloudDownloadIcon,
  'cloud-upload': cloudUploadIcon,
  crosshair: crosshairIcon,
  delete: deleteIcon,
  download: downloadIcon,
  folder: folderIcon,
  keyboard: keyboardIcon,
  media: mediaIcon,
  mouse: mouseIcon,
  record: recordIcon,
  reset: resetIcon,
  setting: settingIcon,
  sparkle: sparkleIcon,
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
