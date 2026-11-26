import { describe, expect, it } from 'vitest'
import { buildEdgeScreenHotspots, findEdgeScreenAction } from './edgeScreenHotspots'

describe('buildEdgeScreenHotspots', () => {
  it('触发角返回四个角热区', () => {
    const spots = buildEdgeScreenHotspots('corner')

    expect(spots).toHaveLength(4)
    expect(spots.map(spot => spot.label)).toEqual(['左上角', '右上角', '左下角', '右下角'])
    expect(new Set(spots.map(spot => spot.key)).size).toBe(4)
    expect(spots.every(spot => spot.side === spot.location)).toBe(true)
    expect(spots.every(spot => spot.wheelDirection === '')).toBe(true)
  })

  it('摩擦边返回四条边热区', () => {
    const spots = buildEdgeScreenHotspots('friction')

    expect(spots).toHaveLength(4)
    expect(spots.map(spot => spot.label)).toEqual(['左边', '右边', '上边', '下边'])
    expect(new Set(spots.map(spot => spot.key)).size).toBe(4)
  })

  it('边缘滚动返回四条边 × 两个方向', () => {
    const spots = buildEdgeScreenHotspots('wheel')

    expect(spots).toHaveLength(8)
    expect(spots.filter(spot => spot.wheelDirection === 'up')).toHaveLength(4)
    expect(spots.filter(spot => spot.wheelDirection === 'down')).toHaveLength(4)
    expect(spots.filter(spot => spot.wheelLabel === '滚轮上')).toHaveLength(4)
    expect(new Set(spots.map(spot => spot.key)).size).toBe(8)
  })
})

describe('findEdgeScreenAction', () => {
  it('按触发类型、位置和滚轮方向匹配动作', () => {
    const actions = [
      { triggerType: 'wheel', location: 'left', wheelDirection: 'up', enabled: true },
      { triggerType: 'wheel', location: 'left', wheelDirection: 'down', enabled: false }
    ]
    const spots = buildEdgeScreenHotspots('wheel')
    const up = spots.find(spot => spot.location === 'left' && spot.wheelDirection === 'up')
    const down = spots.find(spot => spot.location === 'left' && spot.wheelDirection === 'down')

    expect(findEdgeScreenAction(actions, up)).toBe(actions[0])
    expect(findEdgeScreenAction(actions, down)).toBe(actions[1])
  })

  it('触发角把缺省滚轮方向视为空字符串', () => {
    const actions = [{ triggerType: 'corner', location: 'top-left', enabled: true }]
    const spot = buildEdgeScreenHotspots('corner')[0]

    expect(findEdgeScreenAction(actions, spot)).toBe(actions[0])
  })
})
