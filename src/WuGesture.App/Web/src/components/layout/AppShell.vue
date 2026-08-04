<template>
  <section class="page-shell">
    <div
      v-if="$slots.actions"
      class="page-shell__actions"
    >
      <slot name="actions" />
    </div>

    <div
      class="page-shell__grid"
      :class="layoutClass"
    >
      <slot name="left" />
      <slot name="right" />
    </div>
  </section>
</template>

<script setup>
defineProps({ layoutClass: { type: String, default: '' } })
</script>

<style scoped lang="scss">
.panel {
  border: 1px solid var(--border);
  background: var(--panel);
  box-shadow: var(--shadow-soft);
  backdrop-filter: blur(24px) saturate(1.18);
}

.page-shell {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  width: 100%;
  min-height: 100%;

  &__actions {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    margin-bottom: 12px;
  }

  &__grid {
    display: grid;
    flex: 1 1 auto;
    gap: 0;
    min-height: 100%;

    &--split {
      grid-template-columns: 258px minmax(0, 1fr);
    }

    &--single {
      grid-template-columns: minmax(0, 1fr);
    }

    &--editor {
      align-items: stretch;
    }
  }
}
</style>
