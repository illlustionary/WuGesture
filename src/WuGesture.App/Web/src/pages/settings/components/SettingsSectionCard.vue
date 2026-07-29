<script setup>
defineProps({
  title: { type: String, required: true },
  note: { type: String, default: '' }
})
</script>

<template>
  <section class="section-card">
    <div class="section-head">
      <div>
        <h3 class="section-title">{{ title }}</h3>
      </div>
      <div
        v-if="$slots.actions"
        class="section-actions"
      >
        <slot name="actions" />
      </div>
    </div>

    <slot />

    <p
      v-if="note"
      class="mt-12px text-12px text-muted"
    >
      {{ note }}
    </p>
  </section>
</template>
