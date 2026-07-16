<script setup>
import { computed } from 'vue'
import AppShell from '@/components/AppShell.vue'
import GestureRuleList from '@/components/GestureRuleList.vue'
import IconActionButton from '@/components/IconActionButton.vue'
import ScopeCreateDialog from '@/components/ScopeCreateDialog.vue'
import ScopeSidebar from '@/components/ScopeSidebar.vue'
import RulesSection from '@/components/rules/RulesSection.vue'
import ScopeListItem from '@/components/scope/ScopeListItem.vue'
import { VueDraggable } from 'vue-draggable-plus'
import { useGestureRulesStore } from '@/gestureEditor/stores/useGestureRulesStore'
import { useAppPage } from '@/pages/app/composables/useAppPage'

const rulesStore = useGestureRulesStore()
const orderedCategories = computed({
  get() {
    return rulesStore.getCategoriesForApplication()
  },
  set(categories) {
    rulesStore.setApplicationCategories(
      rulesStore.getSelectedName('app'),
      categories
    )
  }
})
const {
  scopeKind,
  appRenameDialogOpen,
  appRenameDraft,
  getAppFallbackGlyph,
  deleteAppItem,
  openAppRenameDialog,
  closeAppRenameDialog,
  confirmAppRenameDialog
} = useAppPage(rulesStore)
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
            @click="rulesStore.openApplicationPicker('', scopeKind)"
          />
        </template>

        <div
          v-if="rulesStore.appItems.length > 0"
          class="list-stack"
        >
          <ScopeListItem
            v-for="item in rulesStore.appItems"
            :key="item.name"
            :item="item"
            :active="item.name === rulesStore.getSelectedName(scopeKind)"
            :label="item.displayName || item.name"
            :icon="item.icon"
            :fallback-glyph="getAppFallbackGlyph(item)"
            delete-label="删除程序"
            icon-mode="app"
            @select="rulesStore.selectScope(scopeKind, $event.name)"
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
          title="关联分类"
          description="越靠下优先级越高；同一手势冲突时，后面的分类会覆盖前面的分类。"
          compact
        >
          <div
            v-if="orderedCategories.length === 0"
            class="empty-state empty-state--compact"
          >
            当前程序还没有关联分类。
          </div>
          <VueDraggable
            v-else
            v-model="orderedCategories"
            class="category-order-list"
            :animation="150"
            ghost-class="category-order-item--ghost"
          >
            <div
              v-for="(category, index) in orderedCategories"
              :key="category"
              class="category-order-item"
            >
              <span
                class="category-order-item__drag-handle"
                title="拖动排序"
                aria-hidden="true"
              >
                ⠿
              </span>
              <span class="category-order-item__rank">{{ index + 1 }}</span>
              <span class="category-order-item__name">{{ category }}</span>
              <span class="category-order-item__actions">
                <button
                  type="button"
                  class="category-order-item__button"
                  :disabled="index === 0"
                  aria-label="上移分类"
                  @click="rulesStore.moveApplicationCategory(rulesStore.getSelectedName(scopeKind), category, 'up')"
                >
                  上移
                </button>
                <button
                  type="button"
                  class="category-order-item__button"
                  :disabled="index === orderedCategories.length - 1"
                  aria-label="下移分类"
                  @click="rulesStore.moveApplicationCategory(rulesStore.getSelectedName(scopeKind), category, 'down')"
                >
                  下移
                </button>
              </span>
            </div>
          </VueDraggable>
        </RulesSection>

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
              @click="rulesStore.openAddRule(scopeKind)"
            />
          </template>

          <GestureRuleList
            :rules="rulesStore.getRulesForScope(scopeKind)"
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

.category-order-list {
  display: grid;
  gap: 8px;
}

.category-order-item {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 50px;
  padding: 9px 12px;
  border: 1px solid var(--border-subtle);
  border-radius: 14px;
  background: var(--interactive-bg);

  &--ghost {
    opacity: 0.45;
  }

  &__drag-handle {
    cursor: grab;
    color: var(--text-subtle);
    font-size: 18px;
    line-height: 1;
    user-select: none;

    &:active {
      cursor: grabbing;
    }
  }

  &__rank {
    display: grid;
    width: 24px;
    height: 24px;
    place-items: center;
    border-radius: 999px;
    background: var(--interactive-icon-bg);
    color: var(--accent-strong);
    font-size: 12px;
    font-weight: 700;
  }

  &__name {
    min-width: 0;
    flex: 1;
    overflow: hidden;
    font-weight: 600;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__actions {
    display: inline-flex;
    gap: 6px;
  }

  &__button {
    padding: 5px 8px;
    border: 1px solid var(--border-subtle);
    border-radius: 8px;
    background: transparent;
    color: var(--text-subtle);
    font-size: 12px;

    &:hover:not(:disabled) {
      border-color: var(--border-strong);
      color: var(--text);
    }

    &:disabled {
      cursor: not-allowed;
      opacity: 0.4;
    }
  }
}
</style>
