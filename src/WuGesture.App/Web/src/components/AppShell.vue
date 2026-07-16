<template>
  <section class="page-shell">
    <div class="page-shell__head">
      <div>
        <h2>{{ title }}</h2>
        <p>{{ description }}</p>
      </div>
      <div
        v-if="$slots.actions"
        class="page-shell__head-actions"
      >
        <slot name="actions" />
      </div>
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
defineProps({
  title: { type: String, required: true },
  description: { type: String, required: true },
  layoutClass: { type: String, default: '' }
})
</script>

<style scoped lang="scss">
.panel {
  border: 1px solid var(--border);
  background: var(--panel);
  box-shadow: var(--shadow-soft);
  backdrop-filter: blur(24px) saturate(1.18);
}

.page-shell {
  border: 1px solid var(--border);
  background: var(--panel);
  box-shadow: var(--shadow-soft);
  padding: 20px;
  border-radius: 28px;
  overflow: hidden;

  &__head {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
    margin-bottom: 16px;

    h2 {
      font-size: 24px;
      line-height: 1.2;
      font-weight: 700;
    }

    p {
      margin-top: 8px;
      color: var(--muted);
      font-size: 14px;
    }
  }

  &__head-actions {
    display: flex;
    align-items: center;
    gap: 8px;
    flex: 0 0 auto;
  }

  &__grid {
    display: grid;
    gap: 20px;

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
