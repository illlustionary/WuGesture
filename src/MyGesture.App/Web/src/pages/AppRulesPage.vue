<script setup>
import AppShell from '../components/AppShell.vue'
import GestureRuleList from '../components/GestureRuleList.vue'
import ScopeSidebar from '../components/ScopeSidebar.vue'
import { useGestureEditorStore } from '../composables/gestureEditorStore'

const editor = useGestureEditorStore()
const scopeKind = 'app'
editor.setActiveScope(scopeKind)
</script>

<template>
  <AppShell
    title="程序规则"
    description="左边选择具体程序，右边只保留手势列表。"
    layout-class="page-shell__grid--split page-shell__grid--editor"
  >
    <template #left>
      <ScopeSidebar
        title="程序名称"
        description="选择一个程序，或在底部新增。"
      >
        <div
          v-if="editor.appItems.length > 0"
          class="scope-list"
        >
          <button
            v-for="item in editor.appItems"
            :key="item.name"
            type="button"
            class="scope-item"
            :class="{ active: item.name === editor.getSelectedName(scopeKind) }"
            @click="editor.selectScope(scopeKind, item.name)"
          >
            <span class="scope-item__main">
              <img
                v-if="item.icon"
                class="app-icon app-icon--small"
                :src="item.icon"
                alt=""
              />
              <span>{{ item.displayName || item.name }}</span>
            </span>
            <small>{{ item.count }} 条</small>
          </button>
        </div>
        <div
          v-else
          class="empty-state"
        >
          还没有程序，先新增一个。
        </div>

        <div class="scope-panel__footer">
          <button
            type="button"
            class="primary-button"
            @click="editor.openApplicationPicker('', scopeKind)"
          >
            添加程序
          </button>
          <button
            type="button"
            class="ghost-button"
            @click="editor.deleteSelectedScope(scopeKind)"
          >
            删除当前程序
          </button>
        </div>
      </ScopeSidebar>
    </template>

    <template #right>
      <section
        class="rules-panel rules-panel--stacked rules-panel--editor rules-panel--app-only"
      >
        <section class="rules-panel__section rules-panel__section--flex">
          <div class="rules-panel__head">
            <div>
              <h3>手势列表</h3>
              <p>这里只维护当前程序的规则。</p>
            </div>
            <div class="rules-panel__actions">
              <button
                type="button"
                class="primary-button"
                @click="editor.openAddRule(scopeKind)"
              >
                添加手势
              </button>
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
  flex: 1 1 auto;
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
  border-bottom: 1px solid rgba(20, 30, 40, 0.07);
  text-align: left;
  background: transparent;

  &:last-child {
    border-bottom: 0;
  }

  &.active {
    background: linear-gradient(
      90deg,
      rgba(29, 81, 109, 0.12),
      rgba(29, 81, 109, 0.03)
    );
  }

  &:hover {
    background: rgba(29, 81, 109, 0.08);
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

@media (max-width: 720px) {
  .rules-panel__section {
    padding: 16px;
  }
}
</style>
