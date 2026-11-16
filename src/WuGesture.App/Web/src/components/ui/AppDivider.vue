<script setup lang="ts">
withDefaults(
  defineProps<{
    inset?: boolean
    vertical?: boolean
  }>(),
  {
    inset: false,
    vertical: false
  }
)
</script>

<template>
  <div
    class="app-divider"
    :class="{ 'app-divider--inset': inset, 'app-divider--vertical': vertical }"
    role="separator"
  />
</template>

<style scoped lang="scss">
.app-divider {
  width: 100%;
  height: 1px;
  flex: 0 0 auto;
  border: 0;
  background: var(--md-sys-color-outline-variant);
}

.app-divider--inset {
  width: auto;
  margin: 0 16px;
}

.app-divider--vertical {
  width: 1px;
  height: auto;
  align-self: stretch;
}
</style>
