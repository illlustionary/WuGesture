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
    title="分类规则"
    description="左边选择分类，右边上方显示图标位，下方是规则表。"
    layout-class="page-shell__grid--split"
  >
    <template #left>
      <ScopeSidebar
        title="分类"
        description="左侧选择一个分类。"
      >
        <template #actions>
          <div class="scope-panel__actions scope-panel__actions--stacked">
            <input
              class="scope-input"
              placeholder="输入分类名"
              v-model="categoryDraft"
              @keydown.enter.prevent="editor.createScopeTarget(scopeKind, categoryDraft); categoryDraft = ''"
            >
            <button type="button" class="secondary-button" @click="editor.createScopeTarget(scopeKind, categoryDraft); categoryDraft = ''">新增</button>
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
      <section class="rules-panel rules-panel--stacked">
        <div class="scope-preview">
          <div class="scope-preview__icon">A</div>
          <div>
            <h3>{{ editor.getSelectedName(scopeKind) || "未选择分类" }}</h3>
            <p>上面是当前选中程序图标位，下面是该分类的规则列表。</p>
          </div>
        </div>

        <div class="rules-panel__head">
          <div>
            <h3>分类规则</h3>
            <p>每个分类共用同样的表结构。</p>
          </div>
          <button type="button" class="primary-button" @click="editor.addRule(scopeKind)">新增规则</button>
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
    </template>
  </AppShell>
</template>
