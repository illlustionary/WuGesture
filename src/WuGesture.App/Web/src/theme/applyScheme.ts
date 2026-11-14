import { COLOR_ROLES, type ColorScheme, type HexColor } from './tokens'
import {
  createColorSchemePair,
  deriveTitleBarColors,
  isHexColor,
  normalizeSchemeVariant,
  resolveSeedColor,
  type AppearanceInput,
  type ColorSource,
  type SchemeVariant,
  type TitleBarColors
} from './scheme'

/**
 * 把配色引擎的结果注入页面：
 * - `<style id="md-scheme">` 覆盖 SCSS 里的静态基线（`:root` 浅色 + `[data-theme='dark']` 深色）。
 * - `localStorage` 缓存最近一次生成的色板，应用挂载前先注入，避免首屏闪回默认蓝。
 *
 * 切换配色只替换样式文本，不重建组件。
 */

export const SCHEME_STYLE_ID = 'md-scheme'

export const SCHEME_CACHE_KEY = 'wugesture.md3.scheme.v1'

const CACHE_VERSION = 1

const DARK_QUERY = '(prefers-color-scheme: dark)'

export interface AppliedAppearance {
  /** 实际使用的种子色（动态配色时是 Windows 强调色）。 */
  seed: HexColor
  variant: SchemeVariant
  light: ColorScheme
  dark: ColorScheme
  titleBar: TitleBarColors
  source: ColorSource
  usedSystemAccent: boolean
  hasSystemAccent: boolean
  systemAccentFallback: boolean
}

interface CachedScheme {
  version: number
  seed: string
  variant: string
  light: unknown
  dark: unknown
}

/** 纯计算：解析配色输入并生成浅色/深色两套色板。 */
export function resolveAppearance(input: AppearanceInput): AppliedAppearance {
  const resolution = resolveSeedColor(input)
  const variant = normalizeSchemeVariant(input.schemeVariant)
  const { light, dark } = createColorSchemePair(resolution.seed, variant)

  return {
    seed: resolution.seed,
    variant,
    light,
    dark,
    titleBar: deriveTitleBarColors(light, dark),
    source: resolution.source,
    usedSystemAccent: resolution.usedSystemAccent,
    hasSystemAccent: resolution.hasSystemAccent,
    systemAccentFallback: resolution.systemAccentFallback
  }
}

/** 计算并立即应用色板，同时写入首屏缓存。 */
export function applyScheme(input: AppearanceInput): AppliedAppearance {
  const applied = resolveAppearance(input)
  writeSchemeStyle(applied)
  writeSchemeCache(applied)
  return applied
}

/** 应用挂载前调用：用缓存色板覆盖 SCSS 基线，`rules` 消息到达后再校正。 */
export function applyCachedScheme(): AppliedAppearance | null {
  const cached = readSchemeCache()
  if (!cached) {
    return null
  }

  writeSchemeStyle(cached)
  return cached
}

/** 切换 `data-theme`，浅/深色板已经同时注入，无需重新计算。 */
export function setThemeMode(isDark: boolean): void {
  document.documentElement.dataset.theme = isDark ? 'dark' : 'light'
}

/** 解析当前模式实际应为浅色还是深色；`system` 跟随 WebView 的 `prefers-color-scheme`。 */
export function isDarkForMode(mode: string): boolean {
  if (mode === 'dark') {
    return true
  }

  if (mode === 'light') {
    return false
  }

  return window.matchMedia(DARK_QUERY).matches
}

function writeSchemeStyle(applied: AppliedAppearance): void {
  if (typeof document === 'undefined') {
    return
  }

  const style = ensureStyleElement()
  style.textContent = [
    `:root{${schemeDeclarations(applied.light)}}`,
    `[data-theme='dark']{${schemeDeclarations(applied.dark)}}`
  ].join('')
}

function ensureStyleElement(): HTMLStyleElement {
  const existing = document.getElementById(SCHEME_STYLE_ID)
  if (existing instanceof HTMLStyleElement) {
    return existing
  }

  const style = document.createElement('style')
  style.id = SCHEME_STYLE_ID
  document.head.appendChild(style)
  return style
}

function schemeDeclarations(scheme: ColorScheme): string {
  return COLOR_ROLES.map(role => `--md-sys-color-${role}:${scheme[role]};`).join('')
}

function writeSchemeCache(applied: AppliedAppearance): void {
  const payload: CachedScheme = {
    version: CACHE_VERSION,
    seed: applied.seed,
    variant: applied.variant,
    light: applied.light,
    dark: applied.dark
  }

  try {
    window.localStorage.setItem(SCHEME_CACHE_KEY, JSON.stringify(payload))
  } catch {
    // 隐私模式或存储空间不足时缓存失败不影响当前页面配色。
  }
}

function readSchemeCache(): AppliedAppearance | null {
  let raw: string | null = null

  try {
    raw = window.localStorage.getItem(SCHEME_CACHE_KEY)
  } catch {
    return null
  }

  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as Partial<CachedScheme>
    if (parsed.version !== CACHE_VERSION || !isHexColor(parsed.seed)) {
      clearSchemeCache()
      return null
    }

    const light = parseCachedScheme(parsed.light)
    const dark = parseCachedScheme(parsed.dark)
    if (!light || !dark) {
      clearSchemeCache()
      return null
    }

    return {
      seed: normalizeCacheSeed(parsed.seed),
      variant: normalizeSchemeVariant(parsed.variant),
      light,
      dark,
      titleBar: deriveTitleBarColors(light, dark),
      source: 'preset',
      usedSystemAccent: false,
      hasSystemAccent: false,
      systemAccentFallback: false
    }
  } catch {
    clearSchemeCache()
    return null
  }
}

function parseCachedScheme(value: unknown): ColorScheme | null {
  if (typeof value !== 'object' || value === null) {
    return null
  }

  const record = value as Record<string, unknown>
  const colors = {} as ColorScheme

  for (const role of COLOR_ROLES) {
    const color = record[role]
    if (!isHexColor(color)) {
      return null
    }

    colors[role] = color.toUpperCase()
  }

  return colors
}

function normalizeCacheSeed(value: HexColor): HexColor {
  return `#${value.slice(1).toUpperCase()}`
}

function clearSchemeCache(): void {
  try {
    window.localStorage.removeItem(SCHEME_CACHE_KEY)
  } catch {
    // 忽略存储清理失败。
  }
}
