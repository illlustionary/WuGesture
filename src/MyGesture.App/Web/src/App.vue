<script setup>
import { RouterLink, RouterView } from "vue-router";
import { useGestureEditorStore } from "./composables/gestureEditorStore";

const editor = useGestureEditorStore();
const tabs = [
  { to: "/global", label: "全局" },
  { to: "/category", label: "分类" },
  { to: "/app", label: "App" }
];

editor.initialize();
</script>

<template>
  <div class="app-shell">
    <header class="app-bar">
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

      <div class="app-bar__meta">
        <div class="status-badge" :data-state="editor.state.statusState">{{ editor.state.statusText }}</div>
        <button type="button" class="menu-button" aria-label="更多菜单">☰</button>
      </div>
    </header>

    <RouterView />
  </div>
</template>
