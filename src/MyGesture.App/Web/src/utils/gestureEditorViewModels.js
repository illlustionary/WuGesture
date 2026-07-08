import {
  normalizeActionType,
  normalizeAmount,
  normalizeBrightnessOperation,
  normalizeMouseButton,
  normalizeVolumeOperation,
  normalizeWindowOperation,
  parseScope,
  toPatternText
} from "./gestureEditorNormalizers";
import { getGestureMnemonic } from "./gestureEditorFormatters";

export function toViewRule(rule, id) {
  const scope = parseScope(rule.scope);
  return {
    id,
    scopeKind: scope.kind,
    scopeName: scope.name,
    patternText: toPatternText(rule.pattern),
    mouseButton: normalizeMouseButton(rule.mouseButton),
    actionName: rule.actionName ?? "",
    keysText: Array.isArray(rule.keys) ? rule.keys.join(" + ") : "",
    actionType: normalizeActionType(rule.actionType),
    windowOperation: normalizeWindowOperation(rule.operation),
    volumeOperation: normalizeVolumeOperation(rule.operation),
    brightnessOperation: normalizeBrightnessOperation(rule.operation),
    amount: normalizeAmount(rule.amount)
  };
}

export function toViewApplication(application) {
  return {
    name: String(application.name ?? "").trim(),
    displayName: String(application.displayName ?? application.name ?? "").trim(),
    path: String(application.path ?? "").trim(),
    category: String(application.category ?? "").trim(),
    icon: String(application.icon ?? "").trim()
  };
}

export function createRuleModel(scopeKind, scopeName, values = {}, id) {
  return {
    id,
    scopeKind,
    scopeName,
    patternText: values.patternText ?? "Left",
    mouseButton: normalizeMouseButton(values.mouseButton),
    actionName: values.actionName ?? getGestureMnemonic({
      patternText: values.patternText ?? "Left",
      mouseButton: values.mouseButton
    }),
    keysText: values.keysText ?? "",
    actionType: normalizeActionType(values.actionType),
    windowOperation: normalizeWindowOperation(values.windowOperation),
    volumeOperation: normalizeVolumeOperation(values.volumeOperation),
    brightnessOperation: normalizeBrightnessOperation(values.brightnessOperation),
    amount: normalizeAmount(values.amount)
  };
}

export function createEmptyGestureDraft() {
  return {
    actionName: "",
    patternText: "",
    mouseButton: "right",
    keysText: "",
    actionType: "hotkey",
    windowOperation: "toggle-maximize",
    volumeOperation: "increase",
    brightnessOperation: "increase",
    amount: 5
  };
}
