import {
  ACTION_TYPES,
  CLOSE_BUTTON_BEHAVIORS,
  EDGE_LOCATIONS,
  EDGE_TRIGGER_TYPES,
  OPERATIONS,
  SCOPE_KINDS,
  WHEEL_DIRECTIONS
} from "./gestureEditorOptions";

export const DEFAULT_RULES = [
  {
    scope: SCOPE_KINDS.global,
    pattern: ["Left"],
    actionName: "Back",
    actionType: ACTION_TYPES.hotkey,
    keys: ["Alt", "Left"]
  },
  {
    scope: SCOPE_KINDS.global,
    pattern: ["Right"],
    actionName: "Forward",
    actionType: ACTION_TYPES.hotkey,
    keys: ["Alt", "Right"]
  },
  {
    scope: SCOPE_KINDS.global,
    pattern: ["Down", "Right"],
    actionName: "Close Tab",
    actionType: ACTION_TYPES.hotkey,
    keys: ["Control", "W"]
  }
];

export const DEFAULT_APPLICATIONS = [
  { name: "msedge", displayName: "Microsoft Edge", path: "", category: "浏览器", icon: "" },
  { name: "chrome", displayName: "Google Chrome", path: "", category: "浏览器", icon: "" }
];

export const DEFAULT_UI_SETTINGS = {
  mouseTrail: {
    enabled: true,
    inactiveColor: "#AAAAAA",
    activeColor: "#87CEEB",
    inactiveThickness: 3,
    activeThickness: 3,
    thickness: 3,
    inactiveOpacity: 74,
    activeOpacity: 100
  },
  gestureHint: {
    enabled: true,
    fontFamily: "Segoe UI Semibold",
    fontSize: 22,
    textColor: "#FFFFFF",
    backgroundColor: "#12181F",
    backgroundOpacity: 90,
    width: 540,
    widthPercent: 28,
    autoWidth: true,
    height: 120,
    heightPercent: 11,
    cornerRadius: 28,
    bottomOffset: 140,
    bottomOffsetPercent: 13
  },
  appBehavior: {
    launchAtStartup: false,
    runAsAdministrator: false,
    closeButtonBehavior: CLOSE_BUTTON_BEHAVIORS.minimizeToTray,
    excludedApplications: []
  },
  webDav: {
    address: "",
    userName: "",
    password: "",
    remotePath: ""
  }
};

export const DEFAULT_EDGE_ACTIONS = [
  createDefaultEdgeAction(EDGE_TRIGGER_TYPES.corner, "top-left"),
  createDefaultEdgeAction(EDGE_TRIGGER_TYPES.corner, "top-right"),
  createDefaultEdgeAction(EDGE_TRIGGER_TYPES.corner, "bottom-left"),
  createDefaultEdgeAction(EDGE_TRIGGER_TYPES.corner, "bottom-right"),
  ...EDGE_LOCATIONS.edge.map((edge) => createDefaultEdgeAction(EDGE_TRIGGER_TYPES.friction, edge.value)),
  ...EDGE_LOCATIONS.edge.flatMap((edge) => [
    createDefaultEdgeAction(EDGE_TRIGGER_TYPES.wheel, edge.value, WHEEL_DIRECTIONS.up),
    createDefaultEdgeAction(EDGE_TRIGGER_TYPES.wheel, edge.value, WHEEL_DIRECTIONS.down)
  ])
];

export function createDefaultEdgeAction(triggerType, location, wheelDirection = "") {
  return {
    enabled: false,
    triggerType,
    location,
    wheelDirection,
    frictionCount: 4,
    keysText: "",
    actionType: ACTION_TYPES.hotkey,
    windowOperation: OPERATIONS.toggleMaximize,
    volumeOperation: OPERATIONS.increase,
    brightnessOperation: OPERATIONS.increase,
    amount: 5
  };
}
