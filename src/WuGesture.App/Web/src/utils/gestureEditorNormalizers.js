import {
  ACTION_TYPES,
  BRIGHTNESS_OPERATIONS,
  CLOSE_BUTTON_BEHAVIORS,
  EDGE_LOCATIONS,
  EDGE_TRIGGER_TYPES,
  MOUSE_BUTTONS,
  OPERATIONS,
  SCOPE_KINDS,
  VOLUME_OPERATIONS,
  WHEEL_DIRECTIONS,
  WINDOW_OPERATIONS
} from "@/constants/gestureEditorOptions";
import {
  DEFAULT_EDGE_ACTIONS,
  DEFAULT_UI_SETTINGS
} from "@/constants/gestureEditorDefaults";
import { GESTURE_EDITOR_LIMITS } from "@/constants/gestureEditorLimits";

export function createDefaultUiSettings() {
  return cloneUiSettings(DEFAULT_UI_SETTINGS);
}

export function cloneUiSettings(settings) {
  const source = normalizeObjectKeys(settings);
  return {
    mouseTrail: normalizeMouseTrailSettings(source.mouseTrail),
    gestureHint: normalizeGestureHintSettings(source.gestureHint),
    levelOsd: normalizeLevelOsdSettings(source.levelOsd),
    gestureSensitivity: normalizeGestureSensitivitySettings(source.gestureSensitivity),
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
    enabled: Boolean(settings?.enabled ?? DEFAULT_UI_SETTINGS.mouseTrail.enabled),
    inactiveColor: String(settings?.inactiveColor ?? DEFAULT_UI_SETTINGS.mouseTrail.inactiveColor).trim() || DEFAULT_UI_SETTINGS.mouseTrail.inactiveColor,
    activeColor: String(settings?.activeColor ?? DEFAULT_UI_SETTINGS.mouseTrail.activeColor).trim() || DEFAULT_UI_SETTINGS.mouseTrail.activeColor,
    inactiveThickness: clampFloat(settings?.inactiveThickness ?? legacyThickness, GESTURE_EDITOR_LIMITS.mouseTrailThickness.min, GESTURE_EDITOR_LIMITS.mouseTrailThickness.max, DEFAULT_UI_SETTINGS.mouseTrail.inactiveThickness),
    activeThickness: clampFloat(settings?.activeThickness ?? legacyThickness, GESTURE_EDITOR_LIMITS.mouseTrailThickness.min, GESTURE_EDITOR_LIMITS.mouseTrailThickness.max, DEFAULT_UI_SETTINGS.mouseTrail.activeThickness),
    thickness: clampFloat(legacyThickness ?? settings?.inactiveThickness, GESTURE_EDITOR_LIMITS.mouseTrailThickness.min, GESTURE_EDITOR_LIMITS.mouseTrailThickness.max, DEFAULT_UI_SETTINGS.mouseTrail.thickness),
    inactiveOpacity: clampInteger(settings?.inactiveOpacity ?? legacyOpacity, GESTURE_EDITOR_LIMITS.opacityPercent.min, GESTURE_EDITOR_LIMITS.opacityPercent.max, DEFAULT_UI_SETTINGS.mouseTrail.inactiveOpacity),
    activeOpacity: clampInteger(settings?.activeOpacity ?? legacyOpacity, GESTURE_EDITOR_LIMITS.opacityPercent.min, GESTURE_EDITOR_LIMITS.opacityPercent.max, DEFAULT_UI_SETTINGS.mouseTrail.activeOpacity)
  };
}

export function normalizeGestureHintSettings(settings) {
  settings = normalizeObjectKeys(settings);
  return {
    enabled: Boolean(settings?.enabled ?? DEFAULT_UI_SETTINGS.gestureHint.enabled),
    fontFamily: String(settings?.fontFamily ?? DEFAULT_UI_SETTINGS.gestureHint.fontFamily).trim() || DEFAULT_UI_SETTINGS.gestureHint.fontFamily,
    fontSize: clampFloat(settings?.fontSize, GESTURE_EDITOR_LIMITS.hintFontSize.min, GESTURE_EDITOR_LIMITS.hintFontSize.max, DEFAULT_UI_SETTINGS.gestureHint.fontSize),
    textColor: String(settings?.textColor ?? DEFAULT_UI_SETTINGS.gestureHint.textColor).trim() || DEFAULT_UI_SETTINGS.gestureHint.textColor,
    backgroundColor: String(settings?.backgroundColor ?? DEFAULT_UI_SETTINGS.gestureHint.backgroundColor).trim() || DEFAULT_UI_SETTINGS.gestureHint.backgroundColor,
    backgroundOpacity: clampInteger(settings?.backgroundOpacity, GESTURE_EDITOR_LIMITS.opacityPercent.min, GESTURE_EDITOR_LIMITS.opacityPercent.max, DEFAULT_UI_SETTINGS.gestureHint.backgroundOpacity),
    width: clampInteger(settings?.width, GESTURE_EDITOR_LIMITS.hintWidth.min, GESTURE_EDITOR_LIMITS.hintWidth.max, DEFAULT_UI_SETTINGS.gestureHint.width),
    widthPercent: clampInteger(settings?.widthPercent, GESTURE_EDITOR_LIMITS.hintWidthPercent.min, GESTURE_EDITOR_LIMITS.hintWidthPercent.max, DEFAULT_UI_SETTINGS.gestureHint.widthPercent),
    autoWidth: Boolean(settings?.autoWidth ?? DEFAULT_UI_SETTINGS.gestureHint.autoWidth),
    height: clampInteger(settings?.height, GESTURE_EDITOR_LIMITS.hintHeight.min, GESTURE_EDITOR_LIMITS.hintHeight.max, DEFAULT_UI_SETTINGS.gestureHint.height),
    heightPercent: clampInteger(settings?.heightPercent, GESTURE_EDITOR_LIMITS.hintHeightPercent.min, GESTURE_EDITOR_LIMITS.hintHeightPercent.max, DEFAULT_UI_SETTINGS.gestureHint.heightPercent),
    cornerRadius: clampFloat(settings?.cornerRadius, GESTURE_EDITOR_LIMITS.hintCornerRadius.min, GESTURE_EDITOR_LIMITS.hintCornerRadius.max, DEFAULT_UI_SETTINGS.gestureHint.cornerRadius),
    bottomOffset: clampInteger(settings?.bottomOffset, GESTURE_EDITOR_LIMITS.hintBottomOffset.min, GESTURE_EDITOR_LIMITS.hintBottomOffset.max, DEFAULT_UI_SETTINGS.gestureHint.bottomOffset),
    bottomOffsetPercent: clampInteger(settings?.bottomOffsetPercent, GESTURE_EDITOR_LIMITS.hintBottomOffsetPercent.min, GESTURE_EDITOR_LIMITS.hintBottomOffsetPercent.max, DEFAULT_UI_SETTINGS.gestureHint.bottomOffsetPercent)
  };
}

export function normalizeLevelOsdSettings(settings) {
  settings = normalizeObjectKeys(settings);
  const width = clampInteger(
    settings?.width,
    GESTURE_EDITOR_LIMITS.levelOsdWidth.min,
    GESTURE_EDITOR_LIMITS.levelOsdWidth.max,
    DEFAULT_UI_SETTINGS.levelOsd.width
  );
  const height = clampInteger(
    settings?.height,
    GESTURE_EDITOR_LIMITS.levelOsdHeight.min,
    GESTURE_EDITOR_LIMITS.levelOsdHeight.max,
    DEFAULT_UI_SETTINGS.levelOsd.height
  );
  const maxRadius = Math.min(width, height) / 2;

  return {
    enabled: Boolean(settings?.enabled ?? DEFAULT_UI_SETTINGS.levelOsd.enabled),
    displayDurationMs: clampInteger(
      settings?.displayDurationMs,
      GESTURE_EDITOR_LIMITS.levelOsdDisplayDuration.min,
      GESTURE_EDITOR_LIMITS.levelOsdDisplayDuration.max,
      DEFAULT_UI_SETTINGS.levelOsd.displayDurationMs
    ),
    fadeDurationMs: clampInteger(
      settings?.fadeDurationMs,
      GESTURE_EDITOR_LIMITS.levelOsdFadeDuration.min,
      GESTURE_EDITOR_LIMITS.levelOsdFadeDuration.max,
      DEFAULT_UI_SETTINGS.levelOsd.fadeDurationMs
    ),
    backgroundColor: normalizeColor(settings?.backgroundColor, DEFAULT_UI_SETTINGS.levelOsd.backgroundColor),
    backgroundOpacity: clampInteger(
      settings?.backgroundOpacity,
      GESTURE_EDITOR_LIMITS.opacityPercent.min,
      GESTURE_EDITOR_LIMITS.opacityPercent.max,
      DEFAULT_UI_SETTINGS.levelOsd.backgroundOpacity
    ),
    textColor: normalizeColor(settings?.textColor, DEFAULT_UI_SETTINGS.levelOsd.textColor),
    trackColor: normalizeColor(settings?.trackColor, DEFAULT_UI_SETTINGS.levelOsd.trackColor),
    volumeColor: normalizeColor(settings?.volumeColor, DEFAULT_UI_SETTINGS.levelOsd.volumeColor),
    brightnessColor: normalizeColor(settings?.brightnessColor, DEFAULT_UI_SETTINGS.levelOsd.brightnessColor),
    width,
    height,
    cornerRadius: Math.min(
      maxRadius,
      clampInteger(
        settings?.cornerRadius,
        GESTURE_EDITOR_LIMITS.levelOsdCornerRadius.min,
        GESTURE_EDITOR_LIMITS.levelOsdCornerRadius.max,
        DEFAULT_UI_SETTINGS.levelOsd.cornerRadius
      )
    ),
    position: normalizeLevelOsdPosition(settings?.position),
    offsetX: clampInteger(
      settings?.offsetX,
      GESTURE_EDITOR_LIMITS.levelOsdOffset.min,
      GESTURE_EDITOR_LIMITS.levelOsdOffset.max,
      DEFAULT_UI_SETTINGS.levelOsd.offsetX
    ),
    offsetY: clampInteger(
      settings?.offsetY,
      GESTURE_EDITOR_LIMITS.levelOsdOffset.min,
      GESTURE_EDITOR_LIMITS.levelOsdOffset.max,
      DEFAULT_UI_SETTINGS.levelOsd.offsetY
    )
  };
}

function normalizeColor(value, fallback) {
  const normalized = String(value ?? "").trim();
  return normalized || fallback;
}

export function normalizeGestureSensitivitySettings(settings) {
  settings = normalizeObjectKeys(settings);
  const levels = ["relaxed", "standard", "strict"];
  const level = String(settings?.level ?? DEFAULT_UI_SETTINGS.gestureSensitivity.level).trim();
  return {
    level: levels.includes(level)
      ? level
      : DEFAULT_UI_SETTINGS.gestureSensitivity.level
  };
}

function normalizeLevelOsdPosition(value) {
  const normalized = String(value ?? "").trim();
  return [
    "center",
    "top-center",
    "bottom-center",
    "top-left",
    "top-right",
    "bottom-left",
    "bottom-right"
  ].includes(normalized)
    ? normalized
    : DEFAULT_UI_SETTINGS.levelOsd.position;
}

export function normalizeAppBehaviorSettings(settings) {
  settings = normalizeObjectKeys(settings);
  return {
    launchAtStartup: Boolean(settings?.launchAtStartup ?? DEFAULT_UI_SETTINGS.appBehavior.launchAtStartup),
    runAsAdministrator: Boolean(settings?.runAsAdministrator ?? DEFAULT_UI_SETTINGS.appBehavior.runAsAdministrator),
    closeButtonBehavior: normalizeCloseButtonBehavior(settings?.closeButtonBehavior),
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
  return Object.values(CLOSE_BUTTON_BEHAVIORS).includes(normalized)
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
  return String(button ?? "").toLowerCase() === MOUSE_BUTTONS.middle
    ? MOUSE_BUTTONS.middle
    : MOUSE_BUTTONS.right;
}

export function normalizeActionType(actionType) {
  const normalized = String(actionType ?? "").toLowerCase();
  return Object.values(ACTION_TYPES).includes(normalized) ? normalized : ACTION_TYPES.hotkey;
}

export function normalizeWindowOperation(operation) {
  const normalized = String(operation ?? "").trim();
  return WINDOW_OPERATIONS.some((item) => item.value === normalized)
    ? normalized
    : OPERATIONS.toggleMaximize;
}

export function normalizeVolumeOperation(operation) {
  const normalized = String(operation ?? "").trim();
  return VOLUME_OPERATIONS.some((item) => item.value === normalized)
    ? normalized
    : OPERATIONS.increase;
}

export function normalizeBrightnessOperation(operation) {
  const normalized = String(operation ?? "").trim();
  return BRIGHTNESS_OPERATIONS.some((item) => item.value === normalized)
    ? normalized
    : OPERATIONS.increase;
}

export function normalizeAmount(value) {
  return clampInteger(value, GESTURE_EDITOR_LIMITS.amount.min, GESTURE_EDITOR_LIMITS.amount.max, GESTURE_EDITOR_LIMITS.amount.fallback);
}

export function normalizeFrictionCount(value) {
  return clampInteger(value, GESTURE_EDITOR_LIMITS.frictionCount.min, GESTURE_EDITOR_LIMITS.frictionCount.max, GESTURE_EDITOR_LIMITS.frictionCount.fallback);
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
  normalized.wheelDirection = normalized.triggerType === EDGE_TRIGGER_TYPES.wheel ? normalizeWheelDirection(action?.wheelDirection) : "";
  return normalized;
}

export function normalizeEdgeActionInPlace(action) {
  Object.assign(action, normalizeEdgeAction(action));
}

export function normalizeEdgeTriggerType(triggerType) {
  const normalized = String(triggerType ?? "").toLowerCase();
  return Object.values(EDGE_TRIGGER_TYPES).includes(normalized) ? normalized : EDGE_TRIGGER_TYPES.corner;
}

export function normalizeEdgeLocation(location, triggerType) {
  const locations = triggerType === EDGE_TRIGGER_TYPES.corner ? EDGE_LOCATIONS.corner : EDGE_LOCATIONS.edge;
  const normalized = String(location ?? "").trim();
  if (triggerType === EDGE_TRIGGER_TYPES.friction) {
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
  return String(direction ?? "").toLowerCase() === WHEEL_DIRECTIONS.down
    ? WHEEL_DIRECTIONS.down
    : WHEEL_DIRECTIONS.up;
}

export function getEdgeActionKey(action) {
  return `${action.triggerType}:${action.location}:${action.wheelDirection || ""}`;
}

export function parseScope(scope) {
  const normalized = String(scope ?? "").trim();
  if (normalized.length === 0 || normalized.toLowerCase() === SCOPE_KINDS.global) {
    return { kind: SCOPE_KINDS.global, name: "" };
  }

  const colonIndex = normalized.indexOf(":");
  if (colonIndex === -1) {
    return { kind: SCOPE_KINDS.global, name: "" };
  }

  const kind = normalized.slice(0, colonIndex).trim().toLowerCase();
  const name = normalized.slice(colonIndex + 1).trim();
  if ((kind === SCOPE_KINDS.category || kind === SCOPE_KINDS.app) && name.length > 0) {
    return { kind, name };
  }

  return { kind: SCOPE_KINDS.global, name: "" };
}

export function buildScope(kind, name) {
  if (kind === SCOPE_KINDS.category || kind === SCOPE_KINDS.app) {
    const trimmed = String(name ?? "").trim();
    return trimmed ? `${kind}:${trimmed}` : "";
  }

  return SCOPE_KINDS.global;
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
