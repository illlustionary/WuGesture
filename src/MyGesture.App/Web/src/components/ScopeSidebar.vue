<template>
  <aside class="scope-panel surface-card">
    <div class="scope-panel__head">
      <div>
        <h3>{{ title }}</h3>
        <p>{{ description }}</p>
      </div>
      <div
        v-if="$slots.actions"
        class="scope-panel__actions"
      >
        <slot name="actions" />
      </div>
    </div>

    <slot />
  </aside>
</template>

<script setup>
defineProps({
  title: { type: String, required: true },
  description: { type: String, required: true }
});
</script>

<style scoped lang="scss">
.scope-panel {
  display: flex;
  flex-direction: column;
  gap: 18px;
  min-width: 0;
  min-height: 100%;
  padding: 20px 18px 18px;
  background: rgba(255, 255, 255, 0.92);
  box-shadow: 0 18px 40px rgba(18, 30, 42, 0.07);

  &__head {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 8px;
  }

  &__actions {
    display: flex;
    gap: 6px;
    flex-shrink: 0;
  }

  h3 {
    font-size: 17px;
    font-weight: 700;
  }

  p {
    color: var(--muted);
    font-size: 13px;
  }
}

@media (max-width: 720px) {
  .scope-panel {
    padding: 16px;
  }
}
</style>
