import { EDGE_LOCATIONS } from "./gestureEditorOptions";

export const DEFAULT_RULES = [
  {
    scope: "global",
    pattern: ["Left"],
    actionName: "Back",
    actionType: "hotkey",
    keys: ["Alt", "Left"]
  },
  {
    scope: "global",
    pattern: ["Right"],
    actionName: "Forward",
    actionType: "hotkey",
    keys: ["Alt", "Right"]
  },
  {
    scope: "global",
    pattern: ["Down", "Right"],
    actionName: "Close Tab",
    actionType: "hotkey",
    keys: ["Control", "W"]
  }
];

export const DEFAULT_APPLICATIONS = [
  { name: "msedge", displayName: "Microsoft Edge", path: "", category: "浏览器", icon: "" },
  { name: "chrome", displayName: "Google Chrome", path: "", category: "浏览器", icon: "" }
];

export const DEFAULT_UI_SETTINGS = {
  mouseTrail: {
    inactiveColor: "#AAAAAA",
    activeColor: "#87CEEB",
    inactiveThickness: 3,
    activeThickness: 3,
    thickness: 3,
    inactiveOpacity: 74,
    activeOpacity: 100
  },
  gestureHint: {
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
    closeButtonBehavior: "minimize-to-tray",
    gesturePaused: false,
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
  createDefaultEdgeAction("corner", "top-left"),
  createDefaultEdgeAction("corner", "top-right"),
  createDefaultEdgeAction("corner", "bottom-left"),
  createDefaultEdgeAction("corner", "bottom-right"),
  ...EDGE_LOCATIONS.edge.map((edge) => createDefaultEdgeAction("friction", edge.value)),
  ...EDGE_LOCATIONS.edge.flatMap((edge) => [
    createDefaultEdgeAction("wheel", edge.value, "up"),
    createDefaultEdgeAction("wheel", edge.value, "down")
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
    actionType: "hotkey",
    windowOperation: "toggle-maximize",
    volumeOperation: "increase",
    brightnessOperation: "increase",
    amount: 5
  };
}
