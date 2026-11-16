import type { HexColor } from './tokens'

/**
 * 预设色板（精简 8 套）。
 *
 * 配置里保存的是 seed 色值而不是索引，因此后续增删预设不会影响已有配置。
 * 阶段 6 的配色引擎会以 seed + schemeVariant 生成完整角色色板。
 */
export type ThemePresetId = 'blue' | 'teal' | 'green' | 'purple' | 'pink' | 'orange' | 'red' | 'grey'

export interface ThemePreset {
  readonly id: ThemePresetId
  readonly label: string
  readonly seed: HexColor
}

export const THEME_PRESETS = [
  { id: 'blue', label: 'WuGesture 蓝', seed: '#529EF1' },
  { id: 'teal', label: '青', seed: '#009688' },
  { id: 'green', label: '绿', seed: '#43A047' },
  { id: 'purple', label: '紫', seed: '#7E57C2' },
  { id: 'pink', label: '粉', seed: '#FF7299' },
  { id: 'orange', label: '橙', seed: '#FB8C00' },
  { id: 'red', label: '红', seed: '#E53935' },
  { id: 'grey', label: '灰', seed: '#757575' }
] as const satisfies readonly ThemePreset[]

/** 默认配色：WuGesture 蓝。 */
export const DEFAULT_PRESET_ID: ThemePresetId = 'blue'

export const DEFAULT_SEED_COLOR: HexColor = '#529EF1'

/** 按 seed 查找预设，用于判断当前 seed 是否命中预设色板。 */
export function findPresetBySeed(seed: string): ThemePreset | undefined {
  const normalized = seed.trim().toUpperCase()
  return THEME_PRESETS.find(preset => preset.seed.toUpperCase() === normalized)
}
