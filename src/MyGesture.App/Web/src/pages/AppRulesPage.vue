<script setup>
import { ref } from "vue";
import AppShell from "../components/AppShell.vue";
import GestureRuleList from "../components/GestureRuleList.vue";
import ScopeSidebar from "../components/ScopeSidebar.vue";
import { useGestureEditorStore } from "../composables/gestureEditorStore";

const editor = useGestureEditorStore();
const scopeKind = "app";
editor.setActiveScope(scopeKind);
const appDraft = ref("");
</script>

<template>
  <AppShell
    title="App 规则"
    description="左边选择具体 App，右边展示同样的规则表。"
    layout-class="page-shell__grid--split"
  >
    <template #left>
      <ScopeSidebar
        title="App 名称"
        description="选择或新建一个 App。"
      >
        <template #actions>
          <div class="scope-panel__actions scope-panel__actions--stacked">
            <input
              class="scope-input"
              placeholder="输入 App 名"
              v-model="appDraft"
              @keydown.enter.prevent="editor.createScopeTarget(scopeKind, appDraft); appDraft = ''"
            >
            <button type="button" class="secondary-button" @click="editor.createScopeTarget(scopeKind, appDraft); appDraft = ''">新增 App</button>
            <button type="button" class="secondary-button" @click="editor.openApplicationPicker()">添加程序...</button>
            <button type="button" class="ghost-button" @click="editor.deleteSelectedScope(scopeKind)">删除 App</button>
          </div>
        </template>

        <div v-if="editor.appItems.length > 0" class="scope-list">
          <button
            v-for="item in editor.appItems"
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
        <div v-else class="empty-state">还没有 App，先新增一个。</div>
      </ScopeSidebar>
    </template>

    <template #right>
      <section class="rules-panel rules-panel--stacked rules-panel--three-col">
        <div class="scope-preview">
          <div class="scope-preview__icon">A</div>
          <div>
            <h3>{{ editor.getSelectedName(scopeKind) || "未选择 App" }}</h3>
            <p>左侧是 App 列表，中间编辑当前 App 的规则，右侧保留参数区域。</p>
          </div>
        </div>

        <div class="rules-panel__body">
          <section class="rules-panel__section">
            <div class="rules-panel__head">
              <div>
                <h3>应用程序</h3>
              </div>
            </div>

            <div class="app-detail">
              <label>
                <span>程序名称</span>
                <input
                  class="scope-input"
                  :value="editor.getSelectedName(scopeKind)"
                  readonly
                >
              </label>

              <label>
                <span>所属分类</span>
                <select
                  class="scope-input"
                  :value="editor.getApplication()?.category ?? ''"
                  @change="editor.updateApplicationCategory(editor.getSelectedName(scopeKind), $event.target.value)"
                >
                  <option value="">无分类</option>
                  <option
                    v-for="category in editor.categoryItems"
                    :key="category.name"
                    :value="category.name"
                  >
                    {{ category.name }}
                  </option>
                </select>
              </label>

              <label>
                <span>路径</span>
                <input
                  class="scope-input"
                  :value="editor.getApplication()?.path || ''"
                  placeholder="后续接入选择 exe"
                  readonly
                >
              </label>

              <button
                type="button"
                class="secondary-button"
                @click="editor.openApplicationPicker(editor.getApplication()?.category ?? '')"
              >
                添加程序...
              </button>
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
              先在左侧选择一个 App。
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
