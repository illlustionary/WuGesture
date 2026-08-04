<script setup>
import GestureRuleField from './GestureRuleField.vue'

defineProps({
  draft: { type: Object, required: true }
})
</script>

<template>
  <GestureRuleField label="名称">
    <input
      v-model.trim="draft.actionName"
      class="scope-input"
      placeholder="例如：关闭标签"
    />
  </GestureRuleField>
</template>
