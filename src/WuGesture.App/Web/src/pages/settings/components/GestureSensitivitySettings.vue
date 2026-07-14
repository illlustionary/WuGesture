<script setup>
import CustomSelect from '@/components/CustomSelect.vue'
import SettingsField from './SettingsField.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'
import { GESTURE_SENSITIVITY_LEVELS } from '@/constants/gestureEditorOptions'

defineProps({
  draft: { type: Object, required: true }
})

const emit = defineEmits(['queue-persist', 'flush-persist'])

const levelNotes = {
  relaxed: '更容易识别，适合手势距离较短或移动较慢的情况。',
  standard: '默认平衡设置，兼顾识别成功率和误触控制。',
  strict: '需要更明确的移动，适合容易误触的情况。'
}

function commit() {
  emit('queue-persist')
  emit('flush-persist')
}
</script>

<template>
  <SettingsSectionCard
    title="手势灵敏度"
    description="选择手势识别的整体宽松程度。"
  >
    <SettingsField
      label="识别档位"
      :note="levelNotes[draft.gestureSensitivity.level]"
    >
      <CustomSelect
        v-model="draft.gestureSensitivity.level"
        :options="GESTURE_SENSITIVITY_LEVELS"
        placeholder="选择灵敏度"
        @change="commit"
      />
    </SettingsField>
  </SettingsSectionCard>
</template>
