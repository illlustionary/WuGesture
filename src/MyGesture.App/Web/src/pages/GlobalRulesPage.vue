<script setup>
import AppShell from '../components/AppShell.vue'
import IconActionButton from '../components/IconActionButton.vue'
import GestureRuleList from '../components/GestureRuleList.vue'
import ScopeSidebar from '../components/ScopeSidebar.vue'
import { useGestureEditorStore } from '../composables/gestureEditorStore'

const editor = useGestureEditorStore()
const scopeKind = 'global'
editor.setActiveScope(scopeKind)
</script>

<template>
  <AppShell
    title="全局规则"
    description="全局规则对所有程序生效。"
    layout-class="page-shell__grid--split page-shell__grid--editor"
  >
    <template #left>
      <ScopeSidebar
        title="全局说明"
        description="这里维护不依赖分类和程序的默认手势。"
      >
        <div class="scope-summary">
          <strong>作用范围</strong>
          <span>所有程序</span>
        </div>
        <div class="scope-summary">
          <strong>规则数量</strong>
          <span>{{ editor.globalRules.length }} 条</span>
        </div>
      </ScopeSidebar>
    </template>

    <template #right>
      <section class="rules-panel rules-panel--stacked rules-panel--editor">
        <section class="rules-panel__section rules-panel__section--flex">
          <div class="rules-panel__head">
            <div>
              <h3>手势列表</h3>
              <p>这里的规则会影响所有页面。</p>
            </div>
            <div class="rules-panel__actions">
              <IconActionButton
                icon="add"
                label="添加规则"
                class="primary-button"
                @click="editor.openAddRule(scopeKind)"
              />
            </div>
          </div>

          <GestureRuleList
            :rules="editor.globalRules"
            :get-gesture-mnemonic="editor.getGestureMnemonic"
            :get-action-label="editor.getActionLabel"
            @remove="editor.removeRule"
            @edit="editor.openEditRule"
            @rename="editor.updateRuleActionName"
          />
        </section>
      </section>
    </template>
  </AppShell>
</template>

<style scoped lang="scss">
.scope-summary {
  display: grid;
  gap: 4px;
  padding: 12px 14px;
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.72);
  color: var(--muted);

  strong {
    color: var(--text);
    font-size: 13px;
  }

  span {
    font-size: 14px;
  }
}

.rules-panel {
  display: flex;
  flex-direction: column;
  gap: 18px;
  min-width: 0;

  h3 {
    font-size: 16px;
    font-weight: 700;
  }

  p {
    color: var(--muted);
    font-size: 13px;
  }

  &__head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 10px;
    margin-bottom: 4px;
  }

  &__actions {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
  }
}
</style>
