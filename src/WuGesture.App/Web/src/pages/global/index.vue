<script setup>
import AppShell from '@/components/layout/AppShell.vue'
import GestureRuleList from '@/components/gesture/GestureRuleList.vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'
import RulesSection from '@/components/rules/RulesSection.vue'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import { SCOPE_KINDS } from '@/constants/gestureEditorOptions'

const rulesStore = useGestureEditorContext()
const scopeKind = SCOPE_KINDS.global
rulesStore.setActiveScope(scopeKind)
</script>

<template>
  <AppShell
    title="全局手势"
    description="对所有程序生效的手势规则"
    layout-class="page-shell__grid--single page-shell__grid--editor"
  >
    <template #actions>
      <IconActionButton
        icon="add"
        label="添加规则"
        class="primary-button"
        @click="rulesStore.openAddRule(scopeKind)"
      />
    </template>

    <template #right>
      <section class="rules-panel">
        <RulesSection
          title="手势列表"
          flex
        >
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
  gap: 0;
  min-height: 100%;
  min-width: 0;
}
</style>
