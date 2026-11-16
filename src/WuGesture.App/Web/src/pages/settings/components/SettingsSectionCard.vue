<script setup>
defineProps({
  title: { type: String, required: true },
  note: { type: String, default: '' }
})
</script>

<template>
  <section class="section-card">
    <h3 class="section-title">{{ title }}</h3>

    <slot />

    <p
      v-if="note"
      class="section-note"
    >
      {{ note }}
    </p>

    <div
      v-if="$slots.footer"
      class="section-footer"
    >
      <slot name="footer" />
    </div>
  </section>
</template>

<style scoped lang="scss">
.section-note {
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
}

.section-footer {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: var(--md-sys-spacing-sm);
  padding-top: var(--md-sys-spacing-lg);
  border-top: 1px solid var(--md-sys-color-outline-variant);
}
</style>
