<script setup>
import CustomSelect from '@/components/form/CustomSelect.vue'
import { GESTURE_EDITOR_LIMITS } from '@/constants/gestureEditorLimits'

defineProps({
  draft: { type: Object, required: true },
  operations: { type: Array, default: () => [] }
})
</script>

<template>
  <div class="gesture-rule-brightness-fields">
    <label class="gesture-rule-command-field">
      <span>执行操作</span>
      <CustomSelect
        v-model="draft.brightnessOperation"
        :options="operations"
        placeholder="选择亮度操作"
      />
    </label>
    <label class="gesture-rule-command-field gesture-rule-command-field--amount">
      <span>数值</span>
      <input
        v-model.number="draft.amount"
        class="scope-input"
        type="number"
        :min="GESTURE_EDITOR_LIMITS.amount.min"
        :max="GESTURE_EDITOR_LIMITS.amount.max"
      />
    </label>
  </div>
</template>

<style scoped lang="scss">
.gesture-rule-brightness-fields {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(104px, 0.42fr);
  gap: 10px;
}

.gesture-rule-command-field {
  display: grid;
  min-width: 0;
  gap: 6px;
  color: var(--muted);
  font-size: 13px;
}

@media (max-width: 720px) {
  .gesture-rule-brightness-fields {
    grid-template-columns: 1fr;
  }
}
</style>
