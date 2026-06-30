<script setup>
import { RouterLink, RouterView } from "vue-router";

const tabs = [
  { to: "/global", label: "全局" },
  { to: "/category", label: "分类" },
  { to: "/app", label: "App" }
];
</script>

<template>
  <div class="app-shell">
    <header class="hero">
      <div>
        <p class="eyebrow">Mouse gesture editor</p>
        <h1>My Gesture</h1>
        <p class="subtitle">全局、分类、App 三层规则编辑器</p>
      </div>
      <div class="status-badge" :data-state="statusState">{{ statusText }}</div>
    </header>

    <nav class="tabs" aria-label="规则作用域">
      <RouterLink
        v-for="tab in tabs"
        :key="tab.to"
        :to="tab.to"
        class="tab"
        active-class="active"
      >
        {{ tab.label }}
      </RouterLink>
    </nav>

    <RouterView />
  </div>
</template>
