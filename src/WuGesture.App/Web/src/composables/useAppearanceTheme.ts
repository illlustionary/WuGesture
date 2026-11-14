import { computed, onBeforeUnmount, ref, watch } from 'vue'
import {
  applyCachedScheme,
  applyScheme,
  isDarkForMode,
  setThemeMode,
  type AppliedAppearance
} from '@/theme/applyScheme'
import { normalizeHexColor, normalizeThemeMode, type AppearanceInput, type ThemeMode } from '@/theme/scheme'
import type { TitleBarColors } from '@/theme/scheme'

/**
 * 配置界面的“模式 × 配色来源”主题入口。
 *
 * - 模式：`system` / `light` / `dark`，只切换 `data-theme`，浅深两套色板已经同时注入。
 * - 配色来源：预设 / 自定义 / 跟随 Windows 强调色，由 `applyScheme` 生成并写入 `<style id="md-scheme">`。
 * - 生成色板后把标题栏前景/背景色回写到配置，宿主据此重绘自绘标题栏和托盘菜单。
 */

interface AppearanceUiSettings extends AppearanceInput {
  [key: string]: unknown
}

interface UiSettingsSnapshot {
  appearance: AppearanceUiSettings
  [key: string]: unknown
}

interface EditorState {
  hostConfigLoaded?: boolean
  rules?: unknown[]
  uiSettings?: {
    appearance?: AppearanceUiSettings
    [key: string]: unknown
  }
}

interface AppearanceEditor {
  state: EditorState
  getUiSettingsSnapshot(): UiSettingsSnapshot
  saveUiSettings(settings: UiSettingsSnapshot): void
}

const systemThemeQuery = window.matchMedia('(prefers-color-scheme: dark)')

const THEME_CYCLE: ThemeMode[] = ['system', 'light', 'dark']

const TITLE_BAR_KEYS: Array<keyof TitleBarColors> = [
  'lightTitleBarColor',
  'lightTitleBarTextColor',
  'darkTitleBarColor',
  'darkTitleBarTextColor'
]

export function useAppearanceTheme(editor: AppearanceEditor) {
  const systemPrefersDark = ref(systemThemeQuery.matches)
  const appearance = computed<AppearanceUiSettings>(() => editor.state.uiSettings?.appearance ?? {})
  const themeMode = computed<ThemeMode>(() => normalizeThemeMode(appearance.value.theme))
  const isDarkTheme = computed(
    () => themeMode.value === 'dark' || (themeMode.value === 'system' && systemPrefersDark.value)
  )
  const appliedScheme = ref<AppliedAppearance | null>(applyCachedScheme())
  // 宿主配置到达前，草稿仍是默认值；此时不要用默认色板覆盖首屏缓存。
  const initialAppearanceSignature = appearanceSignature(appearance.value)

  function appearanceSignature(value: AppearanceInput): string {
    return [value.colorSource, value.seedColor, value.schemeVariant, value.systemAccent]
      .map(item => String(item ?? ''))
      .join('|')
  }

  function updateSystemTheme(event: MediaQueryListEvent) {
    systemPrefersDark.value = event.matches
  }

  function refreshScheme(force = false) {
    if (
      !force &&
      appliedScheme.value &&
      !editor.state.hostConfigLoaded &&
      appearanceSignature(appearance.value) === initialAppearanceSignature
    ) {
      return
    }

    const next = applyScheme(appearance.value)
    appliedScheme.value = next
    persistTitleBarColors(next)
  }

  function persistTitleBarColors(applied: AppliedAppearance) {
    // 宿主配置到达前 state 里只是默认值，rules 为空时保存会被拒绝，也会弹出误报。
    if (!editor.state.hostConfigLoaded || !editor.state.rules?.length) {
      return
    }

    const settings = editor.getUiSettingsSnapshot()
    const target = settings.appearance
    if (!target) {
      return
    }

    const changed = TITLE_BAR_KEYS.some(
      key => normalizeHexColor(target[key], '#000000') !== normalizeHexColor(applied.titleBar[key], '#000000')
    )
    if (!changed) {
      return
    }

    Object.assign(target, applied.titleBar)
    editor.saveUiSettings(settings)
  }

  function cycleTheme() {
    const settings = editor.getUiSettingsSnapshot()
    const current = normalizeThemeMode(settings.appearance.theme)
    const next = THEME_CYCLE[(THEME_CYCLE.indexOf(current) + 1) % THEME_CYCLE.length]
    settings.appearance.theme = next
    editor.saveUiSettings(settings)
    setThemeMode(isDarkForMode(next))
  }

  watch(
    () => [
      appearance.value.colorSource,
      appearance.value.seedColor,
      appearance.value.schemeVariant,
      appearance.value.systemAccent
    ],
    () => refreshScheme(),
    { immediate: true }
  )

  // 宿主配置到达后强制重算一次，避免缓存色板和当前配置不一致。
  watch(
    () => editor.state.hostConfigLoaded,
    loaded => {
      if (loaded) {
        refreshScheme(true)
      }
    }
  )

  watch(isDarkTheme, isDark => setThemeMode(isDark), { immediate: true })

  systemThemeQuery.addEventListener('change', updateSystemTheme)

  onBeforeUnmount(() => {
    systemThemeQuery.removeEventListener('change', updateSystemTheme)
  })

  return { appliedScheme, isDarkTheme, themeMode, cycleTheme }
}
