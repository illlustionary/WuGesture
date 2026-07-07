<script setup>
defineProps({
  label: { type: String, required: true },
  note: { type: String, default: '' },
  check: { type: Boolean, default: false }
})
</script>

<template>
  <label
    class="field-card"
    :class="{ 'settings-field--check': check }"
  >
    <span class="field-label">{{ label }}</span>
    <slot />
    <small
      v-if="note"
      class="field-note"
    >
      {{ note }}
    </small>
  </label>
</template>

<style scoped lang="scss">
.settings-field--check {
  align-content: center;

  input {
    width: 22px;
    height: 22px;
    accent-color: var(--accent);
  }
}
</style>
