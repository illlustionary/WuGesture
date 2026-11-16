<script setup lang="ts">
import { computed } from 'vue'
import type { AppliedAppearance } from '@/theme/applyScheme'

const props = defineProps<{
  appearance: AppliedAppearance
  isDark: boolean
}>()

const palette = computed(() => (props.isDark ? props.appearance.dark : props.appearance.light))

const titleBarStyle = computed(() => ({
  background: palette.value.surface,
  color: palette.value['on-surface'],
  borderColor: palette.value['outline-variant']
}))

const bodyStyle = computed(() => ({
  background: palette.value.surface,
  color: palette.value['on-surface'],
  borderColor: palette.value['outline-variant']
}))

const cardStyle = computed(() => ({
  background: palette.value['surface-container-low'],
  borderColor: palette.value['outline-variant']
}))

const pills = computed(() => [
  { label: '← 返回', background: palette.value.secondary, color: palette.value['on-secondary'] },
  { label: '保存', background: palette.value.primary, color: palette.value['on-primary'] },
  {
    label: '滚轮',
    background: palette.value['tertiary-container'],
    color: palette.value['on-tertiary-container']
  },
  {
    label: '删除',
    background: palette.value['error-container'],
    color: palette.value['on-error-container']
  }
])

const swatches = computed(() => [
  { label: 'primary', color: palette.value.primary },
  { label: 'secondary', color: palette.value.secondary },
  { label: 'tertiary', color: palette.value.tertiary },
  { label: 'error', color: palette.value.error },
  { label: 'surfaceContainer', color: palette.value['surface-container'] }
])
</script>

<template>
  <div class="theme-preview">
    <div class="theme-preview__window">
      <div
        class="theme-preview__titlebar"
        :style="titleBarStyle"
      >
        <span class="theme-preview__brand">
          <span
            class="theme-preview__brand-dot"
            :style="{ background: palette.primary }"
          />
          WuGesture
        </span>
        <span
          class="theme-preview__window-actions"
          aria-hidden="true"
        >
          <span>—</span><span>▢</span><span>×</span>
        </span>
      </div>

      <div
        class="theme-preview__body"
        :style="bodyStyle"
      >
        <div
          class="theme-preview__card"
          :style="cardStyle"
        >
          <span class="theme-preview__card-title">手势规则</span>
          <span
            class="theme-preview__muted"
            :style="{ color: palette['on-surface-variant'] }"
          >
            全局作用域 · 4 条规则
          </span>
          <div class="theme-preview__pills">
            <span
              v-for="pill in pills"
              :key="pill.label"
              class="theme-preview__pill"
              :style="{ background: pill.background, color: pill.color }"
            >
              {{ pill.label }}
            </span>
          </div>
        </div>

        <div class="theme-preview__roles">
          <span
            v-for="swatch in swatches"
            :key="swatch.label"
            class="theme-preview__role"
            :style="{ color: palette['on-surface-variant'] }"
          >
            <span
              class="theme-preview__role-dot"
              :style="{ background: swatch.color }"
            />
            {{ swatch.label }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.theme-preview {
  display: grid;
  padding: var(--md-sys-spacing-sm);
  border-radius: var(--md-sys-shape-corner-large);
  background: var(--md-sys-color-surface-container);
}

.theme-preview__window {
  overflow: hidden;
  border: 1px solid;
  border-radius: var(--md-sys-shape-corner-medium);
}

.theme-preview__titlebar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--md-sys-spacing-md);
  height: 40px;
  padding: 0 var(--md-sys-spacing-md);
  border-bottom: 1px solid;
  font-size: var(--md-sys-typescale-label-large-size);
  font-weight: 500;
}

.theme-preview__brand {
  display: inline-flex;
  align-items: center;
  gap: var(--md-sys-spacing-sm);
}

.theme-preview__brand-dot {
  width: 12px;
  height: 12px;
  border-radius: var(--md-sys-shape-corner-full);
}

.theme-preview__window-actions {
  display: inline-flex;
  align-items: center;
  gap: var(--md-sys-spacing-lg);
  opacity: 0.72;
}

.theme-preview__body {
  display: grid;
  gap: var(--md-sys-spacing-lg);
  padding: var(--md-sys-spacing-lg);
  border-top: 0;
}

.theme-preview__card {
  display: grid;
  gap: var(--md-sys-spacing-xs);
  padding: var(--md-sys-spacing-lg);
  border: 1px solid;
  border-radius: var(--md-sys-shape-corner-medium);
}

.theme-preview__card-title {
  font-size: var(--md-sys-typescale-title-medium-size);
  line-height: var(--md-sys-typescale-title-medium-line-height);
  font-weight: var(--md-sys-typescale-title-medium-weight);
}

.theme-preview__muted {
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
}

.theme-preview__pills {
  display: flex;
  flex-wrap: wrap;
  gap: var(--md-sys-spacing-sm);
  padding-top: var(--md-sys-spacing-sm);
}

.theme-preview__pill {
  padding: 0 var(--md-sys-spacing-md);
  min-height: 32px;
  display: inline-flex;
  align-items: center;
  border-radius: var(--md-sys-shape-corner-full);
  font-size: var(--md-sys-typescale-label-large-size);
  line-height: var(--md-sys-typescale-label-large-line-height);
  font-weight: 500;
}

.theme-preview__roles {
  display: flex;
  flex-wrap: wrap;
  gap: var(--md-sys-spacing-md);
}

.theme-preview__role {
  display: inline-flex;
  align-items: center;
  gap: var(--md-sys-spacing-xs);
  font-size: var(--md-sys-typescale-label-small-size);
  line-height: var(--md-sys-typescale-label-small-line-height);
  font-family: var(--md-sys-typescale-font-family);
}

.theme-preview__role-dot {
  width: 14px;
  height: 14px;
  border-radius: var(--md-sys-shape-corner-full);
  border: 1px solid var(--md-sys-color-outline-variant);
}
</style>
