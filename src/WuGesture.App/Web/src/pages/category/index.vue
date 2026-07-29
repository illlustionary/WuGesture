<script setup>
import AppShell from '@/components/AppShell.vue'
import GestureRuleList from '@/components/GestureRuleList.vue'
import IconActionButton from '@/components/IconActionButton.vue'
import ScopeCreateDialog from '@/components/ScopeCreateDialog.vue'
import ScopeSidebar from '@/components/ScopeSidebar.vue'
import ApplicationListItem from '@/components/applications/ApplicationListItem.vue'
import RulesSection from '@/components/rules/RulesSection.vue'
import ScopeListItem from '@/components/scope/ScopeListItem.vue'
import { useGestureRulesStore } from '@/gestureEditor/stores/useGestureRulesStore'
import { useCategoryPage } from '@/pages/category/composables/useCategoryPage'

const rulesStore = useGestureRulesStore()
const {
  scopeKind,
  categoryDraft,
  categoryDialogOpen,
  categoryRenameDraft,
  categoryRenameDialogOpen,
  openCategoryDialog,
  closeCategoryDialog,
  confirmCategoryDialog,
  openCategoryRenameDialog,
  closeCategoryRenameDialog,
  confirmCategoryRenameDialog,
  getCategoryIcon,
  deleteCategoryItem
} = useCategoryPage(rulesStore)
</script>

<template>
  <AppShell
    title="手势分类"
    description="创建分类来为一组应用程序共享同一套手势设置。"
    layout-class="page-shell__grid--split page-shell__grid--editor"
  >
    <template #left>
      <ScopeSidebar title="分类">
        <template #actions>
          <IconActionButton
            icon="add"
            label="新增分类"
            class="secondary-button"
            @click="openCategoryDialog"
          />
        </template>

        <div
          v-if="rulesStore.categoryItems.length > 0"
          class="list-stack"
        >
          <ScopeListItem
            v-for="item in rulesStore.categoryItems"
            :key="item.name"
            :item="item"
            :active="item.name === rulesStore.getSelectedName(scopeKind)"
            :label="item.name"
            :icon-component="getCategoryIcon(item.name)"
            delete-label="删除分类"
            icon-mode="category"
            @select="rulesStore.selectScope(scopeKind, $event.name)"
            @rename="openCategoryRenameDialog"
            @delete="deleteCategoryItem($event.name)"
          />
        </div>
        <div
          v-else
          class="empty-state"
        >
          还没有分类，先新增一个。
        </div>
      </ScopeSidebar>
    </template>

    <template #right>
      <section class="rules-panel">
        <RulesSection
          title="程序"
          description="同一程序在多个分类有相同手势，后面的分类手势会被覆盖。"
          compact
        >
          <template #actions>
            <IconActionButton
              icon="add"
              label="添加程序"
              class="secondary-button"
              @click="
                rulesStore.openApplicationPicker(
                  rulesStore.getSelectedName(scopeKind)
                )
              "
            />
          </template>

          <div
            v-if="rulesStore.getApplicationsForCategory().length === 0"
            class="empty-state empty-state--compact"
          >
            当前分类还没有关联程序。
          </div>
          <div
            v-else
            class="list-stack"
          >
            <ApplicationListItem
              v-for="app in rulesStore.getApplicationsForCategory()"
              :key="app.name"
              :app="app"
              @remove="rulesStore.removeAppFromCategory($event.name)"
            />
          </div>
        </RulesSection>

        <RulesSection
          title="手势列表"
          description="分类级规则会在这里统一维护。"
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
    v-model="categoryDraft"
    :open="categoryDialogOpen"
    title="新增分类"
    description="输入一个分类名称，创建后会出现在左侧列表。"
    label="分类名称"
    placeholder="例如：浏览器"
    @close="closeCategoryDialog"
    @confirm="confirmCategoryDialog"
  />

  <ScopeCreateDialog
    v-model="categoryRenameDraft"
    :open="categoryRenameDialogOpen"
    title="重命名分类"
    description="输入新的分类名称，失焦或按回车后保存。"
    label="分类名称"
    placeholder="输入分类名称"
    @close="closeCategoryRenameDialog"
    @confirm="confirmCategoryRenameDialog"
  />
</template>

<style scoped lang="scss">
.rules-panel {
  display: flex;
  flex-direction: column;
  gap: 18px;
  min-width: 0;
  min-height: 100%;
}
</style>
