<script setup>
import { nextTick, ref } from 'vue'
import AppShell from '../components/AppShell.vue'
import IconActionButton from '../components/IconActionButton.vue'
import GestureRuleList from '../components/GestureRuleList.vue'
import ScopeSidebar from '../components/ScopeSidebar.vue'
import ScopeCreateDialog from '../components/ScopeCreateDialog.vue'
import { useGestureEditorStore } from '../composables/gestureEditorStore'

const editor = useGestureEditorStore()
const scopeKind = 'category'
editor.setActiveScope(scopeKind)
const categoryDraft = ref('')
const categoryDialogOpen = ref(false)
const editingCategoryName = ref('')
const editingCategoryDraft = ref('')
const editingCategoryInput = ref(null)

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

async function startCategoryRename(item) {
  if (!item || editingCategoryName.value === item.name) {
    return
  }

  editor.selectScope(scopeKind, item.name)
  editingCategoryName.value = item.name
  editingCategoryDraft.value = item.name
  await nextTick()
  editingCategoryInput.value?.focus?.()
  editingCategoryInput.value?.select?.()
}

function commitCategoryRename(item) {
  if (!item || editingCategoryName.value !== item.name) {
    return
  }

  editingCategoryName.value = ''
  editingCategoryInput.value = null
  editor.renameSelectedScope(scopeKind, editingCategoryDraft.value, item.name)
}

function setEditingCategoryInput(el) {
  editingCategoryInput.value = el
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
        description="选择一个分类，或在底部新增。"
      >
        <div
          v-if="editor.categoryItems.length > 0"
          class="scope-list"
        >
          <div
            v-for="item in editor.categoryItems"
            :key="item.name"
            class="scope-item"
            :class="{
              active: item.name === editor.getSelectedName(scopeKind),
              'is-editing': editingCategoryName === item.name
            }"
            role="button"
            tabindex="0"
            @click="editor.selectScope(scopeKind, item.name)"
            @dblclick="startCategoryRename(item)"
          >
            <span class="scope-item__main">
              <img
                v-if="item.icon"
                class="app-icon app-icon--small"
                :src="item.icon"
                alt=""
              />
              <input
                v-if="editingCategoryName === item.name"
                :ref="setEditingCategoryInput"
                v-model.trim="editingCategoryDraft"
                class="scope-input scope-item__edit"
                spellcheck="false"
                @blur="commitCategoryRename(item)"
                @keydown.enter.prevent="commitCategoryRename(item)"
              />
              <span
                v-else
                class="scope-item__name"
              >
                {{ item.name }}
              </span>
            </span>
            <small class="small">{{ item.count }} 条</small>
          </div>
        </div>
        <div
          v-else
          class="empty-state"
        >
          还没有分类，先新增一个。
        </div>

        <div class="scope-panel__footer">
          <IconActionButton
            icon="add"
            label="新增分类"
            class="primary-button"
            @click="openCategoryDialog"
          />
          <IconActionButton
            icon="delete"
            label="删除当前分类"
            class="ghost-button danger-button"
            @click="editor.deleteSelectedScope(scopeKind)"
          />
        </div>
      </ScopeSidebar>
    </template>

    <template #right>
      <section class="rules-panel rules-panel--stacked rules-panel--editor">
        <section class="rules-panel__section rules-panel__section--compact">
          <div class="rules-panel__head">
            <div>
              <h3>程序</h3>
              <p>当前分类关联的程序会从这里管理。</p>
            </div>
            <div class="rules-panel__actions">
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
            </div>
          </div>

          <div
            v-if="editor.getApplicationsForCategory().length === 0"
            class="empty-state empty-state--compact"
          >
            当前分类还没有关联程序。
          </div>
          <div
            v-else
            class="app-list"
          >
            <div
              v-for="app in editor.getApplicationsForCategory()"
              :key="app.name"
              class="app-list__item"
            >
              <span class="app-list__name">
                <img
                  v-if="app.icon"
                  class="app-icon app-icon--small"
                  :src="app.icon"
                  alt=""
                />
                <span>{{ app.displayName || app.name }}</span>
              </span>
              <span class="app-list__path">{{ app.path || '未设置路径' }}</span>
              <IconActionButton
                icon="delete"
                label="移除"
                class="ghost-button danger-button"
                @click="editor.removeAppFromCategory(app.name)"
              />
            </div>
          </div>
        </section>

        <section class="rules-panel__section rules-panel__section--flex">
          <div class="rules-panel__head">
            <div>
              <h3>手势列表</h3>
              <p>分类级规则会在这里统一维护。</p>
            </div>
            <div class="rules-panel__actions">
              <IconActionButton
                icon="add"
                label="添加手势"
                class="primary-button"
                @click="editor.openAddRule(scopeKind)"
              />
            </div>
          </div>

          <GestureRuleList
            :rules="editor.getRulesForScope(scopeKind)"
            :get-gesture-mnemonic="editor.getGestureMnemonic"
            :get-action-label="editor.getActionLabel"
            @remove="editor.removeRule"
            @edit="editor.openEditRule"
            @rename="editor.updateRuleActionName"
          />
        </section>
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
    confirm-text="新增分类"
    @close="closeCategoryDialog"
    @confirm="confirmCategoryDialog"
  />
</template>

<style scoped lang="scss">
.scope-list {
  display: grid;
  gap: 0;
  overflow: hidden;
  border: 1px solid var(--border);
  border-radius: 22px;
  background: rgba(255, 255, 255, 0.9);
  box-shadow: 0 14px 34px rgba(18, 30, 42, 0.08);
}

.scope-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
  min-height: 52px;
  padding: 12px 14px;
  border: 0;
  text-align: left;
  background: transparent;
  cursor: pointer;
  user-select: none;

  &:last-child {
    border-bottom: 0;
  }

  &.active {
    background: transparent;
  }

  small {
    color: var(--muted);
  }

  &__main {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    min-width: 0;
  }

  &__name {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__edit {
    min-width: 0;
    flex: 1;
    margin-left: -2px;
    background: rgba(255, 255, 255, 0.96);
  }
  .small {
    white-space: nowrap;
  }

  &.is-editing {
    cursor: text;
    user-select: text;
  }
}

.scope-panel__footer {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: auto;
  padding-top: 4px;
}

.rules-panel {
  display: flex;
  flex-direction: column;
  gap: 18px;
  min-width: 0;

  &--stacked,
  &--app-only {
    min-height: 100%;
  }

  &--editor {
    padding: 0;
  }

  h3 {
    font-size: 16px;
    font-weight: 700;
  }

  p {
    color: var(--muted);
    font-size: 13px;
  }

  &__section {
    min-width: 0;
    padding: 22px;
    border: 1px solid var(--border);
    border-radius: 26px;
    background: rgba(255, 255, 255, 0.68);
    box-shadow: var(--shadow-soft);
    backdrop-filter: blur(20px) saturate(1.12);

    &--compact {
      flex: 0 0 auto;
    }

    &--flex {
      display: flex;
      flex-direction: column;
      gap: 14px;
      flex: 1 1 auto;
    }

    .empty-state {
      margin-top: 2px;
    }
  }

  &__head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 10px;
    margin-bottom: 4px;
  }

  &__actions {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
  }
}

.app-list {
  display: grid;
  overflow: hidden;
  border: 1px solid var(--border);
  border-radius: 22px;
  background: rgba(255, 255, 255, 0.9);
  box-shadow: 0 14px 34px rgba(18, 30, 42, 0.08);

  &__item {
    display: grid;
    grid-template-columns: 1fr 1.4fr auto;
    gap: 12px;
    min-height: 52px;
    padding: 10px 14px;
    align-items: center;
    cursor: pointer;

    + .app-list__item {
      border-top: 0;
    }
  }

  &__path {
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
    color: var(--muted);
  }

  &__name {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    min-width: 0;
  }

  &__item:hover {
    background: transparent;
  }
}

@media (max-width: 720px) {
  .rules-panel__section {
    padding: 16px;
  }

  .app-list__item {
    grid-template-columns: 1fr;
  }
}
</style>
