<script setup>
import AppShell from "../components/AppShell.vue";
import GestureRuleList from "../components/GestureRuleList.vue";
import ScopeSidebar from "../components/ScopeSidebar.vue";
import { useGestureEditor } from "../composables/gestureEditor";

const editor = useGestureEditor();
</script>

<template>
  <AppShell
    title="App 规则"
    description="左侧是 App 列表，右侧是该 App 的规则。"
    :config-path="editor.configPath"
    :layout-class="editor.workspaceClass"
  >
    <template #left>
      <ScopeSidebar
        title="App"
        description="选择一个具体应用来编辑它下面的规则。"
        placeholder="输入 App 名"
        empty-text="还没有 App，先新增一个。"
        :items="editor.scopeItems"
        :selected-name="editor.selectedScopeName"
        :draft="editor.scopeDraft"
        @create="editor.createScopeTarget"
        @select="editor.selectTab('app') || editor.selectedScopeName"
        @rename="editor.renameSelectedScope"
        @delete="editor.deleteSelectedScope"
      />
    </template>

    <template #right>
      <section class="rules-panel">
        <div class="rules-panel__head">
          <div>
            <h3>{{ editor.selectedScopeName || '未选择 App' }}</h3>
            <p>这里显示该 App 的规则列表。</p>
          </div>
          <button type="button" class="primary-button" @click="editor.addRule">新增规则</button>
        </div>

        <div v-if="editor.visibleRules.length === 0" class="empty-state empty-state--large">
          先在左侧选择一个 App。
        </div>

        <GestureRuleList :rules="editor.visibleRules" @remove="editor.removeRule" />
      </section>
    </template>
  </AppShell>
</template>
