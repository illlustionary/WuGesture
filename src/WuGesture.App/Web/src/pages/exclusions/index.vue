<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppShell from '@/components/layout/AppShell.vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'
import ToggleCheckbox from '@/components/form/ToggleCheckbox.vue'
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
  <AppShell
    title="排除项"
    description="这些程序不会触发手势与边缘操作"
    layout-class="page-shell__grid--single page-shell__grid--editor"
  >
    <template #actions>
      <IconActionButton
        icon="add"
        label="添加程序"
        class="exclusion-add-button"
        @click="exclusionsStore.openExcludedApplicationPicker()"
      />
    </template>

    <template #right>
      <section class="rules-panel">
        <RulesSection
          title="排除项"
          flex
        >
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
@use '../../styles/tokens/state' as *;

.exclusion-list {
  display: grid;
  overflow: hidden;
  border: 1px solid var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-medium);
  background: var(--md-sys-color-surface-container-lowest);
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
  @include state-layer(var(--md-sys-color-on-surface));

  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto auto;
  align-items: center;
  gap: var(--md-sys-spacing-md);
  min-height: var(--md-sys-size-row-single);
  padding: var(--md-sys-spacing-sm) var(--md-sys-spacing-sm) var(--md-sys-spacing-sm) var(--md-sys-spacing-lg);
  background: transparent;

  &--selected {
    background: var(--md-sys-color-secondary-container);
  }
}

.exclusion-item + .exclusion-item {
  border-top: 1px solid var(--md-sys-color-outline-variant);
}

.exclusion-item__icon {
  position: relative;
  z-index: 1;
  display: grid;
  place-items: center;
  width: 32px;
  height: 32px;
  overflow: hidden;
  border-radius: var(--md-sys-shape-corner-small);
  background: var(--md-sys-color-surface-container-high);
  color: var(--md-sys-color-primary);
  font-size: var(--md-sys-typescale-title-small-size);
  font-weight: var(--md-sys-typescale-title-small-weight);
}

.exclusion-item__main {
  position: relative;
  z-index: 1;
  display: grid;
  gap: 2px;
  min-width: 0;

  strong,
  span {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  strong {
    color: var(--md-sys-color-on-surface);
    font-size: var(--md-sys-typescale-body-medium-size);
    line-height: var(--md-sys-typescale-body-medium-line-height);
    font-weight: 500;
  }

  span {
    color: var(--md-sys-color-on-surface-variant);
    font-size: var(--md-sys-typescale-body-small-size);
    line-height: var(--md-sys-typescale-body-small-line-height);
  }
}

.exclusion-item :deep(.toggle-checkbox),
.exclusion-item :deep(.exclusion-item__delete) {
  position: relative;
  z-index: 1;
}

.exclusion-item :deep(.toggle-checkbox) {
  gap: var(--md-sys-spacing-md);
}

.exclusion-empty {
  margin: 0;
  padding: var(--md-sys-spacing-lg);
  border: 1px dashed var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-medium);
  background: transparent;
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
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
