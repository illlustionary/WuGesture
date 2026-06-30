<template>
  <section class="panel panel--surface">
    <div class="panel-head">
      <div>
        <h2>{{ title }}</h2>
        <p>{{ description }}</p>
      </div>
      <div class="config-path">{{ configPath }}</div>
    </div>

    <div class="shell-layout" :class="layoutClass">
      <slot name="left" />
      <slot name="right" />
    </div>
  </section>
</template>

<script setup>
defineProps({
  title: { type: String, required: true },
  description: { type: String, required: true },
  configPath: { type: String, required: true },
  layoutClass: { type: String, default: "" }
});
</script>
