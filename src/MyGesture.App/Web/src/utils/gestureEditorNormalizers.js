import {
  BRIGHTNESS_OPERATIONS,
  EDGE_LOCATIONS,
  VOLUME_OPERATIONS,
  WINDOW_OPERATIONS
} from "../constants/gestureEditorOptions";
import {
  DEFAULT_EDGE_ACTIONS,
  DEFAULT_UI_SETTINGS
} from "../constants/gestureEditorDefaults";

export function createDefaultUiSettings() {
  return cloneUiSettings(DEFAULT_UI_SETTINGS);
}

export function cloneUiSettings(settings) {
  const source = normalizeObjectKeys(settings);
  return {
    mouseTrail: normalizeMouseTrailSettings(source.mouseTrail),
    gestureHint: normalizeGestureHintSettings(source.gestureHint),
    appBehavior: normalizeAppBehaviorSettings(source.appBehavior),
    webDav: normalizeWebDavSettings(source.webDav)
  };
}

export function normalizeUiSettings(settings) {
  return cloneUiSettings(settings);
}

export function normalizeMouseTrailSettings(settings) {
  settings = normalizeObjectKeys(settings);
  const legacyOpacity = settings?.opacity;
  const legacyThickness = settings?.thickness;
  return {
    inactiveColor: String(settings?.inactiveColor ?? DEFAULT_UI_SETTINGS.mouseTrail.inactiveColor).trim() || DEFAULT_UI_SETTINGS.mouseTrail.inactiveColor,
    activeColor: String(settings?.activeColor ?? DEFAULT_UI_SETTINGS.mouseTrail.activeColor).trim() || DEFAULT_UI_SETTINGS.mouseTrail.activeColor,
    inactiveThickness: clampFloat(settings?.inactiveThickness ?? legacyThickness, 1, 20, DEFAULT_UI_SETTINGS.mouseTrail.inactiveThickness),
    activeThickness: clampFloat(settings?.activeThickness ?? legacyThickness, 1, 20, DEFAULT_UI_SETTINGS.mouseTrail.activeThickness),
    thickness: clampFloat(legacyThickness ?? settings?.inactiveThickness, 1, 20, DEFAULT_UI_SETTINGS.mouseTrail.thickness),
    inactiveOpacity: clampInteger(settings?.inactiveOpacity ?? legacyOpacity, 0, 100, DEFAULT_UI_SETTINGS.mouseTrail.inactiveOpacity),
    activeOpacity: clampInteger(settings?.activeOpacity ?? legacyOpacity, 0, 100, DEFAULT_UI_SETTINGS.mouseTrail.activeOpacity)
  };
}

export function normalizeGestureHintSettings(settings) {
  settings = normalizeObjectKeys(settings);
  return {
    fontFamily: String(settings?.fontFamily ?? DEFAULT_UI_SETTINGS.gestureHint.fontFamily).trim() || DEFAULT_UI_SETTINGS.gestureHint.fontFamily,
    fontSize: clampFloat(settings?.fontSize, 10, 48, DEFAULT_UI_SETTINGS.gestureHint.fontSize),
    textColor: String(settings?.textColor ?? DEFAULT_UI_SETTINGS.gestureHint.textColor).trim() || DEFAULT_UI_SETTINGS.gestureHint.textColor,
    backgroundColor: String(settings?.backgroundColor ?? DEFAULT_UI_SETTINGS.gestureHint.backgroundColor).trim() || DEFAULT_UI_SETTINGS.gestureHint.backgroundColor,
    backgroundOpacity: clampInteger(settings?.backgroundOpacity, 0, 100, DEFAULT_UI_SETTINGS.gestureHint.backgroundOpacity),
    width: clampInteger(settings?.width, 240, 960, DEFAULT_UI_SETTINGS.gestureHint.width),
    widthPercent: clampInteger(settings?.widthPercent, 10, 90, DEFAULT_UI_SETTINGS.gestureHint.widthPercent),
    autoWidth: Boolean(settings?.autoWidth ?? DEFAULT_UI_SETTINGS.gestureHint.autoWidth),
    height: clampInteger(settings?.height, 80, 260, DEFAULT_UI_SETTINGS.gestureHint.height),
    heightPercent: clampInteger(settings?.heightPercent, 5, 40, DEFAULT_UI_SETTINGS.gestureHint.heightPercent),
    cornerRadius: clampFloat(settings?.cornerRadius, 0, 80, DEFAULT_UI_SETTINGS.gestureHint.cornerRadius),
    bottomOffset: clampInteger(settings?.bottomOffset, 0, 1200, DEFAULT_UI_SETTINGS.gestureHint.bottomOffset),
    bottomOffsetPercent: clampInteger(settings?.bottomOffsetPercent, 0, 100, DEFAULT_UI_SETTINGS.gestureHint.bottomOffsetPercent)
  };
}

export function normalizeAppBehaviorSettings(settings) {
  settings = normalizeObjectKeys(settings);
  return {
    launchAtStartup: Boolean(settings?.launchAtStartup ?? DEFAULT_UI_SETTINGS.appBehavior.launchAtStartup),
    runAsAdministrator: Boolean(settings?.runAsAdministrator ?? DEFAULT_UI_SETTINGS.appBehavior.runAsAdministrator),
    closeButtonBehavior: normalizeCloseButtonBehavior(settings?.closeButtonBehavior),
    gesturePaused: Boolean(settings?.gesturePaused ?? DEFAULT_UI_SETTINGS.appBehavior.gesturePaused),
    excludedApplications: normalizeExcludedApplications(settings?.excludedApplications)
  };
}

export function normalizeExcludedApplications(applications) {
  const normalized = [];
  for (const application of Array.isArray(applications) ? applications : []) {
    const exclusion = normalizeExcludedApplication(application);
    if (!exclusion.name && !exclusion.path) {
      continue;
    }

    if (!normalized.some((item) => isSameApplicationIdentity(item, exclusion))) {
      normalized.push(exclusion);
    }
  }

  return normalized;
}

export function normalizeExcludedApplication(application) {
  application = normalizeObjectKeys(application);
  const name = String(application?.name ?? "").trim();
  const path = String(application?.path ?? "").trim();
  return {
    name,
    displayName: String(application?.displayName ?? name).trim() || name || path,
    path,
    icon: String(application?.icon ?? "").trim(),
    disableEdgeActions: Boolean(application?.disableEdgeActions ?? false)
  };
}

export function isSameApplicationIdentity(left, right) {
  const leftPath = String(left?.path ?? "").trim().toLowerCase();
  const rightPath = String(right?.path ?? "").trim().toLowerCase();
  if (leftPath && rightPath) {
    return leftPath === rightPath;
  }

  return String(left?.name ?? "").trim().toLowerCase() === String(right?.name ?? "").trim().toLowerCase();
}

export function normalizeWebDavSettings(settings) {
  settings = normalizeObjectKeys(settings);
  return {
    address: String(settings?.address ?? DEFAULT_UI_SETTINGS.webDav.address).trim(),
    userName: String(settings?.userName ?? DEFAULT_UI_SETTINGS.webDav.userName).trim(),
    password: String(settings?.password ?? DEFAULT_UI_SETTINGS.webDav.password),
    remotePath: String(settings?.remotePath ?? DEFAULT_UI_SETTINGS.webDav.remotePath).trim()
  };
}

export function normalizeCloseButtonBehavior(value) {
  const normalized = String(value ?? "").trim();
  return ["minimize-to-tray", "minimize-to-taskbar", "exit"].includes(normalized)
    ? normalized
    : DEFAULT_UI_SETTINGS.appBehavior.closeButtonBehavior;
}

export function normalizeObjectKeys(source) {
  if (!source || typeof source !== "object") {
    return {};
  }

  const normalized = {};
  for (const [key, value] of Object.entries(source)) {
    const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
    normalized[normalizedKey] = value;
  }

  return normalized;
}

export function clampInteger(value, min, max, fallback) {
  const parsed = Number.parseInt(value, 10);
  if (!Number.isFinite(parsed)) {
    return fallback;
  }

  return Math.min(max, Math.max(min, parsed));
}

export function clampFloat(value, min, max, fallback) {
  const parsed = Number.parseFloat(value);
  if (!Number.isFinite(parsed)) {
    return fallback;
  }

  return Math.min(max, Math.max(min, parsed));
}

export function normalizeMouseButton(button) {
  return String(button ?? "").toLowerCase() === "middle" ? "middle" : "right";
}

export function normalizeActionType(actionType) {
  const normalized = String(actionType ?? "").toLowerCase();
  return ["hotkey", "window", "volume", "brightness"].includes(normalized) ? normalized : "hotkey";
}

export function normalizeWindowOperation(operation) {
  const normalized = String(operation ?? "").trim();
  return WINDOW_OPERATIONS.some((item) => item.value === normalized)
    ? normalized
    : "toggle-maximize";
}

export function normalizeVolumeOperation(operation) {
  const normalized = String(operation ?? "").trim();
  return VOLUME_OPERATIONS.some((item) => item.value === normalized)
    ? normalized
    : "increase";
}

export function normalizeBrightnessOperation(operation) {
  const normalized = String(operation ?? "").trim();
  return BRIGHTNESS_OPERATIONS.some((item) => item.value === normalized)
    ? normalized
    : "increase";
}

export function normalizeAmount(value) {
  return clampInteger(value, 1, 100, 5);
}

export function normalizeFrictionCount(value) {
  return clampInteger(value, 1, 20, 4);
}

export function normalizeEdgeActions(edgeActions) {
  const merged = new Map();
  for (const action of DEFAULT_EDGE_ACTIONS) {
    merged.set(getEdgeActionKey(action), { ...action });
  }

  for (const action of Array.isArray(edgeActions) ? edgeActions : []) {
    const normalized = normalizeEdgeAction(action);
    merged.set(getEdgeActionKey(normalized), normalized);
  }

  return [...merged.values()];
}

export function normalizeEdgeAction(action) {
  const normalized = {
    enabled: Boolean(action?.enabled ?? false),
    triggerType: normalizeEdgeTriggerType(action?.triggerType),
    location: "",
    wheelDirection: "",
    frictionCount: normalizeFrictionCount(action?.frictionCount),
    keysText: Array.isArray(action?.action?.keys) ? action.action.keys.join(" + ") : String(action?.keysText ?? "").trim(),
    actionType: normalizeActionType(action?.action?.type ?? action?.actionType),
    windowOperation: normalizeWindowOperation(action?.action?.operation ?? action?.windowOperation),
    volumeOperation: normalizeVolumeOperation(action?.action?.operation ?? action?.volumeOperation),
    brightnessOperation: normalizeBrightnessOperation(action?.action?.operation ?? action?.brightnessOperation),
    amount: normalizeAmount(action?.action?.amount ?? action?.amount)
  };
  normalized.location = normalizeEdgeLocation(action?.location, normalized.triggerType);
  normalized.wheelDirection = normalized.triggerType === "wheel" ? normalizeWheelDirection(action?.wheelDirection) : "";
  return normalized;
}

export function normalizeEdgeActionInPlace(action) {
  Object.assign(action, normalizeEdgeAction(action));
}

export function normalizeEdgeTriggerType(triggerType) {
  const normalized = String(triggerType ?? "").toLowerCase();
  return ["corner", "friction", "wheel"].includes(normalized) ? normalized : "corner";
}

export function normalizeEdgeLocation(location, triggerType) {
  const locations = triggerType === "corner" ? EDGE_LOCATIONS.corner : EDGE_LOCATIONS.edge;
  const normalized = String(location ?? "").trim();
  if (triggerType === "friction") {
    const migratedLocation = migrateLegacyFrictionLocation(normalized);
    if (migratedLocation) {
      return migratedLocation;
    }
  }

  return locations.some((item) => item.value === normalized)
    ? normalized
    : locations[0].value;
}

export function migrateLegacyFrictionLocation(location) {
  return {
    "top-left": "left",
    "top-right": "top",
    "bottom-left": "bottom",
    "bottom-right": "right"
  }[location] ?? "";
}

export function normalizeWheelDirection(direction) {
  return String(direction ?? "").toLowerCase() === "down" ? "down" : "up";
}

export function getEdgeActionKey(action) {
  return `${action.triggerType}:${action.location}:${action.wheelDirection || ""}`;
}

export function parseScope(scope) {
  const normalized = String(scope ?? "").trim();
  if (normalized.length === 0 || normalized.toLowerCase() === "global") {
    return { kind: "global", name: "" };
  }

  const colonIndex = normalized.indexOf(":");
  if (colonIndex === -1) {
    return { kind: "global", name: "" };
  }

  const kind = normalized.slice(0, colonIndex).trim().toLowerCase();
  const name = normalized.slice(colonIndex + 1).trim();
  if ((kind === "category" || kind === "app") && name.length > 0) {
    return { kind, name };
  }

  return { kind: "global", name: "" };
}

export function buildScope(kind, name) {
  if (kind === "category" || kind === "app") {
    const trimmed = String(name ?? "").trim();
    return trimmed ? `${kind}:${trimmed}` : "";
  }

  return "global";
}

export function parsePattern(text) {
  return String(text ?? "")
    .split(/[\s,，]+/)
    .map((part) => part.trim())
    .filter(Boolean);
}

export function parseKeys(text) {
  return String(text ?? "")
    .split("+")
    .map((part) => part.trim())
    .filter(Boolean);
}

export function toPatternText(pattern) {
  if (!Array.isArray(pattern) || pattern.length === 0) {
    return "";
  }

  return pattern.join(", ");
}
