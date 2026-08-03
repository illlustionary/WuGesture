<script setup>
import AppShell from '@/components/AppShell.vue'
import GestureRuleList from '@/components/GestureRuleList.vue'
import IconActionButton from '@/components/IconActionButton.vue'
import RulesSection from '@/components/rules/RulesSection.vue'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import { SCOPE_KINDS } from '@/constants/gestureEditorOptions'

const rulesStore = useGestureEditorContext()
const scopeKind = SCOPE_KINDS.global
rulesStore.setActiveScope(scopeKind)
</script>

<template>
  <AppShell layout-class="page-shell__grid--single page-shell__grid--editor">
    <template #right>
      <section class="rules-panel">
        <RulesSection
          title="手势列表"
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
  gap: 0;
  min-height: 100%;
  min-width: 0;
}
</style>
