import {
  Hct,
  MaterialDynamicColors,
  SchemeExpressive,
  SchemeMonochrome,
  SchemeNeutral,
  SchemeTonalSpot,
  SchemeVibrant,
  argbFromHex,
  hexFromArgb,
  type DynamicColor,
  type DynamicScheme
} from '@material/material-color-utilities'
import { COLOR_ROLES, type ColorRole, type ColorScheme, type HexColor } from './tokens'
import { DEFAULT_SEED_COLOR } from './palettes'

/**
 * MD3 配色引擎：以 seed + 调色板变体生成完整的角色色板。
 *
 * 文件只做纯计算，不访问 DOM / localStorage；首屏注入与缓存见 `applyScheme.ts`。
 */

export type ThemeMode = 'system' | 'light' | 'dark'

export type ColorSource = 'preset' | 'custom' | 'system'

export type SchemeVariant = 'tonalSpot' | 'neutral' | 'vibrant' | 'expressive' | 'monochrome'

export const THEME_MODES = ['system', 'light', 'dark'] as const

export const COLOR_SOURCES = ['preset', 'custom', 'system'] as const

export const SCHEME_VARIANTS = ['tonalSpot', 'neutral', 'vibrant', 'expressive', 'monochrome'] as const

/** 宿主下发的 `uiSettings.appearance` 中与配色相关的字段。 */
export interface AppearanceInput {
  theme?: string
  colorSource?: string
  seedColor?: string
  schemeVariant?: string
  /** 仅运行时存在：宿主读取的 Windows 强调色，不写入配置文件。 */
  systemAccent?: string
}

export interface SeedResolution {
  /** 实际用于生成色板的种子色。 */
  seed: HexColor
  /** 归一化后的配色来源。 */
  source: ColorSource
  /** 是否真的使用了 Windows 强调色。 */
  usedSystemAccent: boolean
  /** 宿主是否提供了强调色（可能是不可用的颜色）。 */
  hasSystemAccent: boolean
  /** 强调色过灰或过亮/过暗，已回退到种子色。 */
  systemAccentFallback: boolean
}

export interface TitleBarColors {
  lightTitleBarColor: HexColor
  lightTitleBarTextColor: HexColor
  darkTitleBarColor: HexColor
  darkTitleBarTextColor: HexColor
}

interface SchemeConstructor {
  new (sourceColorHct: Hct, isDark: boolean, contrastLevel: number): DynamicScheme
}

const SCHEME_CLASSES: Record<SchemeVariant, SchemeConstructor> = {
  tonalSpot: SchemeTonalSpot,
  neutral: SchemeNeutral,
  vibrant: SchemeVibrant,
  expressive: SchemeExpressive,
  monochrome: SchemeMonochrome
}

/** MD3 角色 → `MaterialDynamicColors` 的静态字段名。 */
const ROLE_GETTERS: Record<ColorRole, keyof typeof MaterialDynamicColors> = {
  primary: 'primary',
  'on-primary': 'onPrimary',
  'primary-container': 'primaryContainer',
  'on-primary-container': 'onPrimaryContainer',
  'inverse-primary': 'inversePrimary',
  secondary: 'secondary',
  'on-secondary': 'onSecondary',
  'secondary-container': 'secondaryContainer',
  'on-secondary-container': 'onSecondaryContainer',
  tertiary: 'tertiary',
  'on-tertiary': 'onTertiary',
  'tertiary-container': 'tertiaryContainer',
  'on-tertiary-container': 'onTertiaryContainer',
  error: 'error',
  'on-error': 'onError',
  'error-container': 'errorContainer',
  'on-error-container': 'onErrorContainer',
  surface: 'surface',
  'on-surface': 'onSurface',
  'on-surface-variant': 'onSurfaceVariant',
  'surface-dim': 'surfaceDim',
  'surface-bright': 'surfaceBright',
  'surface-container-lowest': 'surfaceContainerLowest',
  'surface-container-low': 'surfaceContainerLow',
  'surface-container': 'surfaceContainer',
  'surface-container-high': 'surfaceContainerHigh',
  'surface-container-highest': 'surfaceContainerHighest',
  outline: 'outline',
  'outline-variant': 'outlineVariant',
  'inverse-surface': 'inverseSurface',
  'inverse-on-surface': 'inverseOnSurface',
  scrim: 'scrim',
  shadow: 'shadow'
}

/** 三到六位十六进制颜色（可省略 `#`）。 */
const HEX_PATTERN = /^#?([0-9a-f]{3}|[0-9a-f]{6})$/i

/** 完整 `#RRGGBB`。 */
const FULL_HEX_PATTERN = /^#[0-9a-f]{6}$/i

export function isHexColor(value: unknown): value is HexColor {
  return typeof value === 'string' && FULL_HEX_PATTERN.test(value.trim())
}

/** 把任意颜色输入规范成 `#RRGGBB`，非法时返回 fallback。 */
export function normalizeHexColor(value: unknown, fallback: HexColor = DEFAULT_SEED_COLOR): HexColor {
  const match = HEX_PATTERN.exec(String(value ?? '').trim())
  if (!match) {
    return fallback
  }

  const digits = match[1]
  const expanded = digits.length === 3 ? digits.replace(/./g, digit => digit + digit) : digits
  return `#${expanded.toUpperCase()}`
}

export function normalizeThemeMode(value: unknown): ThemeMode {
  const normalized = String(value ?? '')
    .trim()
    .toLowerCase()
  return (THEME_MODES as readonly string[]).includes(normalized) ? (normalized as ThemeMode) : 'system'
}

export function normalizeColorSource(value: unknown): ColorSource {
  const normalized = String(value ?? '')
    .trim()
    .toLowerCase()
  return (COLOR_SOURCES as readonly string[]).includes(normalized) ? (normalized as ColorSource) : 'preset'
}

export function normalizeSchemeVariant(value: unknown): SchemeVariant {
  const normalized = String(value ?? '').trim()
  return (SCHEME_VARIANTS as readonly string[]).includes(normalized) ? (normalized as SchemeVariant) : 'tonalSpot'
}

function createDynamicScheme(seed: HexColor, variant: SchemeVariant, isDark: boolean): DynamicScheme {
  const SchemeClass = SCHEME_CLASSES[variant] ?? SCHEME_CLASSES.tonalSpot
  return new SchemeClass(Hct.fromInt(argbFromHex(seed)), isDark, 0)
}

/** 以 seed + variant + 明暗生成一套完整角色色板。 */
export function createColorScheme(seed: string, variant: SchemeVariant = 'tonalSpot', isDark = false): ColorScheme {
  const normalizedSeed = normalizeHexColor(seed)
  const scheme = createDynamicScheme(normalizedSeed, normalizeSchemeVariant(variant), isDark)
  const colors = {} as ColorScheme

  for (const role of COLOR_ROLES) {
    const getter = MaterialDynamicColors[ROLE_GETTERS[role]] as DynamicColor
    colors[role] = hexFromArgb(getter.getArgb(scheme)).toUpperCase()
  }

  return colors
}

export function createColorSchemePair(
  seed: string,
  variant: SchemeVariant = 'tonalSpot'
): { light: ColorScheme; dark: ColorScheme } {
  return {
    light: createColorScheme(seed, variant, false),
    dark: createColorScheme(seed, variant, true)
  }
}

/**
 * 动态配色使用 Windows 强调色作为 seed。
 * 强调色过灰或过亮/过暗时生成的角色色对比度会明显失真，此时回退到用户保存的种子色。
 */
export function isUsableAccent(value: unknown): boolean {
  if (!isHexColor(value)) {
    return false
  }

  const { s, l } = hexToHsl(value)
  return s >= 0.12 && l > 0.12 && l < 0.9
}

export function resolveSeedColor(input: AppearanceInput): SeedResolution {
  const source = normalizeColorSource(input.colorSource)
  const fallbackSeed = normalizeHexColor(input.seedColor)

  if (source !== 'system') {
    return {
      seed: fallbackSeed,
      source,
      usedSystemAccent: false,
      hasSystemAccent: false,
      systemAccentFallback: false
    }
  }

  const hasSystemAccent = isHexColor(input.systemAccent)
  const accent = normalizeHexColor(input.systemAccent, fallbackSeed)

  if (hasSystemAccent && isUsableAccent(accent)) {
    return {
      seed: accent,
      source,
      usedSystemAccent: true,
      hasSystemAccent,
      systemAccentFallback: false
    }
  }

  return {
    seed: fallbackSeed,
    source,
    usedSystemAccent: false,
    hasSystemAccent,
    systemAccentFallback: hasSystemAccent
  }
}

/**
 * 自绘标题栏和托盘菜单读取同一组颜色。
 * 使用 `surface` / `onSurface` 让标题栏与页面背景、WebView 首帧底色完全一致，避免重建时闪白。
 */
export function deriveTitleBarColors(light: ColorScheme, dark: ColorScheme): TitleBarColors {
  return {
    lightTitleBarColor: toHex(light.surface),
    lightTitleBarTextColor: toHex(light['on-surface']),
    darkTitleBarColor: toHex(dark.surface),
    darkTitleBarTextColor: toHex(dark['on-surface'])
  }
}

function toHex(value: string): HexColor {
  return normalizeHexColor(value, DEFAULT_SEED_COLOR)
}

function hexToHsl(value: string): { h: number; s: number; l: number } {
  const hex = normalizeHexColor(value).slice(1)
  const red = Number.parseInt(hex.slice(0, 2), 16) / 255
  const green = Number.parseInt(hex.slice(2, 4), 16) / 255
  const blue = Number.parseInt(hex.slice(4, 6), 16) / 255
  const max = Math.max(red, green, blue)
  const min = Math.min(red, green, blue)
  const lightness = (max + min) / 2
  const delta = max - min

  if (delta === 0) {
    return { h: 0, s: 0, l: lightness }
  }

  const saturation = lightness > 0.5 ? delta / (2 - max - min) : delta / (max + min)

  let hue: number
  switch (max) {
    case red:
      hue = (green - blue) / delta + (green < blue ? 6 : 0)
      break
    case green:
      hue = (blue - red) / delta + 2
      break
    default:
      hue = (red - green) / delta + 4
      break
  }

  return { h: hue / 6, s: saturation, l: lightness }
}
