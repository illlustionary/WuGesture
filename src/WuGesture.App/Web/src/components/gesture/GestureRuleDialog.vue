<script setup>
import { computed } from 'vue'
import BaseDialog from '@/components/dialog/BaseDialog.vue'
import GestureRuleCommandFields from './rule-dialog/fields/GestureRuleCommandFields.vue'
import GestureRuleGestureField from './rule-dialog/fields/GestureRuleGestureField.vue'
import GestureRuleNameField from './rule-dialog/fields/GestureRuleNameField.vue'
import GestureRuleProgramFields from './rule-dialog/fields/GestureRuleProgramFields.vue'
import { ACTION_TYPES } from '@/constants/gestureEditorOptions'

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

defineEmits(['close', 'persist', 'record', 'record-hotkey', 'select-program', 'open-application-folder'])

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
      <GestureRuleProgramFields
        v-if="draft.actionType === ACTION_TYPES.program"
        :draft="draft"
        :show-picker="false"
        :show-arguments="false"
        @open-application-folder="$emit('open-application-folder', $event)"
      />
      <GestureRuleCommandFields
        :draft="draft"
        :is-recording-hotkey="isRecordingHotkey?.(draft)"
        :window-operations="windowOperations"
        :volume-operations="volumeOperations"
        :brightness-operations="brightnessOperations"
        @record-hotkey="$emit('record-hotkey', draft)"
      />
      <GestureRuleProgramFields
        v-if="draft.actionType === ACTION_TYPES.program"
        :draft="draft"
        :show-program="false"
        :show-picker="true"
        :show-arguments="false"
        @select-program="$emit('select-program')"
      />
      <GestureRuleProgramFields
        v-if="draft.actionType === ACTION_TYPES.program"
        :draft="draft"
        :show-program="false"
        :show-picker="false"
        :show-arguments="true"
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
