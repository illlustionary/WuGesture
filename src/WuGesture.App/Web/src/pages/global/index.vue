<script setup>
import AppShell from '@/components/AppShell.vue'
import GestureRuleList from '@/components/GestureRuleList.vue'
import IconActionButton from '@/components/IconActionButton.vue'
import RulesSection from '@/components/rules/RulesSection.vue'
import ScopePriorityNotice from '@/components/ScopePriorityNotice.vue'
import { useGestureRulesStore } from '@/gestureEditor/stores/useGestureRulesStore'
import { SCOPE_KINDS } from '@/constants/gestureEditorOptions'

const rulesStore = useGestureRulesStore()
const scopeKind = SCOPE_KINDS.global
rulesStore.setActiveScope(scopeKind)
</script>

<template>
  <AppShell
    title="全局规则"
    description="全局规则对所有程序生效。"
    layout-class="page-shell__grid--single page-shell__grid--editor"
  >
    <template #right>
      <section class="rules-panel">
        <ScopePriorityNotice />
        <RulesSection
          title="手势列表"
          description="这里维护不依赖分类和程序的默认手势。"
          flex
        >
          <template #actions>
            <IconActionButton
              icon="add"
              label="添加规则"
              class="primary-button"
              @click="rulesStore.openAddRule(scopeKind)"
            />
          </template>

          <GestureRuleList
            :rules="rulesStore.globalRules"
            :get-gesture-mnemonic="rulesStore.getGestureMnemonic"
            :get-action-label="rulesStore.getActionLabel"
            @remove="rulesStore.removeRule"
            @edit="rulesStore.openEditRule"
            @rename="rulesStore.updateRuleActionName"
          />
        </RulesSection>
      </section>
    </template>
  </AppShell>
</template>

<style scoped lang="scss">
.rules-panel {
  display: flex;
  flex-direction: column;
  gap: 18px;
  min-height: 100%;
  min-width: 0;
}
</style>
