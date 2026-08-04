<script setup>
import { computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import { VueDraggable } from 'vue-draggable-plus'
import AppShell from '@/components/layout/AppShell.vue'
import GestureRuleList from '@/components/gesture/GestureRuleList.vue'
import RulesSection from '@/components/rules/RulesSection.vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import { useGestureEditorNavigation } from '@/gestureEditor/modules/useGestureEditorNavigation'
import { SCOPE_KINDS } from '@/constants/gestureEditorOptions'

const editor = useGestureEditorContext()
const route = useRoute()
const navigation = useGestureEditorNavigation()
const scopeKind = SCOPE_KINDS.app
const selectedName = computed(() => editor.getSelectedName(scopeKind))

editor.setActiveScope(scopeKind)

watch(
  () => [String(route.params.name ?? ''), editor.appItems.map(item => item.name).join('\u0000')],
  ([requestedName]) => {
    const names = editor.appItems.map(item => item.name)
    if (requestedName && names.includes(requestedName)) {
      editor.selectScope(scopeKind, requestedName)
      return
    }

    if (!requestedName && names.length > 0) {
      navigation.replaceScope(scopeKind, names[0])
      return
    }

    editor.selectScope(scopeKind, '')
  },
  { immediate: true }
)

const orderedCategories = computed({
  get() {
    return editor.getCategoriesForApplication()
  },
  set(categories) {
    editor.setApplicationCategories(selectedName.value, categories)
  }
})
</script>

<template>
  <AppShell layout-class="page-shell__grid--single page-shell__grid--editor">
    <template #right>
      <section
        v-if="selectedName"
        class="rules-panel"
      >
        <RulesSection
          title="关联分类"
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
                >⠿</span
              >
              <span class="category-order-item__rank">{{ index + 1 }}</span>
              <span class="category-order-item__name">{{ category }}</span>
              <span class="category-order-item__actions">
                <button
                  type="button"
                  class="category-order-item__button"
                  :disabled="index === 0"
                  aria-label="上移分类"
                  @click="editor.moveApplicationCategory(selectedName, category, 'up')"
                >
                  上移
                </button>
                <button
                  type="button"
                  class="category-order-item__button"
                  :disabled="index === orderedCategories.length - 1"
                  aria-label="下移分类"
                  @click="editor.moveApplicationCategory(selectedName, category, 'down')"
                >
                  下移
                </button>
              </span>
            </div>
          </VueDraggable>
        </RulesSection>

        <RulesSection
          title="手势列表"
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
      <div
        v-else
        class="scope-empty-page"
      >
        <h2>程序</h2>
        <p>还没有程序，请从左侧菜单新增一个程序。</p>
      </div>
    </template>
  </AppShell>
</template>

<style scoped lang="scss">
.rules-panel {
  display: flex;
  flex-direction: column;
  gap: 0;
  min-width: 0;
  min-height: 100%;
}

.scope-empty-page {
  display: grid;
  align-content: center;
  justify-items: center;
  min-height: 100%;
  gap: 8px;
  color: var(--muted);

  h2 {
    color: var(--text);
    font-size: 20px;
  }
}

.category-order-list {
  display: grid;
  gap: 0;
  border-radius: 10px;
  overflow: hidden;
  border: 1px solid var(--border);
}

.category-order-item {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 50px;
  padding: 9px 12px;
  border-radius: 0;
  background: var(--interactive-bg);
  &--ghost {
    opacity: 0.45;
  }

  & + & {
    border-top-width: 0;
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
    text-overflow: ellipsis;
    white-space: nowrap;
    font-weight: 600;
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
