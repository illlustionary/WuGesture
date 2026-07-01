<script setup>
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

    <GestureRuleDialog
      :open="editor.state.gestureEditorOpen"
      :draft="editor.state.gestureDraft"
      :message="editor.state.gestureRecognitionMessage"
      @close="editor.closeGestureEditor"
      @confirm="editor.saveGestureEditor"
      @record="editor.recordGesturePoints"
    />

    <div v-if="editor.state.applicationPickerOpen" class="modal-backdrop" @click.self="editor.closeApplicationPicker()">
      <section class="modal-panel" role="dialog" aria-modal="true" aria-labelledby="application-picker-title">
        <div class="modal-panel__head">
          <h3 id="application-picker-title">添加程序</h3>
          <button type="button" class="ghost-button" @click="editor.closeApplicationPicker()">关闭</button>
        </div>

        <div class="picker-options">
          <button
            type="button"
            class="picker-option"
            @pointerdown.prevent="editor.pickApplicationWindow()"
            @click.prevent
          >
            <strong>拖动准星选择窗口</strong>
            <span>从正在打开的目标窗口读取程序名称和路径。</span>
          </button>

          <button type="button" class="picker-option" @click="editor.selectApplication()">
            <strong>浏览 exe 文件</strong>
            <span>从磁盘选择程序文件作为备用添加方式。</span>
          </button>
        </div>
      </section>
    </div>
  </div>
</template>
