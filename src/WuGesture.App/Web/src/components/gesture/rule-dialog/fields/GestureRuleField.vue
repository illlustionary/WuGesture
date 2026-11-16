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
  gap: var(--md-sys-spacing-sm);
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-label-large-size);
  line-height: var(--md-sys-typescale-label-large-line-height);
  font-weight: var(--md-sys-typescale-label-large-weight);
}
</style>
