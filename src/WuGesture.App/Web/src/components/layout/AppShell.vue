<script setup lang="ts">
defineProps<{
  layoutClass?: string
  title?: string
  description?: string
}>()
</script>

<template>
  <section class="page-shell">
    <header
      v-if="title || description || $slots.actions || $slots.header"
      class="page-shell__header"
    >
      <div class="page-shell__heading">
        <slot name="header">
          <h1
            v-if="title"
            class="page-shell__title"
          >
            {{ title }}
          </h1>
          <p
            v-if="description"
            class="page-shell__description"
          >
            {{ description }}
          </p>
        </slot>
      </div>

      <div
        v-if="$slots.actions"
        class="page-shell__actions"
      >
        <slot name="actions" />
      </div>
    </header>

    <div
      class="page-shell__grid"
      :class="layoutClass"
    >
      <slot name="left" />
      <slot name="right" />
    </div>
  </section>
</template>

<style scoped lang="scss">
.page-shell {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  gap: var(--md-sys-spacing-lg);
  width: 100%;
  min-height: 100%;
  padding: 0 var(--md-sys-spacing-xl) var(--md-sys-spacing-xl);

  &__header {
    display: flex;
    flex: 0 0 auto;
    align-items: center;
    justify-content: space-between;
    gap: var(--md-sys-spacing-lg);
    min-height: 56px;
  }

  &__heading {
    display: grid;
    min-width: 0;
    gap: 2px;
  }

  &__title {
    min-width: 0;
    overflow: hidden;
    color: var(--md-sys-color-on-surface);
    font-size: var(--md-sys-typescale-title-large-size);
    line-height: var(--md-sys-typescale-title-large-line-height);
    font-weight: var(--md-sys-typescale-title-large-weight);
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__description {
    min-width: 0;
    overflow: hidden;
    color: var(--md-sys-color-on-surface-variant);
    font-size: var(--md-sys-typescale-body-small-size);
    line-height: var(--md-sys-typescale-body-small-line-height);
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__actions {
    display: flex;
    flex: 0 0 auto;
    align-items: center;
    gap: var(--md-sys-spacing-sm);
  }

  &__grid {
    display: grid;
    flex: 1 1 auto;
    align-items: stretch;
    gap: var(--md-sys-spacing-xl);
    min-height: 0;

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
