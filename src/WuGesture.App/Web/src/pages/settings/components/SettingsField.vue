<script setup>
defineProps({
  label: { type: String, required: true },
  note: { type: String, default: '' }
})
</script>

<template>
  <label class="field-card">
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
