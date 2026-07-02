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

function hasApplicationPath(app) {
  return Boolean(String(app?.path ?? '').trim())
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
            @keydown.enter.prevent="editor.selectScope(scopeKind, item.name)"
            @keydown.space.prevent="editor.selectScope(scopeKind, item.name)"
          >
            <span
              class="scope-item__accent"
              aria-hidden="true"
            />
            <span class="scope-item__main">
              <span
                class="scope-item__icon"
                aria-hidden="true"
              >
                {{ getCategoryGlyph(item.name) }}
              </span>
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
            <span class="scope-item__meta">
              <small class="scope-item__count">{{ item.count }} 条</small>
              <IconActionButton
                icon="delete"
                label="删除分类"
                class="scope-item__delete"
                @click.stop="deleteCategoryItem(item.name)"
              />
            </span>
          </div>
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
              <span
                class="app-list__icon"
                :class="{ 'app-list__icon--missing': !hasApplicationPath(app) }"
                aria-hidden="true"
              >
                <img
                  v-if="app.icon"
                  class="app-icon app-icon--small"
                  :src="app.icon"
                  alt=""
                />
                <span
                  v-else
                  class="app-list__fallback"
                >
                  {{
                    (app.displayName || app.name || '?')
                      .slice(0, 1)
                      .toUpperCase()
                  }}
                </span>
              </span>
              <span class="app-list__content">
                <span class="app-list__name">{{
                  app.displayName || app.name
                }}</span>
                <span class="app-list__path">{{
                  app.path || '未设置路径'
                }}</span>
              </span>
              <span class="app-list__status">
                <IconActionButton
                  icon="delete"
                  label="移除"
                  class="app-list__delete"
                  @click.stop="editor.removeAppFromCategory(app.name)"
                />
              </span>
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
  gap: 8px;
}

.scope-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
  min-height: 58px;
  padding: 10px 12px 10px 14px;
  border: 0;
  border-radius: 18px;
  text-align: left;
  background: rgba(248, 251, 255, 0.96);
  border: 1px solid rgba(18, 30, 42, 0.08);
  box-shadow: 0 10px 24px rgba(18, 30, 42, 0.04);
  cursor: pointer;
  user-select: none;
  position: relative;
  overflow: hidden;
  transition:
    transform 120ms ease,
    background-color 120ms ease,
    border-color 120ms ease,
    box-shadow 120ms ease;

  &:hover,
  &:focus-visible {
    background: rgba(242, 247, 255, 1);
    border-color: rgba(0, 122, 255, 0.16);
    box-shadow: 0 14px 28px rgba(18, 30, 42, 0.06);
    transform: translateY(-1px);
  }

  &.active {
    background: linear-gradient(
      180deg,
      rgba(231, 241, 255, 1),
      rgba(241, 247, 255, 1)
    );
    border-color: rgba(0, 122, 255, 0.2);
    box-shadow: 0 14px 28px rgba(0, 122, 255, 0.08);
  }

  &.active .scope-item__accent {
    opacity: 1;
  }

  &__accent {
    position: absolute;
    inset: 0 auto 0 0;
    width: 4px;
    border-radius: 999px;
    background: linear-gradient(180deg, var(--accent), var(--accent-strong));
    opacity: 0;
  }

  &__main {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    min-width: 0;
    flex: 1 1 auto;
  }

  &__name {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-weight: 600;
  }

  &__edit {
    min-width: 0;
    flex: 1;
    margin-left: -2px;
    background: rgba(255, 255, 255, 0.98);
  }

  &__icon {
    display: grid;
    place-items: center;
    width: 28px;
    height: 28px;
    flex: 0 0 auto;
    border-radius: 10px;
    background: rgba(0, 122, 255, 0.1);
    color: var(--accent-strong);
    font-size: 15px;
    line-height: 1;
  }

  &__meta {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    flex: 0 0 auto;
  }

  &__count {
    display: inline-flex;
    align-items: center;
    min-height: 22px;
    padding: 0 8px;
    border-radius: 999px;
    background: rgba(102, 117, 137, 0.1);
    color: var(--muted);
    font-size: 12px;
    font-weight: 600;
    white-space: nowrap;
  }

  &__delete {
    opacity: 0;
    transform: scale(0.92);
    transition:
      opacity 120ms ease,
      transform 120ms ease;
  }

  &:hover &__delete,
  &:focus-within &__delete {
    opacity: 1;
    transform: scale(1);
  }

  &.is-editing {
    cursor: text;
    user-select: text;
  }
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
    background: rgba(255, 255, 255, 0.9);
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
  gap: 8px;

  &__item {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) auto;
    gap: 12px;
    min-height: 64px;
    padding: 12px 14px;
    align-items: center;
    border: 1px solid rgba(18, 30, 42, 0.08);
    border-radius: 18px;
    background: rgba(248, 251, 255, 0.96);
    box-shadow: 0 10px 24px rgba(18, 30, 42, 0.04);
    transition:
      transform 120ms ease,
      background-color 120ms ease,
      border-color 120ms ease,
      box-shadow 120ms ease;
  }

  &__item:hover {
    background: rgba(242, 247, 255, 1);
    border-color: rgba(0, 122, 255, 0.16);
    box-shadow: 0 14px 28px rgba(18, 30, 42, 0.06);
    transform: translateY(-1px);
  }

  &__icon {
    position: relative;
    display: grid;
    place-items: center;
    width: 32px;
    height: 32px;
    border-radius: 12px;
    background: rgba(0, 122, 255, 0.1);
    color: var(--accent-strong);
    overflow: hidden;

    &--missing::after {
      content: '!';
      position: absolute;
      top: -4px;
      right: -4px;
      display: grid;
      place-items: center;
      width: 14px;
      height: 14px;
      border-radius: 999px;
      background: #ffd54d;
      color: #7b5300;
      font-size: 10px;
      font-weight: 800;
      border: 1px solid rgba(255, 255, 255, 0.92);
      box-shadow: 0 4px 10px rgba(18, 30, 42, 0.12);
    }
  }

  &__fallback {
    font-size: 14px;
    font-weight: 700;
  }

  &__content {
    display: grid;
    min-width: 0;
    gap: 4px;
  }

  &__name {
    min-width: 0;
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
    font-weight: 600;
  }

  &__path {
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
    color: #8b95a3;
    font-size: 12px;
  }

  &__status {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    flex: 0 0 auto;
  }

  &__badge {
    display: inline-flex;
    align-items: center;
    min-height: 22px;
    padding: 0 8px;
    border-radius: 999px;
    background: rgba(102, 117, 137, 0.1);
    color: var(--muted);
    font-size: 12px;
    font-weight: 600;
    white-space: nowrap;
  }

  &__delete {
    opacity: 0;
    transform: scale(0.92);
    transition:
      opacity 120ms ease,
      transform 120ms ease;
  }

  &__item:hover &__delete,
  &__item:focus-within &__delete {
    opacity: 1;
    transform: scale(1);
  }
}

@media (max-width: 720px) {
  .rules-panel__section {
    padding: 16px;
  }

  .scope-item {
    align-items: flex-start;
    flex-wrap: wrap;
  }

  .scope-item__meta,
  .app-list__status {
    margin-left: auto;
    justify-content: flex-end;
  }

  .app-list__item {
    grid-template-columns: 1fr;
  }
}
</style>
