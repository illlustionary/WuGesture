<script setup>
defineProps({
  title: { type: String, required: true },
  compact: { type: Boolean, default: false },
  flex: { type: Boolean, default: false }
})
</script>

<template>
  <section
    class="rules-section section-card"
    :class="{
      'rules-section--compact': compact,
      'rules-section--flex': flex
    }"
  >
    <div class="rules-section__head">
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
  </section>
</template>

<style scoped lang="scss">
.rules-section {
  &--compact {
    flex: 0 0 auto;
  }

  &--flex {
    display: flex;
    flex-direction: column;
    gap: 14px;
    flex: 1 1 auto;
  }

  &__head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 10px;
    margin-bottom: 4px;
  }

  .empty-state {
    margin-top: 2px;
  }
}

@media (max-width: 720px) {
  .rules-section {
    padding: 16px;
  }
}
</style>
