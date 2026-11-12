<script setup>
import { computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import { VueDraggable } from 'vue-draggable-plus'
import AppShell from '@/components/layout/AppShell.vue'
import GestureRuleList from '@/components/gesture/GestureRuleList.vue'
import RulesSection from '@/components/rules/RulesSection.vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'
import ApplicationListItem from '@/components/scope/ApplicationListItem.vue'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import { useGestureEditorNavigation } from '@/gestureEditor/modules/useGestureEditorNavigation'
import { SCOPE_KINDS } from '@/constants/gestureEditorOptions'

const editor = useGestureEditorContext()
const route = useRoute()
const navigation = useGestureEditorNavigation()
const scopeKind = SCOPE_KINDS.app
const selectedName = computed(() => editor.getSelectedName(scopeKind))
const selectedApplication = computed(() => editor.getApplication(selectedName.value))

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
  <AppShell
    :title="selectedApplication?.displayName || selectedName || '程序'"
    description="程序关联的分类与专属手势规则"
    layout-class="page-shell__grid--single page-shell__grid--editor"
  >
    <template #right>
      <section
        v-if="selectedName"
        class="rules-panel"
      >
        <RulesSection
          title="程序"
          compact
        >
          <ApplicationListItem
            v-if="selectedApplication"
            :app="selectedApplication"
            @open-path="editor.openApplicationFolder"
          />
        </RulesSection>

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
@use '../../styles/tokens/state' as *;

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
  gap: var(--md-sys-spacing-sm);
  color: var(--md-sys-color-on-surface-variant);

  h2 {
    color: var(--md-sys-color-on-surface);
    font-size: var(--md-sys-typescale-title-large-size);
    line-height: var(--md-sys-typescale-title-large-line-height);
  }
}

.category-order-list {
  display: grid;
  gap: 0;
  overflow: hidden;
  border: 1px solid var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-medium);
  background: var(--md-sys-color-surface-container-lowest);
}

.category-order-item {
  @include state-layer(var(--md-sys-color-on-surface));

  display: flex;
  align-items: center;
  gap: var(--md-sys-spacing-sm);
  min-height: var(--md-sys-size-row-single);
  padding: var(--md-sys-spacing-sm) var(--md-sys-spacing-md);
  background: transparent;

  &--ghost {
    opacity: var(--md-sys-state-disabled-content-opacity);
  }

  & + & {
    border-top: 1px solid var(--md-sys-color-outline-variant);
  }

  &__drag-handle {
    cursor: grab;
    color: var(--md-sys-color-on-surface-variant);
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
    border-radius: var(--md-sys-shape-corner-full);
    background: var(--md-sys-color-surface-container-high);
    color: var(--md-sys-color-primary);
    font-size: var(--md-sys-typescale-label-medium-size);
    font-weight: var(--md-sys-typescale-label-medium-weight);
  }

  &__name {
    min-width: 0;
    flex: 1;
    overflow: hidden;
    color: var(--md-sys-color-on-surface);
    font-size: var(--md-sys-typescale-body-medium-size);
    line-height: var(--md-sys-typescale-body-medium-line-height);
    font-weight: 500;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__actions {
    position: relative;
    z-index: 1;
    display: inline-flex;
    gap: var(--md-sys-spacing-xs);
  }

  &__button {
    padding: var(--md-sys-spacing-xs) var(--md-sys-spacing-sm);
    border: 1px solid var(--md-sys-color-outline-variant);
    border-radius: var(--md-sys-shape-corner-small);
    background: transparent;
    color: var(--md-sys-color-on-surface-variant);
    font-size: var(--md-sys-typescale-label-medium-size);
    line-height: var(--md-sys-typescale-label-medium-line-height);

    &:hover:not(:disabled) {
      border-color: var(--md-sys-color-outline);
      color: var(--md-sys-color-on-surface);
    }

    &:disabled {
      cursor: not-allowed;
      opacity: var(--md-sys-state-disabled-content-opacity);
    }
  }
}
</style>
