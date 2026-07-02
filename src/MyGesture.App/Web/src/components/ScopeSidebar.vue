<template>
  <aside class="scope-panel">
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
  gap: 16px;
  min-width: 0;
  min-height: 100%;
  padding: 18px;
  border: 1px solid var(--border);
  border-radius: 22px;
  background: rgba(255, 255, 255, 0.76);
  box-shadow: var(--shadow);

  &__head {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 10px;
  }

  h3 {
    font-size: 16px;
    font-weight: 700;
  }

  p {
    color: var(--muted);
    font-size: 13px;
  }
}

@media (max-width: 720px) {
  .scope-panel {
    padding: 14px;
  }
}
</style>
