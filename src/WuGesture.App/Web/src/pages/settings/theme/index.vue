<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import BaseInput from '@/components/form/BaseInput.vue'
import AppShell from '@/components/layout/AppShell.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppIcon from '@/components/ui/AppIcon.vue'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import SettingsField from '@/pages/settings/components/SettingsField.vue'
import SettingsSectionCard from '@/pages/settings/components/SettingsSectionCard.vue'
import { useUiSettingsDraft } from '@/pages/settings/composables/useUiSettingsDraft'
import { DEFAULT_SEED_COLOR, findPresetBySeed, type ThemePreset } from '@/theme/palettes'
import {
  applyScheme,
  isDarkForMode,
  resolveAppearance,
  setThemeMode,
  type AppliedAppearance
} from '@/theme/applyScheme'
import { normalizeHexColor, type ColorSource, type ThemeMode } from '@/theme/scheme'
import PalettePresetGrid from './components/PalettePresetGrid.vue'
import ThemePreview from './components/ThemePreview.vue'

interface ThemeModeOption {
  value: ThemeMode
  label: string
  icon: string
}

interface ColorSourceOption {
  value: ColorSource
  label: string
  note: string
}

const THEME_OPTIONS: readonly ThemeModeOption[] = [
  { value: 'system', label: '跟随系统', icon: 'monitor' },
  { value: 'light', label: '浅色', icon: 'sun' },
  { value: 'dark', label: '深色', icon: 'moon' }
]

const SOURCE_OPTIONS: readonly ColorSourceOption[] = [
  { value: 'preset', label: '预设色板', note: '从 8 套内置配色中选择' },
  { value: 'custom', label: '自定义', note: '手动指定种子色' },
  { value: 'system', label: '跟随系统', note: '使用 Windows 强调色' }
]

const HEX_PATTERN = /^#?([0-9a-f]{3}|[0-9a-f]{6})$/i

const settingsStore = useGestureEditorContext()
const { draft, queuePersistDraft, flushPersistDraft } = useUiSettingsDraft(settingsStore)

const applied = ref<AppliedAppearance>(resolveAppearance(draft.appearance))
const hexInput = ref(String(draft.appearance.seedColor ?? DEFAULT_SEED_COLOR))
const isDark = computed(() => isDarkForMode(draft.appearance.theme))
const systemAccent = computed(() => String(draft.appearance.systemAccent ?? '').trim())
const themeRowRef = ref<HTMLElement | null>(null)
const sourceRowRef = ref<HTMLElement | null>(null)
let appearanceFrame = 0
let appliedSignature = ''
let colorDragging = false

const systemMessage = computed(() => {
  if (!systemAccent.value) {
    return `未读取到 Windows 强调色，暂时使用当前种子色 ${draft.appearance.seedColor}。`
  }

  if (applied.value.systemAccentFallback) {
    return `Windows 强调色 ${systemAccent.value} 过灰或过亮/过暗，已回退到种子色 ${applied.value.seed}。`
  }

  return `正在使用 Windows 强调色 ${systemAccent.value}。`
})

/** 影响色板的字段签名：宿主回灌草稿或用户操作后，用它判断是否需要重算。 */
function appearanceSignature() {
  return [
    draft.appearance.theme,
    draft.appearance.colorSource,
    draft.appearance.seedColor,
    draft.appearance.schemeVariant,
    draft.appearance.systemAccent
  ].join('\u0000')
}

/** 立即预览：生成色板、写入样式、同步标题栏配色和明暗模式。 */
function applyAppearance(options: { cache?: boolean } = {}) {
  const next = applyScheme(draft.appearance, options)
  applied.value = next
  Object.assign(draft.appearance, next.titleBar)
  hexInput.value = String(draft.appearance.seedColor ?? DEFAULT_SEED_COLOR)
  setThemeMode(isDarkForMode(draft.appearance.theme))
  appliedSignature = appearanceSignature()
}

/**
 * 取色器拖动时每个 input 事件都会改种子色。把它们合并到下一帧执行一次：每个事件都重算 66 个 HCT
 * 角色、整体替换 `<style>` 文本并写 localStorage，会让主线程持续满载。
 */
function scheduleAppearance() {
  if (appearanceFrame) {
    return
  }

  appearanceFrame = window.requestAnimationFrame(() => {
    appearanceFrame = 0
    if (appearanceSignature() === appliedSignature) {
      // 已经由 saveAppearance() 同步应用过，不再重复计算。
      return
    }
    applyAppearance({ cache: !colorDragging })
  })
}

function cancelScheduledAppearance() {
  if (!appearanceFrame) {
    return
  }

  window.cancelAnimationFrame(appearanceFrame)
  appearanceFrame = 0
}

function saveAppearance() {
  colorDragging = false
  cancelScheduledAppearance()
  applyAppearance()
  queuePersistDraft()
  flushPersistDraft()
}

function selectMode(mode: ThemeMode) {
  draft.appearance.theme = mode
  saveAppearance()
}

function selectSource(source: ColorSource) {
  draft.appearance.colorSource = source
  if (source === 'preset' && !findPresetBySeed(String(draft.appearance.seedColor ?? ''))) {
    draft.appearance.seedColor = DEFAULT_SEED_COLOR
  }
  saveAppearance()
}

function selectPreset(preset: ThemePreset) {
  draft.appearance.colorSource = 'preset'
  draft.appearance.seedColor = preset.seed
  saveAppearance()
}

/** radiogroup 的 roving tabindex：方向键循环、Home/End 跳到首尾，选中和焦点同步。 */
function resolveRadioIndex(event: KeyboardEvent, index: number, total: number): number | null {
  if (event.key === 'ArrowRight' || event.key === 'ArrowDown') {
    return (index + 1) % total
  }
  if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
    return (index - 1 + total) % total
  }
  if (event.key === 'Home') {
    return 0
  }
  if (event.key === 'End') {
    return total - 1
  }
  return null
}

function focusRadio(row: HTMLElement | null, index: number) {
  row?.querySelectorAll<HTMLButtonElement>('.app-button')[index]?.focus()
}

function handleModeKeydown(event: KeyboardEvent, index: number) {
  const nextIndex = resolveRadioIndex(event, index, THEME_OPTIONS.length)
  if (nextIndex === null) {
    return
  }

  event.preventDefault()
  selectMode(THEME_OPTIONS[nextIndex].value)
  focusRadio(themeRowRef.value, nextIndex)
}

function handleSourceKeydown(event: KeyboardEvent, index: number) {
  const nextIndex = resolveRadioIndex(event, index, SOURCE_OPTIONS.length)
  if (nextIndex === null) {
    return
  }

  event.preventDefault()
  selectSource(SOURCE_OPTIONS[nextIndex].value)
  focusRadio(sourceRowRef.value, nextIndex)
}

function onColorInput(event: Event) {
  colorDragging = true
  draft.appearance.colorSource = 'custom'
  draft.appearance.seedColor = normalizeHexColor((event.target as HTMLInputElement).value)
  queuePersistDraft()
}

/** 取色器关闭时落一次最终值：补写缓存并立即持久化。 */
function onColorChange() {
  colorDragging = false
  saveAppearance()
}

function commitHexColor() {
  const raw = String(hexInput.value ?? '').trim()
  if (!HEX_PATTERN.test(raw)) {
    hexInput.value = String(draft.appearance.seedColor ?? DEFAULT_SEED_COLOR)
    return
  }

  draft.appearance.colorSource = 'custom'
  draft.appearance.seedColor = normalizeHexColor(raw)
  saveAppearance()
}

// 宿主重载配置或恢复默认设置后，草稿会被替换，这里同步预览和标题栏缓存。
watch(
  () => [
    draft.appearance.theme,
    draft.appearance.colorSource,
    draft.appearance.seedColor,
    draft.appearance.schemeVariant,
    draft.appearance.systemAccent
  ],
  () => scheduleAppearance(),
  { immediate: true }
)
</script>

<template>
  <AppShell
    title="外观"
    description="选择明暗模式、配色来源和种子色；自绘标题栏与托盘菜单会跟随同一套色板。"
    layout-class="page-shell__grid--single"
  >
    <template #right>
      <section class="page-stack">
        <SettingsSectionCard
          title="明暗模式"
          note="“跟随系统”会随 Windows 的应用模式自动切换，浅色与深色色板都已经生成，切换时不会重新计算。"
        >
          <div
            ref="themeRowRef"
            class="theme-option-row"
            role="radiogroup"
            aria-label="明暗模式"
          >
            <AppButton
              v-for="(option, index) in THEME_OPTIONS"
              :key="option.value"
              :variant="draft.appearance.theme === option.value ? 'filled' : 'outlined'"
              role="radio"
              :aria-checked="draft.appearance.theme === option.value"
              :tabindex="draft.appearance.theme === option.value ? 0 : -1"
              @click="selectMode(option.value)"
              @keydown="handleModeKeydown($event, index)"
            >
              <template #leading>
                <AppIcon
                  :name="option.icon"
                  aria-hidden="true"
                />
              </template>
              {{ option.label }}
            </AppButton>
          </div>
        </SettingsSectionCard>

        <SettingsSectionCard
          title="配色来源"
          note="配色会随配置一起保存、导入导出和 WebDAV 同步；系统强调色只在本机读取，不会写入配置文件。"
        >
          <div
            ref="sourceRowRef"
            class="theme-option-row"
            role="radiogroup"
            aria-label="配色来源"
          >
            <AppButton
              v-for="(option, index) in SOURCE_OPTIONS"
              :key="option.value"
              :variant="draft.appearance.colorSource === option.value ? 'filled' : 'outlined'"
              role="radio"
              :aria-checked="draft.appearance.colorSource === option.value"
              :tabindex="draft.appearance.colorSource === option.value ? 0 : -1"
              :title="option.note"
              @click="selectSource(option.value)"
              @keydown="handleSourceKeydown($event, index)"
            >
              {{ option.label }}
            </AppButton>
          </div>

          <div
            v-if="draft.appearance.colorSource === 'preset'"
            class="theme-source-panel"
          >
            <PalettePresetGrid
              :seed="draft.appearance.seedColor"
              :color-source="draft.appearance.colorSource"
              @select="selectPreset"
            />
          </div>

          <div
            v-else-if="draft.appearance.colorSource === 'custom'"
            class="theme-source-panel theme-custom"
          >
            <input
              class="theme-custom__picker"
              type="color"
              :value="draft.appearance.seedColor"
              aria-label="自定义种子色"
              @input="onColorInput"
              @change="onColorChange"
            />
            <SettingsField
              label="HEX 颜色"
              note="支持 #RGB 或 #RRGGBB"
            >
              <BaseInput
                v-model="hexInput"
                maxlength="7"
                placeholder="#529EF1"
                spellcheck="false"
                @blur="commitHexColor"
                @keydown.enter.prevent="commitHexColor"
              />
            </SettingsField>
          </div>

          <div
            v-else
            class="theme-source-panel theme-system"
          >
            <span
              class="theme-system__swatch"
              :style="{ background: applied.seed }"
              aria-hidden="true"
            />
            <div class="theme-system__copy">
              <strong>Windows 强调色</strong>
              <span>{{ systemMessage }}</span>
            </div>
          </div>
        </SettingsSectionCard>

        <SettingsSectionCard
          title="实时预览"
          note="预览使用当前色板，与标题栏、侧栏、按钮和提示条的实际颜色一致。"
        >
          <ThemePreview
            :appearance="applied"
            :is-dark="isDark"
          />
        </SettingsSectionCard>
      </section>
    </template>
  </AppShell>
</template>

<style scoped lang="scss">
.theme-option-row {
  display: flex;
  flex-wrap: wrap;
  gap: var(--md-sys-spacing-sm);
}

.theme-source-panel {
  margin-top: var(--md-sys-spacing-md);
}

.theme-custom {
  display: grid;
  grid-template-columns: 148px minmax(0, 1fr);
  gap: var(--md-sys-spacing-md);
  align-items: start;
}

.theme-custom__picker {
  width: 100%;
  min-height: 148px;
}

.theme-system {
  display: flex;
  align-items: center;
  gap: var(--md-sys-spacing-md);
  padding: var(--md-sys-spacing-md);
  border-radius: var(--md-sys-shape-corner-small);
  background: var(--md-sys-color-surface-container);
}

.theme-system__swatch {
  width: 48px;
  height: 48px;
  flex: 0 0 auto;
  border-radius: var(--md-sys-shape-corner-full);
  border: 1px solid var(--md-sys-color-outline-variant);
}

.theme-system__copy {
  display: grid;
  gap: var(--md-sys-spacing-xs);
  min-width: 0;

  strong {
    font-size: var(--md-sys-typescale-title-small-size);
    line-height: var(--md-sys-typescale-title-small-line-height);
    font-weight: var(--md-sys-typescale-title-small-weight);
  }

  span {
    color: var(--md-sys-color-on-surface-variant);
    font-size: var(--md-sys-typescale-body-small-size);
    line-height: var(--md-sys-typescale-body-small-line-height);
  }
}

@media (max-width: 900px) {
  .theme-custom {
    grid-template-columns: minmax(0, 1fr);
  }

  .theme-custom__picker {
    min-height: 64px;
  }
}
</style>
