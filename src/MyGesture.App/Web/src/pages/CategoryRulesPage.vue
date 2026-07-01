<script setup>
import { ref } from "vue";
import AppShell from "../components/AppShell.vue";
import GestureRuleList from "../components/GestureRuleList.vue";
import ScopeSidebar from "../components/ScopeSidebar.vue";
import ScopeCreateDialog from "../components/ScopeCreateDialog.vue";
import { useGestureEditorStore } from "../composables/gestureEditorStore";

const editor = useGestureEditorStore();
const scopeKind = "category";
editor.setActiveScope(scopeKind);
const categoryDraft = ref("");
const categoryDialogOpen = ref(false);

function openCategoryDialog() {
  categoryDraft.value = "";
  categoryDialogOpen.value = true;
}

function closeCategoryDialog() {
  categoryDialogOpen.value = false;
}

function confirmCategoryDialog() {
  if (editor.createScopeTarget(scopeKind, categoryDraft.value)) {
    closeCategoryDialog();
  }
}
</script>

<template>
  <AppShell
    title="手势分类"
    description="创建分类来为一组应用程序共享同一套手势设置。"
    layout-class="page-shell__grid--split page-shell__grid--editor"
  >
    <template #left>
      <ScopeSidebar
        title="分类名称"
        description="选择一个分类，或在底部新增。"
      >
        <div v-if="editor.categoryItems.length > 0" class="scope-list">
          <button
            v-for="item in editor.categoryItems"
            :key="item.name"
            type="button"
            class="scope-item"
            :class="{ active: item.name === editor.getSelectedName(scopeKind) }"
            @click="editor.selectScope(scopeKind, item.name)"
          >
            <span class="scope-item__main">
              <span>{{ item.name }}</span>
            </span>
            <small>{{ item.count }} 条</small>
          </button>
        </div>
        <div v-else class="empty-state">还没有分类，先新增一个。</div>

        <div class="scope-panel__footer">
          <button type="button" class="primary-button" @click="openCategoryDialog">新增分类</button>
          <button type="button" class="ghost-button" @click="editor.deleteSelectedScope(scopeKind)">删除当前分类</button>
        </div>
      </ScopeSidebar>
    </template>

    <template #right>
      <section class="rules-panel rules-panel--stacked rules-panel--editor">
        <section class="rules-panel__section rules-panel__section--compact">
          <div class="rules-panel__head">
            <div>
              <h3>应用程序</h3>
              <p>当前分类关联的程序会从这里管理。</p>
            </div>
            <div class="rules-panel__actions">
              <button type="button" class="secondary-button" @click="editor.openApplicationPicker(editor.getSelectedName(scopeKind))">添加程序...</button>
            </div>
          </div>

          <div v-if="editor.getApplicationsForCategory().length === 0" class="empty-state empty-state--compact">
            当前分类还没有关联程序。
          </div>
          <div v-else class="app-list">
            <div
              v-for="app in editor.getApplicationsForCategory()"
              :key="app.name"
              class="app-list__item"
            >
              <span class="app-list__name">
                <img v-if="app.icon" class="app-icon app-icon--small" :src="app.icon" alt="">
                <span>{{ app.displayName || app.name }}</span>
              </span>
              <span class="app-list__path">{{ app.path || "未设置路径" }}</span>
              <button type="button" class="ghost-button" @click="editor.removeAppFromCategory(app.name)">移除</button>
            </div>
          </div>
        </section>

        <section class="rules-panel__section rules-panel__section--flex">
          <div class="rules-panel__head">
            <div>
              <h3>手势列表</h3>
              <p>分类级规则会在这里统一维护。</p>
            </div>
            <div class="rules-panel__actions">
              <button type="button" class="primary-button" @click="editor.openAddRule(scopeKind)">添加手势...</button>
            </div>
          </div>

          <div v-if="editor.getRulesForScope(scopeKind).length === 0" class="empty-state empty-state--large">
            先在左侧选择一个分类。
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
    v-model="categoryDraft"
    :open="categoryDialogOpen"
    title="新增分类"
    description="输入一个分类名称，创建后会出现在左侧列表。"
    label="分类名称"
    placeholder="例如：浏览器"
    confirm-text="新增分类"
    @close="closeCategoryDialog"
    @confirm="confirmCategoryDialog"
  />
</template>
