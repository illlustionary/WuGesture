<template>
  <section class="page-shell panel panel--surface">
    <div class="page-shell__head">
      <div>
        <h2>{{ title }}</h2>
        <p>{{ description }}</p>
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
.panel,
.page-shell {
  border: 1px solid var(--border);
  background: var(--panel);
  box-shadow: var(--shadow);
  backdrop-filter: blur(18px);
}

.page-shell {
  padding: 18px;
  border-radius: 24px;
  overflow: hidden;

  &__head {
    margin-bottom: 14px;

    h2 {
      font-size: 22px;
      line-height: 1.2;
    }

    p {
      margin-top: 6px;
      color: var(--muted);
      font-size: 14px;
    }
  }

  &__grid {
    display: grid;
    gap: 18px;

    &--split {
      grid-template-columns: 260px minmax(0, 1fr);
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
