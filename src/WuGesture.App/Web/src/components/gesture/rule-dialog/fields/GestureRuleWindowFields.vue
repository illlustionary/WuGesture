<script setup>
import CustomSelect from '@/components/form/CustomSelect.vue'
import GestureRuleField from './GestureRuleField.vue'

defineProps({
  draft: { type: Object, required: true },
  operations: { type: Array, default: () => [] }
})
</script>

<template>
  <GestureRuleField label="执行操作">
    <CustomSelect
      v-model="draft.windowOperation"
      :options="operations"
      placeholder="选择窗口操作"
    />
  </GestureRuleField>
</template>
