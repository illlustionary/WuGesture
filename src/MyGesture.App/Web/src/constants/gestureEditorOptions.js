export const MOUSE_BUTTON_SYMBOLS = {
  right: "◑",
  middle: "●"
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
  { value: "toggle-topmost", label: "置顶/取消置顶" },
  { value: "toggle-maximize", label: "最大化/还原" },
  { value: "minimize", label: "最小化" },
  { value: "close", label: "关闭窗口" }
];

export const VOLUME_OPERATIONS = [
  { value: "increase", label: "音量 +" },
  { value: "decrease", label: "音量 -" },
  { value: "mute", label: "静音" }
];

export const BRIGHTNESS_OPERATIONS = [
  { value: "increase", label: "亮度 +" },
  { value: "decrease", label: "亮度 -" }
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
