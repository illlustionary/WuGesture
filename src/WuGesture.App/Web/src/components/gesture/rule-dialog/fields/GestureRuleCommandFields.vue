<script setup>
import CustomSelect from '@/components/form/CustomSelect.vue'
import GestureRuleBrightnessFields from './GestureRuleBrightnessFields.vue'
import GestureRuleHotkeyFields from './GestureRuleHotkeyFields.vue'
import GestureRuleVolumeFields from './GestureRuleVolumeFields.vue'
import GestureRuleWindowFields from './GestureRuleWindowFields.vue'
import { ACTION_TYPE_OPTIONS, ACTION_TYPES } from '@/constants/gestureEditorOptions'

defineProps({
  draft: { type: Object, required: true },
  isRecordingHotkey: { type: Boolean, default: false },
  windowOperations: { type: Array, default: () => [] },
  volumeOperations: { type: Array, default: () => [] },
  brightnessOperations: { type: Array, default: () => [] }
})

defineEmits(['record-hotkey'])

const actionTypeOptions = ACTION_TYPE_OPTIONS
</script>

<template>
  <section
    class="gesture-rule-command-fields"
    aria-labelledby="gesture-dialog-command-label"
  >
    <span
      id="gesture-dialog-command-label"
      class="gesture-rule-command-fields__title"
      >执行命令</span
    >
    <div class="gesture-rule-command-fields__grid">
      <label class="gesture-rule-command-fields__type">
        <span>命令类型</span>
        <CustomSelect
          v-model="draft.actionType"
          :options="actionTypeOptions"
          placeholder="选择命令类型"
        />
      </label>

      <div class="gesture-rule-command-fields__detail">
        <GestureRuleWindowFields
          v-if="draft.actionType === ACTION_TYPES.window"
          :draft="draft"
          :operations="windowOperations"
        />
        <GestureRuleVolumeFields
          v-else-if="draft.actionType === ACTION_TYPES.volume"
          :draft="draft"
          :operations="volumeOperations"
        />
        <GestureRuleBrightnessFields
          v-else-if="draft.actionType === ACTION_TYPES.brightness"
          :draft="draft"
          :operations="brightnessOperations"
        />
        <GestureRuleHotkeyFields
          v-else-if="draft.actionType === ACTION_TYPES.hotkey"
          :draft="draft"
          :is-recording="isRecordingHotkey"
          @record="$emit('record-hotkey')"
        />
      </div>
    </div>
  </section>
</template>

<style scoped lang="scss">
.gesture-rule-command-fields {
  display: grid;
  gap: 12px;
  padding-top: 18px;
  border-top: 1px solid var(--border-muted);
}

.gesture-rule-command-fields__title {
  color: var(--muted);
  font-size: 13px;
  font-weight: 600;
}

.gesture-rule-command-fields__grid {
  display: grid;
  grid-template-columns: minmax(150px, 0.8fr) minmax(0, 1.6fr);
  gap: 12px;
}

.gesture-rule-command-fields__type {
  display: grid;
  min-width: 0;
  gap: 6px;
  color: var(--muted);
  font-size: 13px;
}

.gesture-rule-command-fields__detail {
  min-width: 0;
}

@media (max-width: 720px) {
  .gesture-rule-command-fields__grid {
    grid-template-columns: 1fr;
  }
}
</style>
