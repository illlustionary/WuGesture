<script setup lang="ts">
import BaseDialog from './BaseDialog.vue'

withDefaults(
  defineProps<{
    open: boolean
    title: string
    message?: string
    confirmText?: string
    cancelText?: string
    tone?: 'danger' | 'primary'
  }>(),
  {
    message: '',
    confirmText: '确认',
    cancelText: '取消',
    tone: 'danger'
  }
)

const emit = defineEmits<{
  close: []
  confirm: []
}>()
</script>

<template>
  <BaseDialog
    :open="open"
    :title="title"
    title-id="confirm-dialog-title"
    :show-close="true"
    :cancel-text="cancelText"
    :confirm-text="confirmText"
    :confirm-tone="tone"
    panel-class="confirm-dialog"
    @close="emit('close')"
    @confirm="emit('confirm')"
  >
    <div
      v-if="message || $slots.default"
      class="confirm-dialog__body"
    >
      <slot>{{ message }}</slot>
    </div>
  </BaseDialog>
</template>

<style scoped lang="scss">
:deep(.confirm-dialog) {
  width: min(420px, calc(100vw - 36px));
}

.confirm-dialog__body {
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-medium-size);
  line-height: 1.55;
}
</style>
