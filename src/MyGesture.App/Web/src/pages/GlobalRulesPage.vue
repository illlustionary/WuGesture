<script setup>
import AppShell from '../components/AppShell.vue'
import GestureRuleList from '../components/GestureRuleList.vue'
import { useGestureEditorStore } from '../composables/gestureEditorStore'

const editor = useGestureEditorStore()
const scopeKind = 'global'
editor.setActiveScope(scopeKind)
</script>

<template>
  <AppShell
    title="全局规则"
    description="全局规则对所有程序生效"
    layout-class="page-shell__grid--single"
  >
    <template #right>
      <section class="rules-panel">
        <div class="rules-panel__head">
          <button
            type="button"
            class="primary-button"
            @click="editor.openAddRule(scopeKind)"
          >
            新增规则
          </button>
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
    </template>
  </AppShell>
</template>

<style scoped lang="scss">
.rules-panel {
  display: flex;
  flex-direction: column;
  gap: 16px;
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
}
</style>
