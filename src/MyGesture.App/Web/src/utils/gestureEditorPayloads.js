import {
  buildScope,
  normalizeActionType,
  normalizeAmount,
  normalizeBrightnessOperation,
  normalizeEdgeAction,
  normalizeFrictionCount,
  normalizeMouseButton,
  normalizeUiSettings,
  normalizeWebDavSettings,
  normalizeVolumeOperation,
  normalizeWindowOperation,
  parseKeys,
  parsePattern
} from "./gestureEditorNormalizers";
import { getGestureMnemonic } from "./gestureEditorFormatters";

export function toPayloadRule(rule) {
  return {
    scope: buildScope(rule.scopeKind, rule.scopeName),
    mouseButton: normalizeMouseButton(rule.mouseButton),
    pattern: parsePattern(rule.patternText),
    actionName: String(rule.actionName ?? "").trim() || getGestureMnemonic(rule),
    action: toPayloadAction(rule)
  };
}

export function toPayloadAction(rule) {
  if (normalizeActionType(rule.actionType) === "window") {
    return {
      type: "window",
      operation: normalizeWindowOperation(rule.windowOperation)
    };
  }

  if (normalizeActionType(rule.actionType) === "volume") {
    return {
      type: "volume",
      operation: normalizeVolumeOperation(rule.volumeOperation),
      amount: normalizeAmount(rule.amount)
    };
  }

  if (normalizeActionType(rule.actionType) === "brightness") {
    return {
      type: "brightness",
      operation: normalizeBrightnessOperation(rule.brightnessOperation),
      amount: normalizeAmount(rule.amount)
    };
  }

  return {
    type: "hotkey",
    keys: parseKeys(rule.keysText)
  };
}

export function toPayloadApplication(application) {
  return {
    name: application.name.trim(),
    displayName: String(application.displayName || application.name || "").trim(),
    path: application.path.trim(),
    category: application.category.trim()
  };
}

export function toPayloadEdgeAction(action) {
  const normalized = normalizeEdgeAction(action);
  return {
    enabled: Boolean(normalized.enabled),
    triggerType: normalized.triggerType,
    location: normalized.location,
    wheelDirection: normalized.wheelDirection,
    frictionCount: normalizeFrictionCount(normalized.frictionCount),
    action: toPayloadAction(normalized)
  };
}

export function toPayloadUiSettings(settings) {
  const payload = normalizeUiSettings(settings);
  payload.appBehavior.excludedApplications = payload.appBehavior.excludedApplications.map((application) => ({
    name: application.name,
    displayName: application.displayName,
    path: application.path,
    disableEdgeActions: application.disableEdgeActions
  }));
  return payload;
}

export function buildConfigPayload({ rules, applications, edgeActions, uiSettings }) {
  return {
    rules: rules.map(toPayloadRule),
    applications: applications.map(toPayloadApplication),
    edgeActions: edgeActions.map(toPayloadEdgeAction),
    uiSettings: toPayloadUiSettings(uiSettings)
  };
}

export function getWebDavSignature(settings) {
  return JSON.stringify(normalizeWebDavSettings(settings));
}
