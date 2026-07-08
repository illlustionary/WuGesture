<script setup>
import AppShell from '../../components/AppShell.vue'
import GestureRuleList from '../../components/GestureRuleList.vue'
import IconActionButton from '../../components/IconActionButton.vue'
import ScopeSidebar from '../../components/ScopeSidebar.vue'
import RulesSection from '../../components/rules/RulesSection.vue'
import { useGestureEditorStore } from '../../composables/gestureEditorStore'
import { SCOPE_KINDS } from '../../constants/gestureEditorOptions'

const editor = useGestureEditorStore()
const scopeKind = SCOPE_KINDS.global
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
      <section class="rules-panel">
        <RulesSection
          title="手势列表"
          description="这里的规则会影响所有页面。"
          flex
        >
          <template #actions>
            <IconActionButton
              icon="add"
              label="添加规则"
              class="primary-button"
              @click="editor.openAddRule(scopeKind)"
            />
          </template>

          <GestureRuleList
            :rules="editor.globalRules"
            :get-gesture-mnemonic="editor.getGestureMnemonic"
            :get-action-label="editor.getActionLabel"
            @remove="editor.removeRule"
            @edit="editor.openEditRule"
            @rename="editor.updateRuleActionName"
          />
        </RulesSection>
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
  background: var(--panel-soft);
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
}
</style>
