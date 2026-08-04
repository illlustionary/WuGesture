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
} from './gestureEditorNormalizers'
import { getGestureMnemonic } from './gestureEditorFormatters'
import { ACTION_TYPES } from '@/constants/gestureEditorOptions'

export function toPayloadRule(rule) {
  return {
    scope: buildScope(rule.scopeKind, rule.scopeName),
    mouseButton: normalizeMouseButton(rule.mouseButton),
    pattern: parsePattern(rule.patternText),
    actionName: String(rule.actionName ?? '').trim() || getGestureMnemonic(rule),
    action: toPayloadAction(rule)
  }
}

export function toPayloadAction(rule) {
  if (normalizeActionType(rule.actionType) === ACTION_TYPES.window) {
    return {
      type: ACTION_TYPES.window,
      operation: normalizeWindowOperation(rule.windowOperation)
    }
  }

  if (normalizeActionType(rule.actionType) === ACTION_TYPES.volume) {
    return {
      type: ACTION_TYPES.volume,
      operation: normalizeVolumeOperation(rule.volumeOperation),
      amount: normalizeAmount(rule.amount)
    }
  }

  if (normalizeActionType(rule.actionType) === ACTION_TYPES.brightness) {
    return {
      type: ACTION_TYPES.brightness,
      operation: normalizeBrightnessOperation(rule.brightnessOperation),
      amount: normalizeAmount(rule.amount)
    }
  }

  if (normalizeActionType(rule.actionType) === ACTION_TYPES.program) {
    return {
      type: ACTION_TYPES.program,
      path: String(rule.programPath ?? '').trim(),
      arguments: (Array.isArray(rule.programArguments) ? rule.programArguments : [])
        .map(argument => String(argument ?? '').trim())
        .filter(Boolean)
    }
  }

  return {
    type: ACTION_TYPES.hotkey,
    keys: parseKeys(rule.keysText)
  }
}

export function toPayloadApplication(application) {
  return {
    name: application.name.trim(),
    displayName: String(application.displayName || application.name || '').trim(),
    path: application.path.trim(),
    categories: Array.isArray(application.categories)
      ? application.categories.map(category => String(category ?? '').trim()).filter(Boolean)
      : []
  }
}

export function toPayloadEdgeAction(action) {
  const normalized = normalizeEdgeAction(action)
  return {
    enabled: Boolean(normalized.enabled),
    triggerType: normalized.triggerType,
    location: normalized.location,
    wheelDirection: normalized.wheelDirection,
    frictionCount: normalizeFrictionCount(normalized.frictionCount),
    action: toPayloadAction(normalized)
  }
}

export function toPayloadUiSettings(settings) {
  const payload = normalizeUiSettings(settings)
  payload.appBehavior.excludedApplications = payload.appBehavior.excludedApplications.map(application => ({
    name: application.name,
    displayName: application.displayName,
    path: application.path,
    disableEdgeActions: application.disableEdgeActions
  }))
  return payload
}

export function buildConfigPayload({ rules, applications, categories, edgeActions, uiSettings }) {
  return {
    rules: rules.map(toPayloadRule),
    applications: applications.map(toPayloadApplication),
    categories: [...new Set((categories ?? []).map(category => String(category ?? '').trim()).filter(Boolean))],
    edgeActions: edgeActions.map(toPayloadEdgeAction),
    uiSettings: toPayloadUiSettings(uiSettings)
  }
}

export function getWebDavSignature(settings) {
  return JSON.stringify(normalizeWebDavSettings(settings))
}
