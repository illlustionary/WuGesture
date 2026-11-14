import { describe, expect, it } from 'vitest'
import { THEME_PRESETS } from './palettes'
import {
  COLOR_SOURCES,
  createColorScheme,
  createColorSchemePair,
  deriveTitleBarColors,
  isHexColor,
  isUsableAccent,
  normalizeColorSource,
  normalizeHexColor,
  normalizeSchemeVariant,
  normalizeThemeMode,
  resolveSeedColor
} from './scheme'
import { COLOR_ROLES, type ColorScheme } from './tokens'

const SEED = '#529EF1'

function expectCompleteScheme(scheme: ColorScheme) {
  for (const role of COLOR_ROLES) {
    expect(scheme[role], role).toMatch(/^#[0-9A-F]{6}$/)
  }
}

describe('normalizeHexColor', () => {
  it('展开三位十六进制并统一大写', () => {
    expect(normalizeHexColor('#5af')).toBe('#55AAFF')
    expect(normalizeHexColor('009688')).toBe('#009688')
  })

  it('非法输入回退到 fallback', () => {
    expect(normalizeHexColor('#GGGGGG', '#123456')).toBe('#123456')
    expect(normalizeHexColor('', '#123456')).toBe('#123456')
    expect(normalizeHexColor(undefined, '#123456')).toBe('#123456')
  })
})

describe('normalize enums', () => {
  it('模式和配色来源按白名单回退', () => {
    expect(normalizeThemeMode('DARK')).toBe('dark')
    expect(normalizeThemeMode('wallpaper')).toBe('system')
    expect(normalizeColorSource('SYSTEM')).toBe('system')
    expect(normalizeColorSource('wallpaper')).toBe('preset')
    expect(COLOR_SOURCES).toEqual(['preset', 'custom', 'system'])
  })

  it('调色板变体按白名单回退到 tonalSpot', () => {
    expect(normalizeSchemeVariant('vibrant')).toBe('vibrant')
    expect(normalizeSchemeVariant('unknown')).toBe('tonalSpot')
  })
})

describe('createColorScheme', () => {
  it('覆盖全部颜色角色，并与当前引擎基线一致', () => {
    const light = createColorScheme(SEED, 'tonalSpot', false)
    const dark = createColorScheme(SEED, 'tonalSpot', true)

    expectCompleteScheme(light)
    expectCompleteScheme(dark)
    // 基线来自 `@material/material-color-utilities` 0.4.0 的 Spec 2021 Tonal Spot。
    // 计划书 §4.1 的表格是设计参考值（部分取自紫色基线色板），运行时会以引擎结果为准；
    // `_color.scss` 的兜底值已同步到这里，保证首屏与运行时不出现明显跳色。
    expect(light).toMatchObject({
      primary: '#38608F',
      'on-primary': '#FFFFFF',
      'primary-container': '#D2E4FF',
      secondary: '#535F70',
      tertiary: '#6C5677',
      surface: '#F8F9FF',
      'on-surface': '#191C20',
      'surface-container-low': '#F2F3FA',
      outline: '#73777F'
    })
    expect(dark).toMatchObject({
      primary: '#A2C9FE',
      'on-primary': '#00325A',
      'primary-container': '#1D4875',
      surface: '#111418',
      'on-surface': '#E1E2E8',
      'surface-container-low': '#191C20',
      outline: '#8D9199'
    })
  })

  it('相同 seed 和变体生成完全相同的结果', () => {
    expect(createColorScheme(SEED, 'tonalSpot', false)).toEqual(createColorScheme(SEED, 'tonalSpot', false))
  })

  it('浅色与深色成对生成', () => {
    const pair = createColorSchemePair(SEED, 'tonalSpot')
    expect(pair.light).toEqual(createColorScheme(SEED, 'tonalSpot', false))
    expect(pair.dark).toEqual(createColorScheme(SEED, 'tonalSpot', true))
  })

  it('不同变体生成不同色板', () => {
    const tonal = createColorScheme(SEED, 'tonalSpot', false)
    const vibrant = createColorScheme(SEED, 'vibrant', false)
    const monochrome = createColorScheme(SEED, 'monochrome', false)
    expect(vibrant.primary).not.toBe(tonal.primary)
    expect(monochrome.secondary).not.toBe(tonal.secondary)
  })
})

describe('presets', () => {
  it('8 套预设 seed 都是合法 hex', () => {
    expect(THEME_PRESETS).toHaveLength(8)
    for (const preset of THEME_PRESETS) {
      expect(isHexColor(preset.seed), preset.id).toBe(true)
    }
  })
})

describe('resolveSeedColor', () => {
  it('预设和自定义使用保存的种子色', () => {
    expect(resolveSeedColor({ colorSource: 'preset', seedColor: '#009688' })).toMatchObject({
      seed: '#009688',
      usedSystemAccent: false
    })
    expect(resolveSeedColor({ colorSource: 'custom', seedColor: '#123456' })).toMatchObject({
      seed: '#123456',
      usedSystemAccent: false
    })
  })

  it('跟随系统时优先使用可用的 Windows 强调色', () => {
    expect(resolveSeedColor({ colorSource: 'system', seedColor: '#529EF1', systemAccent: '#0078D4' })).toMatchObject({
      seed: '#0078D4',
      usedSystemAccent: true,
      hasSystemAccent: true,
      systemAccentFallback: false
    })
  })

  it('强调色过灰或缺失时回退到种子色', () => {
    expect(isUsableAccent('#808080')).toBe(false)
    expect(resolveSeedColor({ colorSource: 'system', seedColor: '#009688', systemAccent: '#808080' })).toMatchObject({
      seed: '#009688',
      usedSystemAccent: false,
      systemAccentFallback: true
    })

    expect(resolveSeedColor({ colorSource: 'system', seedColor: '#009688' })).toMatchObject({
      seed: '#009688',
      usedSystemAccent: false,
      systemAccentFallback: false
    })
  })
})

describe('deriveTitleBarColors', () => {
  it('标题栏使用 surface / onSurface，保证与页面底色一致', () => {
    const { light, dark } = createColorSchemePair(SEED, 'tonalSpot')
    expect(deriveTitleBarColors(light, dark)).toEqual({
      lightTitleBarColor: light.surface,
      lightTitleBarTextColor: light['on-surface'],
      darkTitleBarColor: dark.surface,
      darkTitleBarTextColor: dark['on-surface']
    })
  })
})
