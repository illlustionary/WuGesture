<template>
  <section class="page-shell panel panel--surface">
    <div class="page-shell__head">
      <div>
        <h2>{{ title }}</h2>
        <p>{{ description }}</p>
      </div>
    </div>

    <div class="page-shell__grid" :class="layoutClass">
      <slot name="left" />
      <slot name="right" />
    </div>
  </section>
</template>

<script setup>
defineProps({
  title: { type: String, required: true },
  description: { type: String, required: true },
  layoutClass: { type: String, default: "" }
});
</script>
