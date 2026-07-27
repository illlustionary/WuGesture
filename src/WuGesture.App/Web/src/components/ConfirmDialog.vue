<script setup>
import BaseDialog from './BaseDialog.vue'

defineProps({
  open: { type: Boolean, required: true },
  title: { type: String, required: true },
  message: { type: String, default: '' },
  confirmText: { type: String, default: '确认' },
  cancelText: { type: String, default: '取消' },
  tone: {
    type: String,
    default: 'danger',
    validator: value => ['danger', 'primary'].includes(value)
  }
})

const emit = defineEmits(['close', 'confirm'])

function confirm() {
  emit('confirm')
}
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
    @confirm="confirm"
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
  color: var(--muted);
  font-size: 13px;
  line-height: 1.55;
}
</style>
