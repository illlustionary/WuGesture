<template>
  <div class="settings-form-grid form-grid">
    <slot />
  </div>
</template>

<style scoped lang="scss">
@media (max-width: 960px) {
  .settings-form-grid {
    grid-template-columns: 1fr;
  }
}
</style>
