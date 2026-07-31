<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppShell from '@/components/AppShell.vue'
import IconActionButton from '@/components/IconActionButton.vue'
import ToggleCheckbox from '@/components/ToggleCheckbox.vue'
import RulesSection from '@/components/rules/RulesSection.vue'
import { useGestureExclusionsStore } from '@/gestureEditor/stores/useGestureExclusionsStore'
import { getApplicationKey } from '@/gestureEditor/stores/useGestureQuickSearchStore'

const exclusionsStore = useGestureExclusionsStore()
const route = useRoute()
const selectedExclusion = computed(() => String(route.query.selected || ''))

function toggleDisableEdgeActions(application) {
  exclusionsStore.updateExcludedApplication(application, {
    disableEdgeActions: Boolean(application.disableEdgeActions)
  })
}
</script>

<template>
  <AppShell layout-class="page-shell__grid--single page-shell__grid--editor">
    <template #right>
      <section class="rules-panel">
        <RulesSection
          title="排除项"
          flex
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

          <section
            v-if="exclusionsStore.hasExclusions"
            class="exclusion-list"
          >
            <article
              v-for="({ application, icon, fallbackGlyph }, index) in exclusionsStore.exclusionsWithIcons"
              :key="application.path || application.name || index"
              class="exclusion-item"
              :class="{
                'exclusion-item--selected': getApplicationKey(application, index) === selectedExclusion
              }"
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
                class="exclusion-item__delete ghost-button"
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
        </RulesSection>
      </section>
    </template>
  </AppShell>
</template>

<style scoped lang="scss">
.exclusion-list {
  display: grid;
  gap: 0;
}

:deep(.exclusion-add-button) {
  width: 36px;
  height: 36px;
}

.rules-panel {
  display: flex;
  flex-direction: column;
  gap: 0;
  min-width: 0;
  min-height: 100%;
}

.exclusion-item {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto auto;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border: 1px solid var(--border-subtle);
  border-radius: 0;
  background: var(--panel-inset);

  &--selected {
    border-color: var(--accent-border);
    box-shadow: 0 0 0 3px var(--focus-ring);
  }

  &:hover :deep(.exclusion-item__delete),
  &:focus-within :deep(.exclusion-item__delete),
  &--selected :deep(.exclusion-item__delete) {
    opacity: 1;
    pointer-events: auto;
  }
}

.exclusion-item + .exclusion-item {
  border-top-width: 0;
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

:deep(.exclusion-item__delete) {
  opacity: 0;
  pointer-events: none;
  transition: opacity 120ms ease;
}
.exclusion-item,
.exclusion-empty {
  border-radius: 10px;
}
.exclusion-empty {
  margin: 0;
  padding: 18px;
  border: 1px dashed var(--border);
  color: var(--muted);
  background: var(--panel-inset);
  font-size: 13px;
}

@media (max-width: 720px) {
  .exclusion-item {
    grid-template-columns: auto minmax(0, 1fr);
  }

  .exclusion-item :deep(.toggle-checkbox),
  .exclusion-item :deep(.icon-action-button) {
    grid-column: 2;
  }
}
</style>
