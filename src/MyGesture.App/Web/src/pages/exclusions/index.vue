<script setup>
import AppShell from '../../components/AppShell.vue'
import IconActionButton from '../../components/IconActionButton.vue'
import ToggleCheckbox from '../../components/ToggleCheckbox.vue'
import { useGestureEditorStore } from '../../composables/gestureEditorStore'

const editor = useGestureEditorStore()

function toggleDisableEdgeActions(application) {
  editor.updateExcludedApplication(application, {
    disableEdgeActions: Boolean(application.disableEdgeActions)
  })
}
</script>

<template>
  <AppShell
    title="排除项"
    description="命中的程序不执行鼠标手势，可单独禁用边缘操作。"
    layout-class="page-shell__grid--single"
  >
    <template #actions>
      <button
        type="button"
        class="primary-button"
        @click="editor.openExcludedApplicationPicker()"
      >
        添加程序
      </button>
    </template>

    <template #right>
      <section
        v-if="editor.state.uiSettings.appBehavior.excludedApplications.length"
        class="exclusion-list"
      >
        <article
          v-for="(application, index) in editor.state.uiSettings.appBehavior.excludedApplications"
          :key="application.path || application.name || index"
          class="exclusion-item"
        >
          <div class="exclusion-item__main">
            <strong>{{ application.displayName || application.name || application.path }}</strong>
            <span>{{ application.path || application.name }}</span>
          </div>
          <ToggleCheckbox
            v-model="application.disableEdgeActions"
            label="同时禁用边缘操作"
            @change="toggleDisableEdgeActions(application)"
          />
          <IconActionButton
            icon="delete"
            label="删除排除项"
            class="ghost-button"
            tone="danger"
            @click="editor.removeExcludedApplication(index)"
          />
        </article>
      </section>

      <p
        v-else
        class="exclusion-empty"
      >
        暂无排除项。
      </p>
    </template>
  </AppShell>
</template>

<style scoped lang="scss">
.exclusion-list {
  display: grid;
  gap: 10px;
}

.exclusion-item {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto;
  align-items: center;
  gap: 10px;
  padding: 12px;
  border: 1px solid var(--border-subtle);
  border-radius: 16px;
  background: var(--panel-inset);
}

.exclusion-item__main {
  display: grid;
  gap: 4px;
  min-width: 0;

  strong,
  span {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  strong {
    color: var(--text);
    font-size: 13px;
  }

  span {
    color: var(--muted);
    font-size: 12px;
  }
}

.exclusion-empty {
  margin: 0;
  padding: 18px;
  border: 1px dashed var(--border);
  border-radius: 16px;
  color: var(--muted);
  background: var(--panel-inset);
  font-size: 13px;
}

@media (max-width: 720px) {
  .exclusion-item {
    grid-template-columns: 1fr;
  }
}
</style>
