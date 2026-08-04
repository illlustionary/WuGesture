<script setup>
defineProps({
  label: { type: String, required: true }
})
</script>

<template>
  <div class="gesture-rule-field">
    <span class="gesture-rule-field__label">{{ label }}</span>
    <slot />
  </div>
</template>

<style scoped lang="scss">
.gesture-rule-field {
  display: grid;
  min-width: 0;
  gap: 6px;
  color: var(--muted);
  font-size: 13px;
}
</style>
