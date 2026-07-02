<script setup>
import AppShell from "../components/AppShell.vue";
import GestureRuleList from "../components/GestureRuleList.vue";
import { useGestureEditorStore } from "../composables/gestureEditorStore";

const editor = useGestureEditorStore();
const scopeKind = "global";
editor.setActiveScope(scopeKind);
</script>

<template>
  <AppShell
    title="全局规则"
    description="全局规则不需要左侧列表，直接编辑整张表。"
    layout-class="page-shell__grid--single"
  >
    <template #right>
      <section class="rules-panel">
        <div class="rules-panel__head">
          <div>
            <h3>全局</h3>
            <p>名称、手势、命令三列编辑，删除放在每行右侧。</p>
          </div>
          <button type="button" class="primary-button" @click="editor.openAddRule(scopeKind)">新增规则</button>
        </div>

        <div v-if="editor.globalRules.length === 0" class="empty-state empty-state--large">
          当前没有全局规则。
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
    margin-bottom: 6px;
  }
}
</style>
