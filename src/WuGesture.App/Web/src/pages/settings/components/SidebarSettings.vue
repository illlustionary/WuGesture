<script setup>
import BaseRange from '@/components/form/BaseRange.vue'
import { GESTURE_EDITOR_LIMITS } from '@/constants/gestureEditorLimits'
import SettingsField from './SettingsField.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'

defineProps({
  draft: { type: Object, required: true }
})

const emit = defineEmits(['queue-persist', 'flush-persist'])
</script>

<template>
  <SettingsSectionCard
    title="侧栏"
    note="侧栏收起后，鼠标移动到窗口左边缘该距离内会自动展开。距离越大越容易触发，但也会占用更多页面左侧的响应区域。"
  >
    <SettingsField
      label="唤醒距离"
      :note="`${draft.sidebar.revealDistance}px`"
    >
      <BaseRange
        v-model.number="draft.sidebar.revealDistance"
        :min="GESTURE_EDITOR_LIMITS.sidebarRevealDistance.min"
        :max="GESTURE_EDITOR_LIMITS.sidebarRevealDistance.max"
        step="2"
        @input="emit('queue-persist')"
        @change="emit('flush-persist')"
      />
    </SettingsField>
  </SettingsSectionCard>
</template>
