<script setup>
import AppShell from '@/components/AppShell.vue'
import IconActionButton from '@/components/IconActionButton.vue'
import ToggleCheckbox from '@/components/ToggleCheckbox.vue'
import { useGestureExclusionsStore } from '@/gestureEditor/stores/useGestureExclusionsStore'

const exclusionsStore = useGestureExclusionsStore()

function toggleDisableEdgeActions(application) {
  exclusionsStore.updateExcludedApplication(application, {
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
      <IconActionButton
        icon="add"
        label="添加程序"
        color="var(--accent-strong)"
        class="exclusion-add-button"
        @click="exclusionsStore.openExcludedApplicationPicker()"
      />
    </template>

    <template #right>
      <section
        v-if="exclusionsStore.hasExclusions"
        class="exclusion-list"
      >
        <article
          v-for="(
            { application, icon, fallbackGlyph }, index
          ) in exclusionsStore.exclusionsWithIcons"
          :key="application.path || application.name || index"
          class="exclusion-item"
        >
          <span
            class="exclusion-item__icon"
            aria-hidden="true"
          >
            <img
              v-if="icon"
              class="app-icon app-icon--small"
              :src="icon"
              alt=""
            />
            <span v-else>{{ fallbackGlyph }}</span>
          </span>
          <div class="exclusion-item__main">
            <strong>{{
              application.displayName || application.name || application.path
            }}</strong>
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
            @click="exclusionsStore.removeExcludedApplication(index)"
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

.exclusion-add-button {
  width: 36px;
  height: 36px;
  border-radius: 14px;
}

.exclusion-item {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto auto;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border: 1px solid var(--border-subtle);
  border-radius: 16px;
  background: var(--panel-inset);
}

.exclusion-item__icon {
  display: grid;
  place-items: center;
  width: 34px;
  height: 34px;
  border-radius: 12px;
  background: var(--interactive-icon-bg);
  color: var(--accent-strong);
  font-size: 14px;
  font-weight: 700;
  overflow: hidden;
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
    grid-template-columns: auto minmax(0, 1fr);
  }

  .exclusion-item :deep(.toggle-checkbox),
  .exclusion-item .icon-action-button {
    grid-column: 2;
  }
}
</style>
