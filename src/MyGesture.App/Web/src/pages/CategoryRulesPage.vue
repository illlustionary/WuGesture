<script setup>
import AppShell from "../components/AppShell.vue";
import GestureRuleList from "../components/GestureRuleList.vue";
import ScopeSidebar from "../components/ScopeSidebar.vue";
import { useGestureEditor } from "../composables/gestureEditor";

const editor = useGestureEditor();
</script>

<template>
  <AppShell
    title="分类规则"
    description="左侧是分类列表，右侧上方显示选中程序图标，下方显示三列规则表。"
    :config-path="editor.configPath"
    :layout-class="editor.workspaceClass"
  >
    <template #left>
      <ScopeSidebar
        title="分类"
        description="选择一个分类来编辑它下面的规则。"
        placeholder="输入分类名"
        empty-text="还没有分类，先新增一个。"
        :items="editor.scopeItems"
        :selected-name="editor.selectedScopeName"
        :draft="editor.scopeDraft"
        @create="editor.createScopeTarget"
        @select="editor.selectTab('category') || editor.selectedScopeName"
        @rename="editor.renameSelectedScope"
        @delete="editor.deleteSelectedScope"
      />
    </template>

    <template #right>
      <section class="rules-panel">
        <div class="rules-panel__head rules-panel__head--stacked">
          <div class="scope-preview">
            <div class="scope-preview__icon">A</div>
            <div>
              <h3>{{ editor.selectedScopeName || '未选择分类' }}</h3>
              <p>上面显示当前选中程序图标，下面是该分类的规则列表。</p>
            </div>
          </div>
          <button type="button" class="primary-button" @click="editor.addRule">新增规则</button>
        </div>

        <div v-if="editor.visibleRules.length === 0" class="empty-state empty-state--large">
          先在左侧选择一个分类。
        </div>

        <GestureRuleList :rules="editor.visibleRules" @remove="editor.removeRule" />
      </section>
    </template>
  </AppShell>
</template>
