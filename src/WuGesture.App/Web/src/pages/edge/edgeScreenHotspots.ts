import { EDGE_LOCATIONS } from '@/constants/gestureEditorOptions'

export type EdgeScreenTriggerType = 'corner' | 'friction' | 'wheel'
export type EdgeScreenSide =
  'top-left' | 'top-right' | 'bottom-left' | 'bottom-right' | 'left' | 'right' | 'top' | 'bottom'
export type EdgeScreenWheelDirection = '' | 'up' | 'down'

export interface EdgeScreenHotspot {
  key: string
  triggerType: EdgeScreenTriggerType
  location: EdgeScreenSide
  side: EdgeScreenSide
  wheelDirection: EdgeScreenWheelDirection
  label: string
  wheelLabel: string
}

export interface EdgeScreenActionLike {
  triggerType: string
  location: string
  wheelDirection?: string
  enabled: boolean
  frictionCount?: number
}

const CORNER_SIDES: EdgeScreenSide[] = ['top-left', 'top-right', 'bottom-left', 'bottom-right']
const EDGE_SIDES: EdgeScreenSide[] = ['left', 'right', 'top', 'bottom']

const locationLabels = new Map<string, string>(
  [...EDGE_LOCATIONS.corner, ...EDGE_LOCATIONS.edge].map(item => [item.value, item.label])
)

function locationLabel(side: EdgeScreenSide) {
  return locationLabels.get(side) ?? side
}

/**
 * 屏幕示意图上的点击热区。三种边缘操作共用同一张图，但热区数量和形态不同：
 * 触发角 4 个角，摩擦边 4 条边，边缘滚动 4 条边 × 上下两个方向。
 */
export function buildEdgeScreenHotspots(triggerType: EdgeScreenTriggerType): EdgeScreenHotspot[] {
  if (triggerType === 'corner') {
    return CORNER_SIDES.map(side => ({
      key: `corner:${side}`,
      triggerType,
      location: side,
      side,
      wheelDirection: '',
      label: locationLabel(side),
      wheelLabel: ''
    }))
  }

  if (triggerType === 'friction') {
    return EDGE_SIDES.map(side => ({
      key: `friction:${side}`,
      triggerType,
      location: side,
      side,
      wheelDirection: '',
      label: locationLabel(side),
      wheelLabel: ''
    }))
  }

  return EDGE_SIDES.flatMap(side =>
    (['up', 'down'] as const).map(wheelDirection => ({
      key: `wheel:${side}:${wheelDirection}`,
      triggerType,
      location: side,
      side,
      wheelDirection,
      label: locationLabel(side),
      wheelLabel: wheelDirection === 'down' ? '滚轮下' : '滚轮上'
    }))
  )
}

export function findEdgeScreenAction<T extends EdgeScreenActionLike>(
  actions: readonly T[],
  hotspot: EdgeScreenHotspot
): T | undefined {
  return actions.find(
    action =>
      action.triggerType === hotspot.triggerType &&
      action.location === hotspot.location &&
      (action.wheelDirection || '') === hotspot.wheelDirection
  )
}
