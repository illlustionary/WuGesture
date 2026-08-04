<script setup>
import CustomSelect from '@/components/form/CustomSelect.vue'
import { GESTURE_EDITOR_LIMITS } from '@/constants/gestureEditorLimits'
import GestureRuleField from './GestureRuleField.vue'

defineProps({
  draft: { type: Object, required: true },
  operations: { type: Array, default: () => [] }
})
</script>

<template>
  <div class="gesture-rule-brightness-fields">
    <GestureRuleField label="执行操作">
      <CustomSelect
        v-model="draft.brightnessOperation"
        :options="operations"
        placeholder="选择亮度操作"
      />
    </GestureRuleField>
    <GestureRuleField label="数值">
      <input
        v-model.number="draft.amount"
        class="scope-input"
        type="number"
        :min="GESTURE_EDITOR_LIMITS.amount.min"
        :max="GESTURE_EDITOR_LIMITS.amount.max"
      />
    </GestureRuleField>
  </div>
</template>

<style scoped lang="scss">
.gesture-rule-brightness-fields {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(104px, 0.42fr);
  gap: 10px;
}

@media (max-width: 720px) {
  .gesture-rule-brightness-fields {
    grid-template-columns: 1fr;
  }
}
</style>
