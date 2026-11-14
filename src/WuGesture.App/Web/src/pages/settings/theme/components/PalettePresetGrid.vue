<script setup lang="ts">
import { computed } from 'vue'
import AppIcon from '@/components/ui/AppIcon.vue'
import { createColorScheme } from '@/theme/scheme'
import { THEME_PRESETS, type ThemePreset } from '@/theme/palettes'

/** 8 套预设色板卡片：四色条预览 + 选中描边。 */
const props = defineProps<{
  seed: string
  colorSource: string
}>()

const emit = defineEmits<{
  select: [preset: ThemePreset]
}>()

const previews = THEME_PRESETS.map(preset => {
  const scheme = createColorScheme(preset.seed, 'tonalSpot', false)
  return {
    preset,
    colors: [scheme.primary, scheme.secondary, scheme.tertiary, scheme['surface-container']]
  }
})

const selectedSeed = computed(() => String(props.seed ?? '').toUpperCase())
const isPresetSource = computed(() => props.colorSource === 'preset')

function isSelected(preset: ThemePreset): boolean {
  return isPresetSource.value && preset.seed.toUpperCase() === selectedSeed.value
}
</script>

<template>
  <div
    class="palette-grid"
    role="radiogroup"
    aria-label="预设配色"
  >
    <button
      v-for="item in previews"
      :key="item.preset.id"
      type="button"
      class="palette-card"
      :class="{ 'is-selected': isSelected(item.preset) }"
      role="radio"
      :aria-checked="isSelected(item.preset)"
      @click="emit('select', item.preset)"
    >
      <span
        class="palette-card__swatches"
        aria-hidden="true"
      >
        <span
          v-for="color in item.colors"
          :key="color"
          class="palette-card__swatch"
          :style="{ background: color }"
        />
      </span>
      <span class="palette-card__label">
        {{ item.preset.label }}
        <AppIcon
          v-if="isSelected(item.preset)"
          name="check"
          class="palette-card__check"
          aria-hidden="true"
        />
      </span>
    </button>
  </div>
</template>

<style scoped lang="scss">
.palette-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  gap: var(--md-sys-spacing-sm);
}

.palette-card {
  display: grid;
  gap: var(--md-sys-spacing-sm);
  padding: var(--md-sys-spacing-sm);
  border: 2px solid transparent;
  border-radius: var(--md-sys-shape-corner-medium);
  background: var(--md-sys-color-surface-container);
  color: var(--md-sys-color-on-surface);
  text-align: left;
  cursor: pointer;
  transition:
    border-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover {
    background: var(--md-sys-color-surface-container-high);
  }

  &:focus-visible {
    outline-offset: 0;
  }

  &.is-selected {
    border-color: var(--md-sys-color-primary);
    background: var(--md-sys-color-secondary-container);
    color: var(--md-sys-color-on-secondary-container);
  }
}

.palette-card__swatches {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  overflow: hidden;
  border-radius: var(--md-sys-shape-corner-small);
  border: 1px solid var(--md-sys-color-outline-variant);
}

.palette-card__swatch {
  height: 22px;
}

.palette-card__label {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--md-sys-spacing-xs);
  font-size: var(--md-sys-typescale-label-large-size);
  line-height: var(--md-sys-typescale-label-large-line-height);
  font-weight: var(--md-sys-typescale-label-large-weight);
}

.palette-card__check {
  width: 16px;
  height: 16px;
  flex: 0 0 auto;
  fill: none;
  stroke: currentColor;
}
</style>
