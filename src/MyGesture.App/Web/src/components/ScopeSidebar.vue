<template>
  <aside class="scope-panel surface-card">
    <div class="scope-panel__head">
      <div>
        <h3>{{ title }}</h3>
        <p>{{ description }}</p>
      </div>
      <slot name="actions" />
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
  padding: 20px;

  &__head {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
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
