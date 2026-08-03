import {
  ACTION_TYPES,
  CLOSE_BUTTON_BEHAVIORS,
  EDGE_LOCATIONS,
  EDGE_TRIGGER_TYPES,
  OPERATIONS,
  WINDOW_TARGET_MODES,
  WHEEL_DIRECTIONS
} from './gestureEditorOptions'

export const DEFAULT_UI_SETTINGS = {
  appearance: {
    theme: 'dark',
    lightTitleBarColor: '#FFFFFF',
    lightTitleBarTextColor: '#16202B',
    darkTitleBarColor: '#1B222B',
    darkTitleBarTextColor: '#EDF2F7'
  },
  sidebar: {
    collapsed: true
  },
  mouseTrail: {
    enabled: true,
    inactiveColor: '#BDBDBD',
    activeColor: '#87CEEB',
    inactiveThickness: 3,
    activeThickness: 3,
    inactiveOpacity: 74,
    activeOpacity: 100
  },
  gestureHint: {
    enabled: true,
    displayDurationMs: 300,
    fadeDurationMs: 240,
    fontFamily: 'Segoe UI Semibold',
    fontSize: 22,
    textColor: '#FFFFFF',
    backgroundColor: '#12181F',
    backgroundOpacity: 90,
    widthPercent: 10,
    autoWidth: true,
    heightPercent: 5,
    cornerRadius: 28,
    bottomOffsetPercent: 6
  },
  levelOsd: {
    enabled: true,
    displayDurationMs: 1800,
    fadeDurationMs: 240,
    backgroundColor: '#28282C',
    backgroundOpacity: 88,
    textColor: '#DCDCDC',
    trackColor: '#464646',
    volumeColor: '#64C8FF',
    brightnessColor: '#FFC828',
    width: 210,
    height: 190,
    cornerRadius: 22,
    position: 'center',
    offsetX: 0,
    offsetY: 0
  },
  gestureSensitivity: {
    percent: 110
  },
  appBehavior: {
    launchAtStartup: true,
    showConfigWindowOnLaunch: true,
    runAsAdministrator: false,
    disableGesturesInFullscreen: false,
    disableEdgeActionsInFullscreen: false,
    closeButtonBehavior: CLOSE_BUTTON_BEHAVIORS.minimizeToTray,
    targetWindowMode: WINDOW_TARGET_MODES.startWindow,
    excludedApplications: []
  },
  webDav: {
    address: '',
    userName: '',
    password: '',
    remotePath: ''
  }
}

export const DEFAULT_EDGE_ACTIONS = [
  createDefaultEdgeAction(EDGE_TRIGGER_TYPES.corner, 'top-left', '', {
    enabled: true,
    keysText: 'Win + Tab'
  }),
  createDefaultEdgeAction(EDGE_TRIGGER_TYPES.corner, 'top-right', '', {
    enabled: true,
    keysText: 'Win + Tab'
  }),
  createDefaultEdgeAction(EDGE_TRIGGER_TYPES.corner, 'bottom-left', '', {
    enabled: true,
    keysText: 'Win'
  }),
  createDefaultEdgeAction(EDGE_TRIGGER_TYPES.corner, 'bottom-right'),
  ...EDGE_LOCATIONS.edge.map(edge =>
    createDefaultEdgeAction(EDGE_TRIGGER_TYPES.friction, edge.value, '', {
      enabled: edge.value === 'right',
      keysText:
        edge.value === 'left' || edge.value === 'right'
          ? 'Control + Shift + Escape'
          : edge.value === 'top'
            ? 'M + I + K + U'
            : ''
    })
  ),
  ...EDGE_LOCATIONS.edge.flatMap(edge => [
    createDefaultEdgeAction(EDGE_TRIGGER_TYPES.wheel, edge.value, WHEEL_DIRECTIONS.up, {
      enabled: edge.value === 'right' || edge.value === 'top',
      actionType:
        edge.value === 'right'
          ? ACTION_TYPES.volume
          : edge.value === 'top'
            ? ACTION_TYPES.brightness
            : ACTION_TYPES.hotkey,
      volumeOperation: OPERATIONS.increase,
      brightnessOperation: OPERATIONS.increase,
      amount: edge.value === 'right' ? 2 : 5
    }),
    createDefaultEdgeAction(EDGE_TRIGGER_TYPES.wheel, edge.value, WHEEL_DIRECTIONS.down, {
      enabled: edge.value === 'right' || edge.value === 'top',
      actionType:
        edge.value === 'right'
          ? ACTION_TYPES.volume
          : edge.value === 'top'
            ? ACTION_TYPES.brightness
            : ACTION_TYPES.hotkey,
      volumeOperation: OPERATIONS.decrease,
      brightnessOperation: OPERATIONS.decrease,
      amount: edge.value === 'right' ? 2 : 5
    })
  ])
]

export function createDefaultEdgeAction(triggerType, location, wheelDirection = '', overrides = {}) {
  return {
    enabled: false,
    triggerType,
    location,
    wheelDirection,
    frictionCount: 4,
    keysText: '',
    actionType: ACTION_TYPES.hotkey,
    windowOperation: OPERATIONS.toggleMaximize,
    volumeOperation: OPERATIONS.increase,
    brightnessOperation: OPERATIONS.increase,
    amount: 5,
    ...overrides
  }
}
