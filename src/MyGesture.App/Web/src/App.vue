<script setup>
import { ref } from "vue";
import { RouterLink, RouterView } from "vue-router";
import GestureRuleDialog from "./components/GestureRuleDialog.vue";
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
      <div class="app-bar__title">
        <p>MyGesture</p>
        <h1>手势规则编辑器</h1>
      </div>

      <div class="app-bar__meta">
        <div class="status-badge" :data-state="editor.state.statusState">{{ editor.state.statusText }}</div>
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
        <button type="button" class="menu-button" aria-label="更多菜单">☰</button>
      </div>
    </header>

    <RouterView />

    <GestureRuleDialog
      :open="editor.state.gestureEditorOpen"
      :draft="editor.state.gestureDraft"
      :message="editor.state.gestureRecognitionMessage"
      :is-recording-hotkey="editor.isRecordingHotkey"
      :get-gesture-mnemonic="editor.getGestureMnemonic"
      @close="editor.closeGestureEditor"
      @confirm="editor.saveGestureEditor"
      @record="editor.recordGesturePoints"
      @record-hotkey="editor.startRecording"
    />
  </div>
</template>
