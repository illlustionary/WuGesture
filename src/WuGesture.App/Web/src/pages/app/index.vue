<script setup>
import AppShell from '../../components/AppShell.vue'
import GestureRuleList from '../../components/GestureRuleList.vue'
import IconActionButton from '../../components/IconActionButton.vue'
import ScopeCreateDialog from '../../components/ScopeCreateDialog.vue'
import ScopeSidebar from '../../components/ScopeSidebar.vue'
import RulesSection from '../../components/rules/RulesSection.vue'
import ScopeListItem from '../../components/scope/ScopeListItem.vue'
import { useGestureEditorStore } from '../../composables/gestureEditorStore'
import { useAppPage } from './composables/useAppPage'

const editor = useGestureEditorStore()
const {
  scopeKind,
  appRenameDialogOpen,
  appRenameDraft,
  getAppFallbackGlyph,
  deleteAppItem,
  openAppRenameDialog,
  closeAppRenameDialog,
  confirmAppRenameDialog
} = useAppPage(editor)
</script>

<template>
  <AppShell
    title="程序规则"
    description="为每个程序单独设置独有的手势"
    layout-class="page-shell__grid--split page-shell__grid--editor"
  >
    <template #left>
      <ScopeSidebar title="程序名称">
        <template #actions>
          <IconActionButton
            icon="add"
            label="添加程序"
            class="secondary-button"
            @click="editor.openApplicationPicker('', scopeKind)"
          />
        </template>

        <div
          v-if="editor.appItems.length > 0"
          class="list-stack"
        >
          <ScopeListItem
            v-for="item in editor.appItems"
            :key="item.name"
            :item="item"
            :active="item.name === editor.getSelectedName(scopeKind)"
            :label="item.displayName || item.name"
            :icon="item.icon"
            :fallback-glyph="getAppFallbackGlyph(item)"
            delete-label="删除程序"
            icon-mode="app"
            @select="editor.selectScope(scopeKind, $event.name)"
            @rename="openAppRenameDialog"
            @delete="deleteAppItem($event.name)"
          />
        </div>
        <div
          v-else
          class="empty-state"
        >
          还没有程序，先新增一个。
        </div>
      </ScopeSidebar>
    </template>

    <template #right>
      <section class="rules-panel rules-panel--app-only">
        <RulesSection
          title="手势列表"
          description="这里只维护当前程序的规则。"
          flex
        >
          <template #actions>
            <IconActionButton
              icon="add"
              label="添加手势"
              class="primary-button"
              @click="editor.openAddRule(scopeKind)"
            />
          </template>

          <GestureRuleList
            :rules="editor.getRulesForScope(scopeKind)"
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

  <ScopeCreateDialog
    v-model="appRenameDraft"
    :open="appRenameDialogOpen"
    title="重命名程序"
    description="输入新的程序名称，失焦或按回车后保存。"
    label="程序名称"
    placeholder="输入程序名称"
    @close="closeAppRenameDialog"
    @confirm="confirmAppRenameDialog"
  />
</template>

<style scoped lang="scss">
.rules-panel {
  display: flex;
  flex-direction: column;
  gap: 18px;
  min-width: 0;

  &--app-only {
    min-height: 100%;
  }
}
</style>
