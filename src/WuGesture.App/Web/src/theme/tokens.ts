/**
 * MD3 设计令牌的类型契约。
 *
 * 与 `src/styles/tokens/*.scss` 中的自定义属性一一对应。
 * 阶段 6 的配色引擎会基于这些类型生成色板并注入 `<style id="md-scheme">`。
 * 新增角色时必须同时更新 SCSS 令牌文件，否则类型与样式会漂移。
 */

/** MD3 标准颜色角色（由 seed 生成）。 */
export const COLOR_ROLES = [
  'primary',
  'on-primary',
  'primary-container',
  'on-primary-container',
  'inverse-primary',
  'secondary',
  'on-secondary',
  'secondary-container',
  'on-secondary-container',
  'tertiary',
  'on-tertiary',
  'tertiary-container',
  'on-tertiary-container',
  'error',
  'on-error',
  'error-container',
  'on-error-container',
  'surface',
  'on-surface',
  'on-surface-variant',
  'surface-dim',
  'surface-bright',
  'surface-container-lowest',
  'surface-container-low',
  'surface-container',
  'surface-container-high',
  'surface-container-highest',
  'outline',
  'outline-variant',
  'inverse-surface',
  'inverse-on-surface',
  'scrim',
  'shadow'
] as const

export type ColorRole = (typeof COLOR_ROLES)[number]

/**
 * 项目扩展颜色角色。
 * MD3 没有 success / warning，这两组不随 seed 生成，保持固定语义。
 */
export const EXTENDED_COLOR_ROLES = [
  'success',
  'on-success',
  'success-container',
  'on-success-container',
  'warning',
  'on-warning',
  'warning-container',
  'on-warning-container'
] as const

export type ExtendedColorRole = (typeof EXTENDED_COLOR_ROLES)[number]

/** 一套完整的配色方案：角色 → 色值。 */
export type ColorScheme = Record<ColorRole, string>

/** `#RRGGBB` 形式的十六进制颜色。 */
export type HexColor = `#${string}`

/** Typescale 角色。 */
export const TYPESCALE_ROLES = [
  'headline-small',
  'title-large',
  'title-medium',
  'title-small',
  'body-large',
  'body-medium',
  'body-small',
  'label-large',
  'label-medium',
  'label-small'
] as const

export type TypescaleRole = (typeof TYPESCALE_ROLES)[number]

/** 形状角色。 */
export const SHAPE_ROLES = ['none', 'extra-small', 'small', 'medium', 'large', 'extra-large', 'full'] as const

export type ShapeRole = (typeof SHAPE_ROLES)[number]

/** 4dp 网格间距（px）。 */
export const SPACING_STEPS = [4, 8, 12, 16, 24, 32, 48] as const

export type SpacingStep = (typeof SPACING_STEPS)[number]

/** 动效令牌。 */
export const MOTION_DURATIONS = {
  short: '100ms',
  medium: '200ms',
  long: '300ms'
} as const

export const MOTION_EASINGS = {
  standard: 'cubic-bezier(0.2, 0, 0, 1)',
  emphasized: 'cubic-bezier(0.2, 0, 0, 1)'
} as const

/** 生成颜色角色的 CSS 变量引用，避免在 TS 里手写 `var(--md-sys-color-*)`。 */
export function colorVar(role: ColorRole | ExtendedColorRole): string {
  return `var(--md-sys-color-${role})`
}

/** 生成 Typescale 的 CSS 变量引用。 */
export function typescaleVar(role: TypescaleRole, part: 'size' | 'line-height' | 'weight' = 'size'): string {
  return `var(--md-sys-typescale-${role}-${part})`
}

/** 生成形状令牌的 CSS 变量引用。 */
export function shapeVar(role: ShapeRole): string {
  return `var(--md-sys-shape-corner-${role})`
}
