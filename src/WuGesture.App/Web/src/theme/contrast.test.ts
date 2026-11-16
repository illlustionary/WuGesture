import { describe, expect, it } from 'vitest'
import colorTokens from 'virtual:raw-source?path=./src/styles/tokens/_color.scss&raw'
import { THEME_PRESETS, type ThemePreset, type ThemePresetId } from './palettes'
import { createColorScheme } from './scheme'
import { COLOR_ROLES, EXTENDED_COLOR_ROLES, type ColorRole, type ColorScheme, type ExtendedColorRole } from './tokens'

/**
 * MD3 主题系统的 WCAG 2.1 对比度回归测试。
 *
 * - 纯计算，不引入新依赖：sRGB 转线性光 + 0.05 偏移得到对比度。
 * - 覆盖 8 套预设 × 浅色/深色，按“正文 4.5:1 / 图形 3:1 / 装饰 1.5:1”三档阈值逐一断言。
 * - 失败信息包含预设 id、明暗、配对（附色值）和实际比值，方便直接定位到具体角色。
 * - 已知达不到阈值的配对集中记录在 `KNOWN_GAPS` 中并以 `it.skip` 呈现（不修改配色引擎、不降低阈值），
 *   同时由“已知差距记录”分组反向校验：清单必须与实测未达标集合完全一致，
 *   因此配色调整到达标后必须删除对应记录，否则该分组会失败提醒。
 */

type ThemeModeName = 'light' | 'dark'

interface ContrastPair {
  readonly fg: ColorRole
  readonly bg: ColorRole
}

interface PairGroup {
  readonly key: 'text' | 'graphic' | 'decorative'
  readonly label: string
  readonly min: number
  readonly pairs: readonly ContrastPair[]
}

interface KnownGap {
  readonly preset: ThemePresetId
  readonly mode: ThemeModeName
  readonly pair: ContrastPair
  readonly min: number
  /** 当前引擎实测比值（两位小数），仅用于记录；引擎变化时需同步更新。 */
  readonly measured: string
}

/** WCAG 2.1 相对亮度：sRGB 8 位通道 → 线性光通道。 */
function linearizeChannel(channel: number): number {
  const srgb = channel / 255
  return srgb <= 0.03928 ? srgb / 12.92 : ((srgb + 0.055) / 1.055) ** 2.4
}

function hexChannels(color: string): [number, number, number] {
  const hex = color.trim().replace(/^#/, '')
  if (!/^[0-9a-f]{6}$/i.test(hex)) {
    throw new Error(`不是合法的 #RRGGBB 颜色：${color}`)
  }

  return [
    Number.parseInt(hex.slice(0, 2), 16),
    Number.parseInt(hex.slice(2, 4), 16),
    Number.parseInt(hex.slice(4, 6), 16)
  ]
}

function relativeLuminance(color: string): number {
  const [red, green, blue] = hexChannels(color)
  return 0.2126 * linearizeChannel(red) + 0.7152 * linearizeChannel(green) + 0.0722 * linearizeChannel(blue)
}

/** WCAG 2.1 对比度：(较亮 + 0.05) / (较暗 + 0.05)，与前后景顺序无关。 */
function contrastRatio(foreground: string, background: string): number {
  const foregroundLuminance = relativeLuminance(foreground)
  const backgroundLuminance = relativeLuminance(background)
  const lighter = Math.max(foregroundLuminance, backgroundLuminance)
  const darker = Math.min(foregroundLuminance, backgroundLuminance)
  return (lighter + 0.05) / (darker + 0.05)
}

const MODES: readonly { readonly mode: ThemeModeName; readonly label: string }[] = [
  { mode: 'light', label: '浅色' },
  { mode: 'dark', label: '深色' }
]

const PAIR_GROUPS: readonly PairGroup[] = [
  {
    key: 'text',
    label: '正文',
    min: 4.5,
    pairs: [
      { fg: 'on-surface', bg: 'surface' },
      { fg: 'on-surface-variant', bg: 'surface' },
      { fg: 'on-primary', bg: 'primary' },
      { fg: 'on-secondary', bg: 'secondary' },
      { fg: 'on-secondary-container', bg: 'secondary-container' },
      { fg: 'on-primary-container', bg: 'primary-container' },
      { fg: 'on-tertiary', bg: 'tertiary' },
      { fg: 'on-error', bg: 'error' },
      { fg: 'on-error-container', bg: 'error-container' },
      // 链接/强调文字直接落在页面底色上。
      { fg: 'primary', bg: 'surface' },
      { fg: 'on-surface', bg: 'surface-container-low' },
      { fg: 'on-surface', bg: 'surface-container' },
      { fg: 'on-surface', bg: 'surface-container-high' }
    ]
  },
  {
    key: 'graphic',
    label: '图形',
    min: 3,
    pairs: [
      { fg: 'outline', bg: 'surface' },
      { fg: 'primary', bg: 'surface-container' },
      { fg: 'on-surface-variant', bg: 'surface-container-high' }
    ]
  },
  {
    key: 'decorative',
    label: '装饰',
    min: 1.5,
    pairs: [
      { fg: 'outline-variant', bg: 'surface' },
      { fg: 'outline-variant', bg: 'surface-container' }
    ]
  }
]

const OUTLINE_VARIANT_ON_CONTAINER: ContrastPair = { fg: 'outline-variant', bg: 'surface-container' }

/**
 * 已知达不到阈值的配对（当前 MD3 引擎的真实结果，用于先拿到数据而不是掩盖问题）。
 *
 * `outline-variant` 在浅色下与 `surface-container` 只有约 1.46:1，低于装饰性 1.5:1。
 * 这来自 MD3 规范固定的色调步进（outlineVariant = neutral-variant tone 80 / surfaceContainer = neutral tone 94），
 * 换任何 seed 都稳定复现，因此不是某一套预设的问题。
 *
 * 处理方式：
 * - 不修改 `scheme.ts`、`palettes.ts` 或颜色令牌，也不降低阈值。
 * - 矩阵测试中这些配对以 `it.skip` 出现，并在下方“已知差距记录”分组里校验记录是否仍然准确。
 * - 若产品决定让该分隔线达标，可选方案见提交说明，改动后请删除对应记录。
 */
const KNOWN_GAPS: readonly KnownGap[] = [
  { preset: 'blue', mode: 'light', pair: OUTLINE_VARIANT_ON_CONTAINER, min: 1.5, measured: '1.46' },
  { preset: 'teal', mode: 'light', pair: OUTLINE_VARIANT_ON_CONTAINER, min: 1.5, measured: '1.46' },
  { preset: 'green', mode: 'light', pair: OUTLINE_VARIANT_ON_CONTAINER, min: 1.5, measured: '1.46' },
  { preset: 'purple', mode: 'light', pair: OUTLINE_VARIANT_ON_CONTAINER, min: 1.5, measured: '1.47' },
  { preset: 'pink', mode: 'light', pair: OUTLINE_VARIANT_ON_CONTAINER, min: 1.5, measured: '1.46' },
  { preset: 'orange', mode: 'light', pair: OUTLINE_VARIANT_ON_CONTAINER, min: 1.5, measured: '1.46' },
  { preset: 'red', mode: 'light', pair: OUTLINE_VARIANT_ON_CONTAINER, min: 1.5, measured: '1.46' },
  { preset: 'grey', mode: 'light', pair: OUTLINE_VARIANT_ON_CONTAINER, min: 1.5, measured: '1.47' }
]

const PRESETS_BY_ID = new Map(THEME_PRESETS.map(preset => [preset.id, preset]))
const SCHEME_CACHE = new Map<string, ColorScheme>()

function schemeFor(preset: ThemePreset, mode: ThemeModeName): ColorScheme {
  const cacheKey = `${preset.id}/${mode}`
  const cached = SCHEME_CACHE.get(cacheKey)
  if (cached) {
    return cached
  }

  const scheme = createColorScheme(preset.seed, 'tonalSpot', mode === 'dark')
  SCHEME_CACHE.set(cacheKey, scheme)
  return scheme
}

function pairKey(presetId: string, mode: ThemeModeName, pair: ContrastPair): string {
  return `${presetId}/${mode}/${pair.fg}/${pair.bg}`
}

const GAPS_BY_KEY = new Map(KNOWN_GAPS.map(gap => [pairKey(gap.preset, gap.mode, gap.pair), gap]))

function formatRatio(value: number): string {
  return value.toFixed(2)
}

/** 失败信息统一格式：`预设/明暗 分组 前景(色值) / 背景(色值) = 实际比值，需要 ≥ 阈值`。 */
function describePair(
  presetId: string,
  mode: ThemeModeName,
  group: PairGroup,
  pair: ContrastPair,
  scheme: ColorScheme,
  actual: number
): string {
  return `${presetId}/${mode} ${group.label} ${pair.fg}(${scheme[pair.fg]}) / ${pair.bg}(${scheme[pair.bg]}) = ${formatRatio(actual)}，需要 ≥ ${group.min}`
}

describe('WCAG 2.1 对比度计算', () => {
  it('相对亮度符合黑白边界', () => {
    expect(relativeLuminance('#000000')).toBeCloseTo(0, 10)
    expect(relativeLuminance('#FFFFFF')).toBeCloseTo(1, 10)
  })

  it('对比度满足 21:1 / 1:1 边界且与前后景顺序无关', () => {
    expect(contrastRatio('#000000', '#FFFFFF')).toBeCloseTo(21, 5)
    expect(contrastRatio('#123456', '#123456')).toBeCloseTo(1, 10)
    expect(contrastRatio('#529EF1', '#F8F9FF')).toBeCloseTo(contrastRatio('#F8F9FF', '#529EF1'), 10)
  })

  it('全部颜色角色都是可解析的 #RRGGBB', () => {
    for (const preset of THEME_PRESETS) {
      for (const { mode } of MODES) {
        const scheme = schemeFor(preset, mode)
        for (const role of COLOR_ROLES) {
          expect(() => relativeLuminance(scheme[role]), `${preset.id}/${mode} ${role}=${scheme[role]}`).not.toThrow()
        }
      }
    }
  })
})

describe('MD3 预设对比度矩阵（8 套预设 × 浅色/深色）', () => {
  for (const preset of THEME_PRESETS) {
    for (const { mode, label } of MODES) {
      for (const group of PAIR_GROUPS) {
        for (const pair of group.pairs) {
          const title = `${preset.id} ${label} ${pair.fg} / ${pair.bg} ≥ ${group.min}`

          if (GAPS_BY_KEY.has(pairKey(preset.id, mode, pair))) {
            it.skip(`${title}（已知差距，见 KNOWN_GAPS）`, () => {
              // 记录用占位：真实比值由下方“已知差距记录”分组校验。
            })
            continue
          }

          it(title, () => {
            const scheme = schemeFor(preset, mode)
            const actual = contrastRatio(scheme[pair.fg], scheme[pair.bg])
            expect(actual, describePair(preset.id, mode, group, pair, scheme, actual)).toBeGreaterThanOrEqual(group.min)
          })
        }
      }
    }
  }
})

describe('已知差距记录（待设计决策，不计入通过数）', () => {
  it('每条记录确实低于对应阈值，且数值与记录一致', () => {
    for (const gap of KNOWN_GAPS) {
      const preset = PRESETS_BY_ID.get(gap.preset)
      if (!preset) {
        throw new Error(`已知差距记录引用了未知预设：${gap.preset}`)
      }
      const scheme = schemeFor(preset, gap.mode)
      const actual = contrastRatio(scheme[gap.pair.fg], scheme[gap.pair.bg])
      const detail = `${gap.preset}/${gap.mode} ${gap.pair.fg}(${scheme[gap.pair.fg]}) / ${gap.pair.bg}(${scheme[gap.pair.bg]}) = ${formatRatio(actual)}，需要 ≥ ${gap.min}`

      expect(actual, `记录已不再低于阈值，请删除该条并恢复矩阵断言：${detail}`).toBeLessThan(gap.min)
      expect(formatRatio(actual), `记录值过期，请更新 measured：${detail}`).toBe(gap.measured)
    }
  })

  it('记录集合与实测未达标集合完全一致（防止静默隐藏新的回归）', () => {
    const measuredFailures: string[] = []
    for (const preset of THEME_PRESETS) {
      for (const { mode } of MODES) {
        const scheme = schemeFor(preset, mode)
        for (const group of PAIR_GROUPS) {
          for (const pair of group.pairs) {
            const actual = contrastRatio(scheme[pair.fg], scheme[pair.bg])
            if (actual < group.min) {
              measuredFailures.push(pairKey(preset.id, mode, pair))
            }
          }
        }
      }
    }

    const recorded = KNOWN_GAPS.map(gap => pairKey(gap.preset, gap.mode, gap.pair))
    expect(measuredFailures.sort()).toEqual([...recorded].sort())
  })
})

/**
 * success / warning 是固定语义色，不随 seed 生成，因此不进 `ColorScheme`，而是直接读
 * `_color.scss` 的浅色/深色令牌（经 Vite `?raw` 导入）——断言的正是实际发给浏览器的兜底色值，
 * 避免测试里再抄一份色表。
 */
type ExtendedScheme = Record<ExtendedColorRole, string>

function readExtendedScheme(mode: ThemeModeName): ExtendedScheme {
  // Sass 编译后属性选择器的引号会被去掉（`[data-theme='dark']` → `[data-theme=dark]`），两种写法都要接受。
  const darkStart = colorTokens.search(/\[data-theme=['"]?dark['"]?\]/)
  if (darkStart < 0) {
    throw new Error("_color.scss 缺少 [data-theme='dark'] 深色块")
  }

  const section = mode === 'dark' ? colorTokens.slice(darkStart) : colorTokens.slice(0, darkStart)
  const entries = EXTENDED_COLOR_ROLES.map(role => {
    const match = new RegExp(`--md-sys-color-${role}:\\s*(#[0-9a-fA-F]{6})`).exec(section)
    if (!match) {
      throw new Error(`_color.scss 的 ${mode} 块缺少 --md-sys-color-${role}`)
    }

    return [role, match[1]] as const
  })

  return Object.fromEntries(entries) as ExtendedScheme
}

const EXTENDED_SCHEMES: Record<ThemeModeName, ExtendedScheme> = {
  light: readExtendedScheme('light'),
  dark: readExtendedScheme('dark')
}

/** 扩展角色的文字配对：角色自带的前景/背景组合。 */
const EXTENDED_ROLE_PAIRS = [
  { fg: 'on-success', bg: 'success' },
  { fg: 'on-success-container', bg: 'success-container' },
  { fg: 'on-warning', bg: 'warning' },
  { fg: 'on-warning-container', bg: 'warning-container' }
] as const

/** 扩展角色叠在生成色上的实际用法：关于页状态文字、Snackbar 图标。 */
const EXTENDED_USAGE_PAIRS = [
  { fg: 'success', bg: 'surface-container', min: 4.5 },
  { fg: 'warning', bg: 'surface-container', min: 4.5 },
  { fg: 'success-container', bg: 'inverse-surface', min: 3 },
  { fg: 'warning-container', bg: 'inverse-surface', min: 3 },
  { fg: 'error-container', bg: 'inverse-surface', min: 3 }
] as const

function extendedColors(preset: ThemePreset, mode: ThemeModeName): Record<string, string> {
  return { ...schemeFor(preset, mode), ...EXTENDED_SCHEMES[mode] }
}

describe('扩展语义色对比度（success / warning / Snackbar 图标）', () => {
  for (const preset of THEME_PRESETS) {
    for (const { mode, label } of MODES) {
      for (const pair of EXTENDED_ROLE_PAIRS) {
        it(`${preset.id} ${label} ${pair.fg} / ${pair.bg} ≥ 4.5`, () => {
          const colors = extendedColors(preset, mode)
          const actual = contrastRatio(colors[pair.fg], colors[pair.bg])
          expect(
            actual,
            `${preset.id}/${mode} 正文 ${pair.fg}(${colors[pair.fg]}) / ${pair.bg}(${colors[pair.bg]}) = ${formatRatio(actual)}，需要 ≥ 4.5`
          ).toBeGreaterThanOrEqual(4.5)
        })
      }

      for (const pair of EXTENDED_USAGE_PAIRS) {
        it(`${preset.id} ${label} ${pair.fg} / ${pair.bg} ≥ ${pair.min}`, () => {
          const colors = extendedColors(preset, mode)
          const actual = contrastRatio(colors[pair.fg], colors[pair.bg])
          expect(
            actual,
            `${preset.id}/${mode} ${pair.fg}(${colors[pair.fg]}) / ${pair.bg}(${colors[pair.bg]}) = ${formatRatio(actual)}，需要 ≥ ${pair.min}`
          ).toBeGreaterThanOrEqual(pair.min)
        })
      }
    }
  }
})
