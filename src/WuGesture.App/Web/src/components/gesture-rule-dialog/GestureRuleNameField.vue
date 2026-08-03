<script setup>
defineProps({
  draft: { type: Object, required: true }
})
</script>

<template>
  <label class="gesture-rule-name-field">
    <span>名称</span>
    <input
      v-model.trim="draft.actionName"
      class="scope-input"
      placeholder="例如：关闭标签"
    />
  </label>
</template>

<style scoped lang="scss">
.gesture-rule-name-field {
  display: grid;
  min-width: 0;
  gap: 6px;
  color: var(--muted);
  font-size: 13px;
}
</style>
