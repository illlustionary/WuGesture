<template>
  <div class="settings-toggle-row">
    <slot />
  </div>
</template>

<style scoped lang="scss">
.settings-toggle-row {
  display: flex;
  align-items: center;
  border-radius: var(--md-sys-shape-corner-small);
  background: var(--md-sys-color-surface-container);
}
</style>
