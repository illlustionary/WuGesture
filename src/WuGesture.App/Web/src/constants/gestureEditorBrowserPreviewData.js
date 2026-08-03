import { ACTION_TYPES, OPERATIONS } from './gestureEditorOptions'

export const PREVIEW_RULES = [
  {
    scope: 'global',
    pattern: ['Left'],
    actionName: '返回',
    actionType: ACTION_TYPES.hotkey,
    keys: ['Alt', 'Left']
  },
  {
    scope: 'global',
    pattern: ['Right'],
    actionName: '前进',
    actionType: ACTION_TYPES.hotkey,
    keys: ['Alt', 'Right']
  },
  {
    scope: 'global',
    pattern: ['Down', 'Right'],
    actionName: '关闭窗口',
    actionType: ACTION_TYPES.window,
    windowOperation: OPERATIONS.close
  },
  {
    scope: 'global',
    pattern: ['DownLeft'],
    actionName: '最小化',
    actionType: ACTION_TYPES.window,
    windowOperation: OPERATIONS.minimize
  },
  {
    scope: 'global',
    pattern: ['Down', 'Left'],
    actionName: 'Enter',
    actionType: ACTION_TYPES.hotkey,
    keys: ['Enter']
  },
  {
    scope: 'global',
    pattern: ['UpRight'],
    actionName: '最大化',
    actionType: ACTION_TYPES.window,
    windowOperation: OPERATIONS.toggleMaximize
  },
  {
    scope: 'category:浏览器和编辑器',
    pattern: ['Left'],
    actionName: '返回',
    actionType: ACTION_TYPES.hotkey,
    keys: ['Alt', 'Left']
  },
  {
    scope: 'category:浏览器和编辑器',
    pattern: ['Right'],
    actionName: '前进',
    actionType: ACTION_TYPES.hotkey,
    keys: ['Alt', 'Right']
  },
  {
    scope: 'category:浏览器和编辑器',
    pattern: ['Down', 'Right'],
    actionName: '关闭一个',
    actionType: ACTION_TYPES.hotkey,
    keys: ['Control', 'W']
  },
  {
    scope: 'category:浏览器和编辑器',
    mouseButton: 'middle',
    pattern: ['Down', 'Right'],
    actionName: '全部关闭',
    actionType: ACTION_TYPES.window,
    windowOperation: OPERATIONS.close
  }
]

export const PREVIEW_APPLICATIONS = [
  {
    name: 'msedge.exe',
    displayName: 'Microsoft Edge',
    path: '',
    categories: ['浏览器和编辑器'],
    icon: ''
  },
  {
    name: 'code.exe',
    displayName: 'Visual Studio Code',
    path: '',
    categories: ['浏览器和编辑器'],
    icon: ''
  }
]
