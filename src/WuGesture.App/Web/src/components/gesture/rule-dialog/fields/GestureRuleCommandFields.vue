<script setup>
import CustomSelect from '@/components/form/CustomSelect.vue'
import GestureRuleBrightnessFields from './GestureRuleBrightnessFields.vue'
import GestureRuleField from './GestureRuleField.vue'
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
      <GestureRuleField
        class="gesture-rule-command-fields__type"
        label="命令类型"
      >
        <CustomSelect
          v-model="draft.actionType"
          :options="actionTypeOptions"
          placeholder="选择命令类型"
        />
      </GestureRuleField>

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
  gap: var(--md-sys-spacing-md);
  padding-top: var(--md-sys-spacing-lg);
  border-top: 1px solid color-mix(in srgb, var(--md-sys-color-outline-variant) 60%, transparent);
}

.gesture-rule-command-fields__title {
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-label-large-size);
  line-height: var(--md-sys-typescale-label-large-line-height);
  font-weight: var(--md-sys-typescale-label-large-weight);
}

.gesture-rule-command-fields__grid {
  display: grid;
  grid-template-columns: minmax(150px, 0.8fr) minmax(0, 1.6fr);
  gap: var(--md-sys-spacing-md);
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
