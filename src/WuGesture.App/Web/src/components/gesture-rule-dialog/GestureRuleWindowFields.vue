<script setup>
import CustomSelect from '@/components/CustomSelect.vue'

defineProps({
  draft: { type: Object, required: true },
  operations: { type: Array, default: () => [] }
})
</script>

<template>
  <label class="gesture-rule-command-field">
    <span>执行操作</span>
    <CustomSelect
      v-model="draft.windowOperation"
      :options="operations"
      placeholder="选择窗口操作"
    />
  </label>
</template>

<style scoped lang="scss">
.gesture-rule-command-field {
  display: grid;
  min-width: 0;
  gap: 6px;
  color: var(--muted);
  font-size: 13px;
}
</style>
