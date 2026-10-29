import { describe, expect, it } from 'vitest'
import { createEmptyGestureDraft, createRuleModel, toViewApplication, toViewRule } from './gestureEditorViewModels'

describe('toViewRule', () => {
  it('把宿主规则转换成编辑器模型', () => {
    const rule = toViewRule(
      {
        scope: 'app:Code.exe',
        pattern: ['Down', 'Right'],
        mouseButton: 'MIDDLE',
        actionName: '关闭窗口',
        keys: ['Alt', 'F4'],
        actionType: 'VOLUME',
        operation: 'decrease',
        amount: 30,
        path: ' C:\\Tools\\a.exe ',
        arguments: [' --flag ', '', '  '],
        programName: ' a.exe ',
        programMissing: 1
      },
      12
    )

    expect(rule).toMatchObject({
      id: 12,
      scopeKind: 'app',
      scopeName: 'Code.exe',
      patternText: 'Down, Right',
      mouseButton: 'middle',
      actionName: '关闭窗口',
      keysText: 'Alt + F4',
      actionType: 'volume',
      volumeOperation: 'decrease',
      brightnessOperation: 'decrease',
      windowOperation: 'toggle-maximize',
      amount: 30,
      programPath: 'C:\\Tools\\a.exe',
      programArguments: ['--flag'],
      programName: 'a.exe',
      programMissing: true
    })
  })

  it('缺失字段使用默认值和空文本', () => {
    const rule = toViewRule({}, 1)

    expect(rule).toMatchObject({
      scopeKind: 'global',
      scopeName: '',
      patternText: '',
      keysText: '',
      actionType: 'hotkey',
      amount: 5,
      programArguments: [],
      programMissing: false
    })
  })
})

describe('toViewApplication', () => {
  it('裁剪字段并去重分类', () => {
    const application = toViewApplication({
      name: ' code ',
      displayName: ' ',
      path: ' C:\\Code ',
      categories: [' 开发 ', '开发', '', null],
      icon: ' data:image/png;base64,aa '
    })

    expect(application).toEqual({
      name: 'code',
      displayName: 'code',
      path: 'C:\\Code',
      categories: ['开发'],
      icon: 'data:image/png;base64,aa'
    })
  })
})

describe('createRuleModel', () => {
  it('默认使用 Left 手势和对应助记符', () => {
    const model = createRuleModel('global', '', {}, 3)

    expect(model).toMatchObject({
      id: 3,
      scopeKind: 'global',
      patternText: 'Left',
      mouseButton: 'right',
      actionName: '◑←',
      actionType: 'hotkey',
      amount: 5
    })
  })

  it('保留传入的动作名称并归一化其余字段', () => {
    const model = createRuleModel(
      'category',
      '工具',
      {
        patternText: ' Up ',
        mouseButton: 'MIDDLE',
        actionName: '最大化',
        actionType: 'window',
        windowOperation: ' MINIMIZE ',
        amount: 0,
        programPath: ' C:\\a.exe '
      },
      4
    )

    expect(model).toMatchObject({
      scopeKind: 'category',
      scopeName: '工具',
      patternText: ' Up ',
      actionName: '最大化',
      actionType: 'window',
      windowOperation: 'minimize',
      amount: 5,
      programPath: 'C:\\a.exe'
    })
  })
})

describe('createEmptyGestureDraft', () => {
  it('返回可直接编辑的默认草稿', () => {
    expect(createEmptyGestureDraft()).toEqual({
      actionName: '',
      patternText: '',
      mouseButton: 'right',
      keysText: '',
      actionType: 'hotkey',
      windowOperation: 'toggle-maximize',
      volumeOperation: 'increase',
      brightnessOperation: 'increase',
      amount: 5,
      programPath: '',
      programArguments: [],
      programName: '',
      programIcon: '',
      programMissing: false
    })
  })
})
