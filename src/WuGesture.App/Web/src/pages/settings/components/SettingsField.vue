<script setup lang="ts">
withDefaults(
  defineProps<{
    label: string
    note?: string
  }>(),
  {
    note: ''
  }
)
</script>

<template>
  <label class="settings-field">
    <span class="settings-field__label">
      {{ label }}
      <small
        v-if="note"
        class="settings-field__note"
      >
        （{{ note }}）
      </small>
    </span>
    <slot />
  </label>
</template>

<style scoped lang="scss">
.settings-field {
  display: grid;
  gap: var(--md-sys-spacing-sm);
  padding: var(--md-sys-spacing-md);
  border-radius: var(--md-sys-shape-corner-small);
  background: var(--md-sys-color-surface-container);
}

.settings-field__label {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: var(--md-sys-spacing-xs);
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-label-large-size);
  line-height: var(--md-sys-typescale-label-large-line-height);
  font-weight: var(--md-sys-typescale-label-large-weight);
}

.settings-field__note {
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
  font-weight: 400;
}
</style>
