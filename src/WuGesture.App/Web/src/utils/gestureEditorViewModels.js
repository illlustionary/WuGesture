import { ACTION_TYPES, MOUSE_BUTTONS, OPERATIONS } from '@/constants/gestureEditorOptions'
import {
  normalizeActionType,
  normalizeAmount,
  normalizeBrightnessOperation,
  normalizeMouseButton,
  normalizeVolumeOperation,
  normalizeWindowOperation,
  parseScope,
  toPatternText
} from './gestureEditorNormalizers'
import { getGestureMnemonic } from './gestureEditorFormatters'

export function toViewRule(rule, id) {
  const scope = parseScope(rule.scope)
  return {
    id,
    scopeKind: scope.kind,
    scopeName: scope.name,
    patternText: toPatternText(rule.pattern),
    mouseButton: normalizeMouseButton(rule.mouseButton),
    actionName: rule.actionName ?? '',
    keysText: Array.isArray(rule.keys) ? rule.keys.join(' + ') : '',
    actionType: normalizeActionType(rule.actionType),
    windowOperation: normalizeWindowOperation(rule.operation),
    volumeOperation: normalizeVolumeOperation(rule.operation),
    brightnessOperation: normalizeBrightnessOperation(rule.operation),
    amount: normalizeAmount(rule.amount),
    programPath: String(rule.path ?? '').trim(),
    programArguments: normalizeProgramArguments(rule.arguments),
    programName: String(rule.programName ?? '').trim(),
    programIcon: String(rule.programIcon ?? '').trim(),
    programMissing: Boolean(rule.programMissing)
  }
}

export function toViewApplication(application) {
  const categories = normalizeCategories(application.categories)
  return {
    name: String(application.name ?? '').trim(),
    displayName: String(application.displayName ?? application.name ?? '').trim(),
    path: String(application.path ?? '').trim(),
    categories,
    icon: String(application.icon ?? '').trim()
  }
}

function normalizeCategories(categories) {
  const values = Array.isArray(categories) ? categories : []
  return [...new Set(values.map(category => String(category ?? '').trim()).filter(Boolean))]
}

export function createRuleModel(scopeKind, scopeName, values = {}, id) {
  return {
    id,
    scopeKind,
    scopeName,
    patternText: values.patternText ?? 'Left',
    mouseButton: normalizeMouseButton(values.mouseButton),
    actionName:
      values.actionName ??
      getGestureMnemonic({
        patternText: values.patternText ?? 'Left',
        mouseButton: values.mouseButton
      }),
    keysText: values.keysText ?? '',
    actionType: normalizeActionType(values.actionType),
    windowOperation: normalizeWindowOperation(values.windowOperation),
    volumeOperation: normalizeVolumeOperation(values.volumeOperation),
    brightnessOperation: normalizeBrightnessOperation(values.brightnessOperation),
    amount: normalizeAmount(values.amount),
    programPath: String(values.programPath ?? '').trim(),
    programArguments: normalizeProgramArguments(values.programArguments),
    programName: String(values.programName ?? '').trim(),
    programIcon: String(values.programIcon ?? '').trim(),
    programMissing: Boolean(values.programMissing)
  }
}

export function createEmptyGestureDraft() {
  return {
    actionName: '',
    patternText: '',
    mouseButton: MOUSE_BUTTONS.right,
    keysText: '',
    actionType: ACTION_TYPES.hotkey,
    windowOperation: OPERATIONS.toggleMaximize,
    volumeOperation: OPERATIONS.increase,
    brightnessOperation: OPERATIONS.increase,
    amount: 5,
    programPath: '',
    programArguments: [],
    programName: '',
    programIcon: '',
    programMissing: false
  }
}

function normalizeProgramArguments(argumentsList) {
  return (Array.isArray(argumentsList) ? argumentsList : []).map(value => String(value ?? '').trim()).filter(Boolean)
}
