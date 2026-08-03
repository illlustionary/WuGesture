<script setup>
import CustomSelect from '@/components/CustomSelect.vue'
import BaseDialog from '@/components/BaseDialog.vue'
import ToggleCheckbox from '@/components/ToggleCheckbox.vue'
import EdgeActionCommandFields from './EdgeActionCommandFields.vue'
import { ACTION_TYPE_OPTIONS, EDGE_TRIGGER_TYPES } from '@/constants/gestureEditorOptions'
import { GESTURE_EDITOR_LIMITS } from '@/constants/gestureEditorLimits'

defineProps({
  action: { type: Object, required: true },
  draft: { type: Object, required: true },
  isRecordingHotkey: { type: Function, required: true },
  title: { type: String, required: true },
  operationOptions: { type: Function, required: true },
  operationModel: { type: Function, required: true }
})

const emit = defineEmits(['close', 'record-hotkey', 'update-operation'])

const actionTypeOptions = ACTION_TYPE_OPTIONS
</script>

<template>
  <BaseDialog
    :open="true"
    :title="title"
    description="关闭弹窗后自动保存。"
    title-id="edge-dialog-title"
    :show-close="true"
    :show-actions="false"
    panel-class="edge-dialog"
    @close="emit('close')"
  >
    <div class="edge-dialog__grid">
      <div class="edge-field edge-dialog__enabled">
        <span>是否启用</span>
        <ToggleCheckbox
          v-model="draft.enabled"
          label="启用"
        />
      </div>

      <label
        v-if="draft.triggerType === EDGE_TRIGGER_TYPES.friction"
        class="edge-field"
      >
        <span>摩擦次数</span>
        <input
          v-model.number="draft.frictionCount"
          class="scope-input"
          type="number"
          :min="GESTURE_EDITOR_LIMITS.frictionCount.min"
          :max="GESTURE_EDITOR_LIMITS.frictionCount.max"
        />
      </label>

      <label class="edge-field">
        <span>命令类型</span>
        <CustomSelect
          v-model="draft.actionType"
          :options="actionTypeOptions"
          placeholder="选择命令类型"
        />
      </label>
    </div>

    <EdgeActionCommandFields
      :draft="draft"
      :is-recording-hotkey="isRecordingHotkey"
      :operation-options="operationOptions"
      :operation-model="operationModel"
      @record-hotkey="emit('record-hotkey')"
      @update-operation="emit('update-operation', $event)"
    />
  </BaseDialog>
</template>

<style scoped lang="scss">
.edge-dialog__grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

:deep(.edge-dialog) {
  width: min(620px, calc(100vw - 36px));
}

.edge-field {
  display: grid;
  gap: 6px;
  color: var(--muted);
  font-size: 13px;
}

.edge-dialog__enabled {
  min-height: 42px;
}

@media (max-width: 920px) {
  .edge-dialog__grid {
    grid-template-columns: 1fr;
  }
}
</style>
