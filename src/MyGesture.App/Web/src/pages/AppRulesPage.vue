<script setup>
import { ref } from "vue";
import AppShell from "../components/AppShell.vue";
import GestureRuleList from "../components/GestureRuleList.vue";
import ScopeSidebar from "../components/ScopeSidebar.vue";
import ScopeCreateDialog from "../components/ScopeCreateDialog.vue";
import { useGestureEditorStore } from "../composables/gestureEditorStore";

const editor = useGestureEditorStore();
const scopeKind = "app";
editor.setActiveScope(scopeKind);
const appDraft = ref("");
const appDialogOpen = ref(false);

function openAppDialog() {
  appDraft.value = "";
  appDialogOpen.value = true;
}

function closeAppDialog() {
  appDialogOpen.value = false;
}

function confirmAppDialog() {
  if (editor.createScopeTarget(scopeKind, appDraft.value)) {
    closeAppDialog();
  }
}
</script>

<template>
  <AppShell
    title="App 规则"
    description="左边选择具体 App，右边只保留手势列表。"
    layout-class="page-shell__grid--split page-shell__grid--editor"
  >
    <template #left>
      <ScopeSidebar
        title="App 名称"
        description="选择一个 App，或在底部新增。"
      >
        <div v-if="editor.appItems.length > 0" class="scope-list">
          <button
            v-for="item in editor.appItems"
            :key="item.name"
            type="button"
            class="scope-item"
            :class="{ active: item.name === editor.getSelectedName(scopeKind) }"
            @click="editor.selectScope(scopeKind, item.name)"
          >
            <span class="scope-item__main">
              <img v-if="item.icon" class="app-icon app-icon--small" :src="item.icon" alt="">
              <span>{{ item.displayName || item.name }}</span>
            </span>
            <small>{{ item.count }} 条</small>
          </button>
        </div>
        <div v-else class="empty-state">还没有 App，先新增一个。</div>

        <div class="scope-panel__footer">
          <button type="button" class="primary-button" @click="openAppDialog">新增 App</button>
          <button type="button" class="ghost-button" @click="editor.deleteSelectedScope(scopeKind)">删除当前 App</button>
        </div>
      </ScopeSidebar>
    </template>

    <template #right>
      <section class="rules-panel rules-panel--stacked rules-panel--editor rules-panel--app-only">
        <section class="rules-panel__section rules-panel__section--flex">
          <div class="rules-panel__head">
            <div>
              <h3>手势列表</h3>
              <p>这里只维护当前 App 的规则。</p>
            </div>
            <div class="rules-panel__actions">
              <button type="button" class="primary-button" @click="editor.openAddRule(scopeKind)">添加手势...</button>
            </div>
          </div>

          <div v-if="editor.getRulesForScope(scopeKind).length === 0" class="empty-state empty-state--large">
            先在左侧选择一个 App。
          </div>

          <GestureRuleList
            :rules="editor.getRulesForScope(scopeKind)"
            :is-recording="editor.isRecordingHotkey"
            :get-gesture-mnemonic="editor.getGestureMnemonic"
            @remove="editor.removeRule"
            @edit="editor.openEditRule"
            @record="editor.startRecording"
            @rename="editor.updateRuleActionName"
          />
        </section>
      </section>
    </template>
  </AppShell>

  <ScopeCreateDialog
    v-model="appDraft"
    :open="appDialogOpen"
    title="新增 App"
    description="输入一个进程名，创建后会出现在左侧列表。"
    label="App 名称"
    placeholder="例如：notepad.exe"
    confirm-text="新增 App"
    @close="closeAppDialog"
    @confirm="confirmAppDialog"
  />
</template>
