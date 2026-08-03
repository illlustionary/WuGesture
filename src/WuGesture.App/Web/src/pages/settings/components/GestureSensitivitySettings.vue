<script setup>
import SettingsField from './SettingsField.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'
import BaseRange from '@/components/BaseRange.vue'
import { GESTURE_EDITOR_LIMITS } from '@/constants/gestureEditorLimits'

defineProps({
  draft: { type: Object, required: true }
})

const emit = defineEmits(['queue-persist', 'flush-persist'])
</script>

<template>
  <SettingsSectionCard title="手势灵敏度">
    <SettingsField
      label="灵敏度"
      :note="`${draft.gestureSensitivity.percent}%`"
    >
      <BaseRange
        v-model.number="draft.gestureSensitivity.percent"
        :min="GESTURE_EDITOR_LIMITS.gestureSensitivityPercent.min"
        :max="GESTURE_EDITOR_LIMITS.gestureSensitivityPercent.max"
        step="5"
        @input="emit('queue-persist')"
        @change="emit('flush-persist')"
      />
    </SettingsField>
  </SettingsSectionCard>
</template>
