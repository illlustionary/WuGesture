<script setup>
import { ref } from "vue";
import AppShell from "../components/AppShell.vue";
import GestureRuleList from "../components/GestureRuleList.vue";
import ScopeSidebar from "../components/ScopeSidebar.vue";
import { useGestureEditorStore } from "../composables/gestureEditorStore";

const editor = useGestureEditorStore();
const scopeKind = "category";
editor.setActiveScope(scopeKind);
const categoryDraft = ref("");
</script>

<template>
  <AppShell
    title="手势分类"
    description="创建分类来为一组应用程序共享同一套手势设置。"
    layout-class="page-shell__grid--split"
  >
    <template #left>
      <ScopeSidebar
        title="分类名称"
        description="选择或新建一个分类。"
      >
        <template #actions>
          <div class="scope-panel__actions scope-panel__actions--stacked">
            <input
              class="scope-input"
              placeholder="输入分类名"
              v-model="categoryDraft"
              @keydown.enter.prevent="editor.createScopeTarget(scopeKind, categoryDraft); categoryDraft = ''"
            >
            <button type="button" class="secondary-button" @click="editor.createScopeTarget(scopeKind, categoryDraft); categoryDraft = ''">新增分类</button>
            <button type="button" class="ghost-button" @click="editor.deleteSelectedScope(scopeKind)">删除分类</button>
          </div>
        </template>

        <div v-if="editor.categoryItems.length > 0" class="scope-list">
          <button
            v-for="item in editor.categoryItems"
            :key="item.name"
            type="button"
            class="scope-item"
            :class="{ active: item.name === editor.getSelectedName(scopeKind) }"
            @click="editor.selectScope(scopeKind, item.name)"
          >
            <span>{{ item.name }}</span>
            <small>{{ item.count }} 条</small>
          </button>
        </div>
        <div v-else class="empty-state">还没有分类，先新增一个。</div>
      </ScopeSidebar>
    </template>

    <template #right>
      <section class="rules-panel rules-panel--stacked rules-panel--three-col">
        <div class="scope-preview">
          <div class="scope-preview__icon">类</div>
          <div>
            <h3>{{ editor.getSelectedName(scopeKind) || "未选择分类" }}</h3>
            <p>左侧是分类列表，中间编辑当前分类的规则，右侧保留参数区域。</p>
          </div>
        </div>

        <div class="rules-panel__body">
          <section class="rules-panel__section">
            <div class="rules-panel__head">
              <div>
                <h3>应用程序</h3>
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

          <section class="rules-panel__section">
            <div class="rules-panel__head">
              <div>
                <h3>手势列表</h3>
              </div>
              <div class="rules-panel__actions">
                <button type="button" class="primary-button" @click="editor.addRule(scopeKind)">添加手势...</button>
                <button type="button" class="secondary-button">修改手势</button>
                <button type="button" class="ghost-button">删除手势</button>
              </div>
            </div>

            <div v-if="editor.getRulesForScope(scopeKind).length === 0" class="empty-state empty-state--large">
              先在左侧选择一个分类。
            </div>

            <GestureRuleList
              :rules="editor.getRulesForScope(scopeKind)"
              @remove="editor.removeRule"
              @record="editor.startRecording"
              @stop-record="editor.stopRecording"
            />
          </section>
        </div>
      </section>
    </template>
  </AppShell>
</template>
