<script setup>
import { computed } from 'vue'
import BaseDialog from './BaseDialog.vue'
import GestureRuleCommandFields from './gesture-rule-dialog/GestureRuleCommandFields.vue'
import GestureRuleGestureField from './gesture-rule-dialog/GestureRuleGestureField.vue'
import GestureRuleNameField from './gesture-rule-dialog/GestureRuleNameField.vue'

const props = defineProps({
  open: { type: Boolean, required: true },
  draft: { type: Object, required: true },
  message: { type: String, default: '' },
  isRecordingHotkey: { type: Function, default: null },
  isRecordingGesture: { type: Boolean, default: false },
  getGestureMnemonic: { type: Function, default: null },
  windowOperations: { type: Array, default: () => [] },
  volumeOperations: { type: Array, default: () => [] },
  brightnessOperations: { type: Array, default: () => [] }
})

defineEmits(['close', 'persist', 'record', 'record-hotkey'])

const patternLabel = computed(() => props.getGestureMnemonic?.(props.draft) || '尚未录制')
</script>

<template>
  <BaseDialog
    :open="open"
    title="手势"
    description="设置触发手势和执行命令。"
    title-id="gesture-dialog-title"
    :show-close="true"
    :show-actions="false"
    backdrop-class="gesture-dialog-backdrop"
    panel-class="gesture-dialog"
    @close="$emit('close')"
  >
    <div class="gesture-dialog__form">
      <GestureRuleNameField :draft="draft" />
      <GestureRuleGestureField
        :pattern-label="patternLabel"
        :is-empty="!getGestureMnemonic?.(draft)"
        :is-recording="isRecordingGesture"
        :message="message"
        @record="$emit('record')"
      />
      <GestureRuleCommandFields
        :draft="draft"
        :is-recording-hotkey="isRecordingHotkey?.(draft)"
        :window-operations="windowOperations"
        :volume-operations="volumeOperations"
        :brightness-operations="brightnessOperations"
        @record-hotkey="$emit('record-hotkey', draft)"
      />
    </div>
  </BaseDialog>
</template>

<style scoped lang="scss">
:deep(.gesture-dialog) {
  width: min(720px, calc(100vw - 36px));
}

.gesture-dialog__form {
  display: grid;
  gap: 18px;
}

:deep(.gesture-dialog-backdrop) {
  padding: 0;
  background: var(--scrim-subtle);
}
</style>
