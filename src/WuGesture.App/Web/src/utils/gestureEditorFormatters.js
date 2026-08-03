import {
  ACTION_TYPES,
  BRIGHTNESS_OPERATIONS,
  DIRECTION_SYMBOLS,
  EDGE_LOCATIONS,
  EDGE_TRIGGER_TYPES,
  MOUSE_BUTTON_SYMBOLS,
  OPERATIONS,
  VOLUME_OPERATIONS,
  WHEEL_DIRECTIONS,
  WINDOW_OPERATIONS
} from '@/constants/gestureEditorOptions'
import {
  normalizeActionType,
  normalizeAmount,
  normalizeBrightnessOperation,
  normalizeEdgeTriggerType,
  normalizeMouseButton,
  normalizeVolumeOperation,
  normalizeWindowOperation,
  parsePattern
} from './gestureEditorNormalizers'

export function getGestureMnemonic(source) {
  const pattern = Array.isArray(source?.pattern) ? source.pattern : parsePattern(source?.patternText)
  if (pattern.length === 0) {
    return ''
  }

  const button = MOUSE_BUTTON_SYMBOLS[normalizeMouseButton(source?.mouseButton)] ?? MOUSE_BUTTON_SYMBOLS.right
  const directions = pattern.map(direction => DIRECTION_SYMBOLS[direction] ?? '').join('')
  return directions ? `${button}${directions}` : ''
}

export function getActionLabel(rule) {
  const actionType = normalizeActionType(rule?.actionType)
  if (actionType === ACTION_TYPES.window) {
    const operation = WINDOW_OPERATIONS.find(item => item.value === normalizeWindowOperation(rule?.windowOperation))
    return operation ? `窗口控制：${operation.label}` : '窗口控制'
  }

  if (actionType === ACTION_TYPES.volume) {
    const operation = VOLUME_OPERATIONS.find(item => item.value === normalizeVolumeOperation(rule?.volumeOperation))
    return operation?.value === OPERATIONS.mute
      ? '音量控制：静音'
      : `音量控制：${operation?.label ?? '音量 +'} ${normalizeAmount(rule?.amount)}`
  }

  if (actionType === ACTION_TYPES.brightness) {
    const operation = BRIGHTNESS_OPERATIONS.find(
      item => item.value === normalizeBrightnessOperation(rule?.brightnessOperation)
    )
    return `亮度控制：${operation?.label ?? '亮度 +'} ${normalizeAmount(rule?.amount)}`
  }

  return rule?.keysText || '点击设置'
}

export function getEdgeActionLabel(action) {
  const triggerLabel = {
    [EDGE_TRIGGER_TYPES.corner]: '触发角',
    [EDGE_TRIGGER_TYPES.friction]: '摩擦边',
    [EDGE_TRIGGER_TYPES.wheel]: '边缘滚动'
  }[normalizeEdgeTriggerType(action?.triggerType)]
  const allLocations = [...EDGE_LOCATIONS.corner, ...EDGE_LOCATIONS.edge]
  const location = allLocations.find(item => item.value === action?.location)?.label ?? ''
  const wheel = action?.wheelDirection ? (action.wheelDirection === WHEEL_DIRECTIONS.down ? '滚轮下' : '滚轮上') : ''
  return [triggerLabel, location, wheel].filter(Boolean).join(' ')
}
