export const MOUSE_BUTTON_SYMBOLS = {
  right: "◑",
  middle: "●"
};

export const MOUSE_BUTTONS = {
  right: "right",
  middle: "middle"
};

export const SCOPE_KINDS = {
  global: "global",
  category: "category",
  app: "app"
};

export const ACTION_TYPES = {
  hotkey: "hotkey",
  window: "window",
  volume: "volume",
  brightness: "brightness"
};

export const OPERATIONS = {
  toggleTopmost: "toggle-topmost",
  toggleMaximize: "toggle-maximize",
  minimize: "minimize",
  close: "close",
  increase: "increase",
  decrease: "decrease",
  mute: "mute"
};

export const EDGE_TRIGGER_TYPES = {
  corner: "corner",
  friction: "friction",
  wheel: "wheel"
};

export const WHEEL_DIRECTIONS = {
  up: "up",
  down: "down"
};

export const CLOSE_BUTTON_BEHAVIORS = {
  minimizeToTray: "minimize-to-tray",
  minimizeToTaskbar: "minimize-to-taskbar",
  exit: "exit"
};

export const WEBVIEW_MESSAGE_TYPES = {
  getStatus: "get-status",
  selectApplication: "select-application",
  pickApplicationWindow: "pick-application-window",
  startGestureRecording: "start-gesture-recording",
  stopGestureRecording: "stop-gesture-recording",
  setGesturePaused: "set-gesture-paused",
  startHotkeyRecording: "start-hotkey-recording",
  stopHotkeyRecording: "stop-hotkey-recording",
  saveRules: "save-rules",
  webDavTest: "webdav-test",
  webDavSave: "webdav-save",
  webDavRestore: "webdav-restore",
  reloadRules: "reload-rules",
  resetRules: "reset-rules",
  status: "status",
  rules: "rules",
  configResult: "config-result",
  webDavResult: "webdav-result",
  applicationSelected: "application-selected",
  gestureRecorded: "gesture-recorded",
  hotkeyRecorded: "hotkey-recorded"
};

export const DIRECTION_SYMBOLS = {
  Up: "↑",
  Down: "↓",
  Left: "←",
  Right: "→",
  UpLeft: "↖",
  UpRight: "↗",
  DownLeft: "↙",
  DownRight: "↘"
};

export const WINDOW_OPERATIONS = [
  { value: OPERATIONS.toggleTopmost, label: "置顶/取消置顶" },
  { value: OPERATIONS.toggleMaximize, label: "最大化/还原" },
  { value: OPERATIONS.minimize, label: "最小化" },
  { value: OPERATIONS.close, label: "关闭窗口" }
];

export const VOLUME_OPERATIONS = [
  { value: OPERATIONS.increase, label: "音量 +" },
  { value: OPERATIONS.decrease, label: "音量 -" },
  { value: OPERATIONS.mute, label: "静音" }
];

export const BRIGHTNESS_OPERATIONS = [
  { value: OPERATIONS.increase, label: "亮度 +" },
  { value: OPERATIONS.decrease, label: "亮度 -" }
];

export const EDGE_LOCATIONS = {
  corner: [
    { value: "top-left", label: "左上角" },
    { value: "top-right", label: "右上角" },
    { value: "bottom-left", label: "左下角" },
    { value: "bottom-right", label: "右下角" }
  ],
  edge: [
    { value: "left", label: "左边" },
    { value: "right", label: "右边" },
    { value: "top", label: "上边" },
    { value: "bottom", label: "下边" }
  ]
};

export const ACTION_TYPE_OPTIONS = [
  { value: ACTION_TYPES.hotkey, label: "快捷键" },
  { value: ACTION_TYPES.window, label: "窗口控制" },
  { value: ACTION_TYPES.volume, label: "音量控制" },
  { value: ACTION_TYPES.brightness, label: "亮度控制" }
];

export const CLOSE_BUTTON_BEHAVIOR_OPTIONS = [
  { value: CLOSE_BUTTON_BEHAVIORS.minimizeToTray, label: "最小化到托盘" },
  { value: CLOSE_BUTTON_BEHAVIORS.minimizeToTaskbar, label: "最小化到任务栏" },
  { value: CLOSE_BUTTON_BEHAVIORS.exit, label: "直接关闭" }
];
