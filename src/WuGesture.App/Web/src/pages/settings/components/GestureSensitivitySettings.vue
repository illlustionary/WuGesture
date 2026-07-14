<script setup>
import SettingsField from './SettingsField.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'
import { GESTURE_EDITOR_LIMITS } from '@/constants/gestureEditorLimits'
import { getRangeProgress } from '@/utils/rangeProgress'

defineProps({
  draft: { type: Object, required: true }
})

const emit = defineEmits(['queue-persist', 'flush-persist'])
</script>

<template>
  <SettingsSectionCard
    title="手势灵敏度"
    description="数值越高，越容易识别短距离和快速手势，但误触概率也会增加。"
  >
    <SettingsField
      label="灵敏度"
      :note="`${draft.gestureSensitivity.percent}%`"
    >
      <input
        v-model.number="draft.gestureSensitivity.percent"
        type="range"
        :min="GESTURE_EDITOR_LIMITS.gestureSensitivityPercent.min"
        :max="GESTURE_EDITOR_LIMITS.gestureSensitivityPercent.max"
        :style="{
          '--range-progress': getRangeProgress(
            draft.gestureSensitivity.percent,
            GESTURE_EDITOR_LIMITS.gestureSensitivityPercent.min,
            GESTURE_EDITOR_LIMITS.gestureSensitivityPercent.max
          )
        }"
        step="5"
        @input="emit('queue-persist')"
        @change="emit('flush-persist')"
      />
    </SettingsField>
  </SettingsSectionCard>
</template>
