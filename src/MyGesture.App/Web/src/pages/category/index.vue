<script setup>
import { ref } from 'vue'
import AppShell from '../../components/AppShell.vue'
import GestureRuleList from '../../components/GestureRuleList.vue'
import IconActionButton from '../../components/IconActionButton.vue'
import ScopeCreateDialog from '../../components/ScopeCreateDialog.vue'
import ScopeSidebar from '../../components/ScopeSidebar.vue'
import ApplicationListItem from '../../components/applications/ApplicationListItem.vue'
import RulesSection from '../../components/rules/RulesSection.vue'
import ScopeListItem from '../../components/scope/ScopeListItem.vue'
import { useGestureEditorStore } from '../../composables/gestureEditorStore'

const editor = useGestureEditorStore()
const scopeKind = 'category'
editor.setActiveScope(scopeKind)
const categoryDraft = ref('')
const categoryDialogOpen = ref(false)
const categoryRenameDraft = ref('')
const categoryRenameDialogOpen = ref(false)
const categoryRenameSource = ref('')

function openCategoryDialog() {
  categoryDraft.value = ''
  categoryDialogOpen.value = true
}

function closeCategoryDialog() {
  categoryDialogOpen.value = false
}

function confirmCategoryDialog() {
  if (editor.createScopeTarget(scopeKind, categoryDraft.value)) {
    closeCategoryDialog()
  }
}

function openCategoryRenameDialog(item) {
  const name = String(item?.name ?? '').trim()
  if (!name) {
    return
  }

  editor.selectScope(scopeKind, name)
  categoryRenameSource.value = name
  categoryRenameDraft.value = name
  categoryRenameDialogOpen.value = true
}

function closeCategoryRenameDialog() {
  categoryRenameDialogOpen.value = false
  categoryRenameDraft.value = ''
  categoryRenameSource.value = ''
}

function confirmCategoryRenameDialog() {
  const nextName = String(categoryRenameDraft.value ?? '').trim()
  if (!nextName) {
    return
  }

  if (nextName === categoryRenameSource.value) {
    closeCategoryRenameDialog()
    return
  }

  if (editor.renameSelectedScope(scopeKind, nextName, categoryRenameSource.value)) {
    closeCategoryRenameDialog()
  }
}

function getCategoryGlyph(name) {
  const value = String(name ?? '')
    .trim()
    .toLowerCase()
  if (!value) {
    return '◌'
  }

  if (
    value.includes('浏览') ||
    value.includes('browser') ||
    value.includes('网页')
  ) {
    return '🌐'
  }
  if (value.includes('办公') || value.includes('office')) {
    return '💼'
  }
  if (
    value.includes('开发') ||
    value.includes('dev') ||
    value.includes('编程')
  ) {
    return '🛠'
  }
  if (value.includes('设计') || value.includes('创作')) {
    return '✦'
  }
  if (
    value.includes('媒体') ||
    value.includes('音乐') ||
    value.includes('视频')
  ) {
    return '🎬'
  }

  return '📁'
}

function deleteCategoryItem(name) {
  editor.selectScope(scopeKind, name)
  editor.deleteSelectedScope(scopeKind)
}
</script>

<template>
  <AppShell
    title="手势分类"
    description="创建分类来为一组应用程序共享同一套手势设置。"
    layout-class="page-shell__grid--split page-shell__grid--editor"
  >
    <template #left>
      <ScopeSidebar
        title="分类名称"
        description="选择一个分类，或点击右侧 + 新增。"
      >
        <template #actions>
          <IconActionButton
            icon="add"
            label="新增分类"
            class="secondary-button"
            @click="openCategoryDialog"
          />
        </template>

        <div
          v-if="editor.categoryItems.length > 0"
          class="list-stack"
        >
          <ScopeListItem
            v-for="item in editor.categoryItems"
            :key="item.name"
            :item="item"
            :active="item.name === editor.getSelectedName(scopeKind)"
            :label="item.name"
            :fallback-glyph="getCategoryGlyph(item.name)"
            delete-label="删除分类"
            icon-mode="category"
            @select="editor.selectScope(scopeKind, $event.name)"
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
          description="当前分类关联的程序会从这里管理。"
          compact
        >
          <template #actions>
            <IconActionButton
              icon="add"
              label="添加程序"
              class="secondary-button"
              @click="
                editor.openApplicationPicker(
                  editor.getSelectedName(scopeKind)
                )
              "
            />
          </template>

          <div
            v-if="editor.getApplicationsForCategory().length === 0"
            class="empty-state empty-state--compact"
          >
            当前分类还没有关联程序。
          </div>
          <div
            v-else
            class="list-stack"
          >
            <ApplicationListItem
              v-for="app in editor.getApplicationsForCategory()"
              :key="app.name"
              :app="app"
              @remove="editor.removeAppFromCategory($event.name)"
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
