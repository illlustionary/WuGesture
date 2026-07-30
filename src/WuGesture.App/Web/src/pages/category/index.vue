<script setup>
import { computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import AppShell from '@/components/AppShell.vue'
import GestureRuleList from '@/components/GestureRuleList.vue'
import IconActionButton from '@/components/IconActionButton.vue'
import ApplicationListItem from '@/components/applications/ApplicationListItem.vue'
import RulesSection from '@/components/rules/RulesSection.vue'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import { useGestureEditorNavigation } from '@/gestureEditor/modules/useGestureEditorNavigation'
import { SCOPE_KINDS } from '@/constants/gestureEditorOptions'

const editor = useGestureEditorContext()
const route = useRoute()
const navigation = useGestureEditorNavigation()
const scopeKind = SCOPE_KINDS.category
const selectedName = computed(() => editor.getSelectedName(scopeKind))

editor.setActiveScope(scopeKind)

watch(
  () => [
    String(route.params.name ?? ''),
    editor.categoryItems.map(item => item.name).join('\u0000')
  ],
  ([requestedName]) => {
    const names = editor.categoryItems.map(item => item.name)
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

function addApplication() {
  if (selectedName.value) {
    editor.openApplicationPicker(selectedName.value, scopeKind)
  }
}
</script>

<template>
  <AppShell layout-class="page-shell__grid--single page-shell__grid--editor">
    <template #right>
      <section v-if="selectedName" class="rules-panel">
        <RulesSection title="程序" compact>
          <template #actions>
            <IconActionButton
              icon="add"
              label="添加程序"
              class="secondary-button"
              @click="addApplication"
            />
          </template>

          <div
            v-if="editor.getApplicationsForCategory().length === 0"
            class="empty-state empty-state--compact"
          >
            当前分类还没有关联程序。
          </div>
          <div v-else class="list-stack">
            <ApplicationListItem
              v-for="app in editor.getApplicationsForCategory()"
              :key="app.name"
              :app="app"
              @remove="editor.removeAppFromCategory($event.name)"
            />
          </div>
        </RulesSection>

        <RulesSection title="手势列表" flex>
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
      <div v-else class="scope-empty-page">
        <h2>分类</h2>
        <p>还没有分类，请从左侧菜单新增一个分类。</p>
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
</style>
