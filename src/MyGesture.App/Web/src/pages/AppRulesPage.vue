<script setup>
import AppShell from '../components/AppShell.vue'
import IconActionButton from '../components/IconActionButton.vue'
import GestureRuleList from '../components/GestureRuleList.vue'
import ScopeSidebar from '../components/ScopeSidebar.vue'
import { useGestureEditorStore } from '../composables/gestureEditorStore'

const editor = useGestureEditorStore()
const scopeKind = 'app'
editor.setActiveScope(scopeKind)

function getAppFallbackGlyph(item) {
  const text = String(item?.displayName ?? item?.name ?? '').trim()
  if (!text) {
    return '?'
  }

  return text.slice(0, 1).toUpperCase()
}

function deleteAppItem(name) {
  editor.selectScope(scopeKind, name)
  editor.deleteSelectedScope(scopeKind)
}
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
        description="选择一个程序，或点击右侧 + 新增。"
      >
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
          class="scope-list"
        >
          <div
            v-for="item in editor.appItems"
            :key="item.name"
            class="scope-item"
            :class="{ active: item.name === editor.getSelectedName(scopeKind) }"
            role="button"
            tabindex="0"
            @click="editor.selectScope(scopeKind, item.name)"
            @keydown.enter.prevent="editor.selectScope(scopeKind, item.name)"
            @keydown.space.prevent="editor.selectScope(scopeKind, item.name)"
          >
            <span class="scope-item__accent" aria-hidden="true" />
            <span class="scope-item__main">
              <img
                v-if="item.icon"
                class="app-icon app-icon--small scope-item__icon"
                :src="item.icon"
                alt=""
              />
              <span
                v-else
                class="scope-item__icon scope-item__icon--fallback"
                aria-hidden="true"
              >
                {{ getAppFallbackGlyph(item) }}
              </span>
              <span>{{ item.displayName || item.name }}</span>
            </span>
            <span class="scope-item__meta">
              <small class="scope-item__count">{{ item.count }} 条</small>
              <IconActionButton
                icon="delete"
                label="删除程序"
                class="scope-item__delete"
                @click.stop="deleteAppItem(item.name)"
              />
            </span>
          </div>
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
</template>

<style scoped lang="scss">
.scope-list {
  display: grid;
  gap: 8px;
  flex: 1 1 auto;
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
    background: linear-gradient(180deg, rgba(231, 241, 255, 1), rgba(241, 247, 255, 1));
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

  &__icon--fallback {
    font-weight: 700;
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

  .scope-item {
    align-items: flex-start;
    flex-wrap: wrap;
  }

  .scope-item__meta {
    margin-left: auto;
  }
}
</style>
