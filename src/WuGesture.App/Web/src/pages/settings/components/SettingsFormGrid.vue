<template>
  <div class="settings-form-grid">
    <slot />
  </div>
</template>

<style scoped lang="scss">
.settings-form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--md-sys-spacing-lg);
}

@media (max-width: 960px) {
  .settings-form-grid {
    grid-template-columns: 1fr;
  }
}
</style>
