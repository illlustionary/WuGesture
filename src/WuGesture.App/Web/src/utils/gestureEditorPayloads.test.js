import { describe, expect, it } from 'vitest'
import {
  buildConfigPayload,
  getWebDavSignature,
  toPayloadAction,
  toPayloadApplication,
  toPayloadEdgeAction,
  toPayloadRule,
  toPayloadUiSettings
} from './gestureEditorPayloads'

describe('toPayloadRule', () => {
  it('拼装 scope、鼠标键和手势方向', () => {
    const payload = toPayloadRule({
      scopeKind: 'category',
      scopeName: ' 工具 ',
      mouseButton: 'MIDDLE',
      patternText: 'Down, Right',
      keysText: 'Alt + Left',
      actionType: 'hotkey'
    })

    expect(payload).toMatchObject({
      scope: 'category:工具',
      mouseButton: 'middle',
      pattern: ['Down', 'Right'],
      action: { type: 'hotkey', keys: ['Alt', 'Left'] }
    })
  })

  it('动作名称为空时使用手势助记符', () => {
    expect(toPayloadRule({ patternText: 'Left', mouseButton: 'right', actionType: 'hotkey' }).actionName).toBe('◑←')
    expect(toPayloadRule({ patternText: '', mouseButton: 'right', actionType: 'hotkey' }).actionName).toBe('')
  })

  it('保留显式动作名称并去除空白', () => {
    expect(toPayloadRule({ actionName: ' 最大化 ', patternText: 'Up' }).actionName).toBe('最大化')
  })
})

describe('toPayloadAction', () => {
  it('窗口操作回退到最大化/还原', () => {
    const action = toPayloadAction({ actionType: 'window', windowOperation: 'explode' })

    expect(action).toEqual({ type: 'window', operation: 'toggle-maximize' })
  })

  it('音量与亮度动作带上收敛后的步进值', () => {
    expect(toPayloadAction({ actionType: 'volume', volumeOperation: 'DECREASE', amount: 0 })).toEqual({
      type: 'volume',
      operation: 'decrease',
      amount: 5
    })
    expect(toPayloadAction({ actionType: 'brightness', brightnessOperation: 'decrease', amount: 250 })).toEqual({
      type: 'brightness',
      operation: 'decrease',
      amount: 100
    })
  })

  it('运行程序动作裁剪路径和参数', () => {
    expect(
      toPayloadAction({
        actionType: 'program',
        programPath: '  C:\\Tools\\a.exe ',
        programArguments: [' --flag ', '', '   ']
      })
    ).toEqual({ type: 'program', path: 'C:\\Tools\\a.exe', arguments: ['--flag'] })
  })

  it('快捷键动作拆分按键文本', () => {
    expect(toPayloadAction({ actionType: 'hotkey', keysText: 'Control + W' })).toEqual({
      type: 'hotkey',
      keys: ['Control', 'W']
    })
  })
})

describe('toPayloadApplication', () => {
  it('裁剪名称、路径和分类', () => {
    const payload = toPayloadApplication({
      name: ' code.exe ',
      displayName: ' ',
      path: ' C:\\Code\\Code.exe ',
      categories: [' 开发 ', '', '工具']
    })

    expect(payload).toEqual({
      name: 'code.exe',
      displayName: 'code.exe',
      path: 'C:\\Code\\Code.exe',
      categories: ['开发', '工具']
    })
  })
})

describe('toPayloadEdgeAction', () => {
  it('滚轮操作保留方向，程序动作退化为快捷键', () => {
    const wheel = toPayloadEdgeAction({
      triggerType: 'wheel',
      location: 'left',
      wheelDirection: 'DOWN',
      amount: 0,
      action: { type: 'volume', operation: 'decrease', amount: 0 }
    })

    expect(wheel).toEqual({
      enabled: false,
      triggerType: 'wheel',
      location: 'left',
      wheelDirection: 'down',
      frictionCount: 4,
      action: { type: 'volume', operation: 'decrease', amount: 5 }
    })

    const corner = toPayloadEdgeAction({
      triggerType: 'corner',
      location: 'top-left',
      wheelDirection: 'down',
      action: { type: 'program', path: 'C:\\a.exe' }
    })

    expect(corner.wheelDirection).toBe('')
    expect(corner.action).toEqual({ type: 'hotkey', keys: [] })
  })
})

describe('toPayloadUiSettings', () => {
  it('排除项只保留宿主需要的字段并完成归一化', () => {
    const payload = toPayloadUiSettings({
      appBehavior: {
        excludedApplications: [
          { name: 'notepad.exe', path: ' C:\\Windows\\notepad.exe ', displayName: ' ', icon: 'data:image/png' }
        ]
      }
    })

    expect(payload.appBehavior.excludedApplications).toEqual([
      {
        name: 'notepad',
        displayName: 'notepad',
        path: 'C:\\Windows\\notepad.exe',
        disableEdgeActions: false
      }
    ])
    expect(payload.appBehavior.excludedApplications[0]).not.toHaveProperty('icon')
  })
})

describe('buildConfigPayload', () => {
  it('组装完整配置并去重分类', () => {
    const payload = buildConfigPayload({
      rules: [{ patternText: 'Left', actionType: 'hotkey', keysText: 'Alt + Left', actionName: '返回' }],
      applications: [{ name: ' code ', displayName: '', path: '', categories: [] }],
      categories: [' 工具 ', '', '工具'],
      edgeActions: [{ triggerType: 'corner', location: 'top-left', action: { type: 'hotkey', keys: ['Win'] } }],
      uiSettings: {}
    })

    expect(payload.rules).toHaveLength(1)
    expect(payload.applications[0]).toMatchObject({ name: 'code', displayName: 'code' })
    expect(payload.categories).toEqual(['工具'])
    expect(payload.edgeActions[0]).toMatchObject({ triggerType: 'corner', location: 'top-left' })
    expect(payload.uiSettings.appBehavior).toBeDefined()
  })
})

describe('getWebDavSignature', () => {
  it('忽略空白差异但保留密码原样', () => {
    const first = getWebDavSignature({ address: ' https://dav.example.com ', password: ' secret ' })
    const second = getWebDavSignature({ address: 'https://dav.example.com', password: ' secret ' })

    expect(first).toBe(second)
    expect(first).not.toBe(getWebDavSignature({ address: 'https://dav.example.com', password: 'secret' }))
  })
})
