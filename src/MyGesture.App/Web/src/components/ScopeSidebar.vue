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
