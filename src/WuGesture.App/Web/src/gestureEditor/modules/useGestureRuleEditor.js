import { ACTION_TYPES, SCOPE_KINDS, WEBVIEW_MESSAGE_TYPES } from '@/constants/gestureEditorOptions'
import {
  normalizeActionType,
  normalizeAmount,
  normalizeBrightnessOperation,
  normalizeMouseButton,
  normalizeVolumeOperation,
  normalizeWindowOperation,
  parseKeys,
  parsePattern,
  toPatternText
} from '@/utils/gestureEditorNormalizers'
import { getActionLabel, getGestureMnemonic } from '@/utils/gestureEditorFormatters'
import { createEmptyGestureDraft } from '@/utils/gestureEditorViewModels'

export function useGestureRuleEditor({
  state,
  activeScope,
  createRequestId,
  createRule,
  ensureSelection,
  getFirstScopeName,
  getSelectedName,
  scheduleSaveRules,
  setGesturePaused,
  setMessage,
  setSelectedName,
  webView
}) {
  function addRule(kind = activeScope.value, name = getSelectedName(kind)) {
    openAddRule(kind, name)
  }

  function openAddRule(kind = activeScope.value, name = getSelectedName(kind)) {
    if (kind === SCOPE_KINDS.global) {
      openGestureEditor('add', null, SCOPE_KINDS.global, '')
      return
    }

    const scopeName = String(name || getFirstScopeName(kind)).trim()
    if (!scopeName) {
      setMessage(kind === SCOPE_KINDS.category ? '先新增或选择一个分类。' : '先新增或选择一个程序。', 'error')
      return
    }

    setSelectedName(kind, scopeName)
    openGestureEditor('add', null, kind, scopeName)
  }

  function openEditRule(ruleId) {
    const rule = state.rules.find(item => item.id === ruleId)
    if (!rule) {
      return
    }

    openGestureEditor('edit', rule, rule.scopeKind, rule.scopeName)
  }

  function closeGestureEditor() {
    stopGestureRecording()
    stopRecording()
    commitGestureEditor({ allowIncomplete: state.gestureEditorMode === 'edit' })
    resetGestureEditor()
  }

  function resetGestureEditor() {
    state.gestureEditorOpen = false
    state.gestureRecognitionMessage = ''
    state.gestureDraft = createEmptyGestureDraft()
    setGesturePaused(false)
  }

  function persistGestureEditor() {
    return commitGestureEditor({ allowIncomplete: state.gestureEditorMode === 'edit' })
  }

  function saveGestureEditor() {
    const isSaved = commitGestureEditor({ allowIncomplete: state.gestureEditorMode === 'edit' })
    if (isSaved) {
      resetGestureEditor()
    }

    return isSaved
  }

  function commitGestureEditor({ allowIncomplete }) {
    const draft = state.gestureDraft
    const pattern = parsePattern(draft.patternText)
    const actionType = normalizeActionType(draft.actionType)

    if (pattern.length === 0 && !allowIncomplete) {
      state.gestureRecognitionMessage = '请先录制手势。'
      return false
    }

    if (actionType === ACTION_TYPES.hotkey && parseKeys(draft.keysText).length === 0 && !allowIncomplete) {
      state.gestureRecognitionMessage = '请先录入快捷键。'
      return false
    }

    if (actionType === ACTION_TYPES.window && !normalizeWindowOperation(draft.windowOperation) && !allowIncomplete) {
      state.gestureRecognitionMessage = '请选择窗口控制操作。'
      return false
    }

    if (pattern.length > 0 && findDuplicateGestureRule(pattern, draft.mouseButton)) {
      if (!allowIncomplete) {
        state.gestureRecognitionMessage = '⚠ 当前作用域已存在相同手势，请更换后再保存。'
      }
      return false
    }

    const actionName = getActionName(draft)
    draft.actionName = actionName

    let rule = null
    if (state.gestureEditorMode === 'edit') {
      rule = state.rules.find(item => item.id === state.gestureEditorRuleId)
      if (!rule) {
        return false
      }
    } else {
      rule = createRule(state.gestureEditorScopeKind, state.gestureEditorScopeName, {
        actionName,
        patternText: toPatternText(pattern),
        mouseButton: normalizeMouseButton(draft.mouseButton),
        keysText: draft.keysText,
        actionType,
        windowOperation: normalizeWindowOperation(draft.windowOperation),
        volumeOperation: normalizeVolumeOperation(draft.volumeOperation),
        brightnessOperation: normalizeBrightnessOperation(draft.brightnessOperation),
        amount: normalizeAmount(draft.amount),
        programPath: String(draft.programPath ?? '').trim(),
        programArguments: normalizeProgramArguments(draft.programArguments),
        programName: String(draft.programName ?? '').trim(),
        programIcon: String(draft.programIcon ?? '').trim(),
        programMissing: Boolean(draft.programMissing)
      })
      state.rules.push(rule)
      state.gestureEditorRuleId = rule.id
      state.gestureEditorMode = 'edit'
    }

    rule.actionName = actionName
    rule.patternText = toPatternText(pattern)
    rule.mouseButton = normalizeMouseButton(draft.mouseButton)
    rule.keysText = draft.keysText
    rule.actionType = actionType
    rule.windowOperation = normalizeWindowOperation(draft.windowOperation)
    rule.volumeOperation = normalizeVolumeOperation(draft.volumeOperation)
    rule.brightnessOperation = normalizeBrightnessOperation(draft.brightnessOperation)
    rule.amount = normalizeAmount(draft.amount)
    rule.programPath = String(draft.programPath ?? '').trim()
    rule.programArguments = normalizeProgramArguments(draft.programArguments)
    rule.programName = String(draft.programName ?? '').trim()
    rule.programIcon = String(draft.programIcon ?? '').trim()
    rule.programMissing = Boolean(draft.programMissing)
    scheduleSaveRules()

    return true
  }

  function removeRule(id) {
    state.rules = state.rules.filter(rule => rule.id !== id)
    ensureSelection(SCOPE_KINDS.category)
    ensureSelection(SCOPE_KINDS.app)
    scheduleSaveRules()
  }

  function updateRuleActionName(rule, actionName) {
    if (!rule) {
      return
    }

    rule.actionName = String(actionName ?? '').trim()
    scheduleSaveRules()
  }

  function startRecording(target) {
    if (normalizeActionType(target?.actionType) !== ACTION_TYPES.hotkey) {
      setMessage('只有快捷键命令需要录入快捷键。', 'error')
      return
    }

    stopRecording()

    if (!webView.isAvailable()) {
      setMessage('浏览器预览无法拦截系统快捷键，请在桌面应用中录制。', 'error')
      return
    }

    const requestId = createRequestId()
    state.recordingHotkeyTarget = target
    state.recordingHotkeyRequestId = requestId
    webView.postSilent({
      type: WEBVIEW_MESSAGE_TYPES.startHotkeyRecording,
      requestId
    })
    setMessage('正在录制快捷键，松开所有按键后完成。')
  }

  function startGestureRecording() {
    if (state.gestureRecordingActive) {
      stopGestureRecording()
      state.gestureRecognitionMessage = '已停止录制。'
      return
    }

    if (!webView.isAvailable()) {
      setMessage('浏览器预览无法录制系统鼠标手势，请在桌面应用中录制。', 'error')
      return
    }

    const requestId = createRequestId()
    state.gestureRecordingActive = true
    state.gestureRecordingRequestId = requestId
    state.gestureDraft.patternText = ''
    state.gestureRecognitionMessage = '录制中，再点一次停止。按住右键或中键绘制手势。'
    webView.postSilent({
      type: WEBVIEW_MESSAGE_TYPES.startGestureRecording,
      requestId
    })
  }

  function stopGestureRecording() {
    if (!state.gestureRecordingActive) {
      return
    }

    state.gestureRecordingActive = false
    state.gestureRecordingRequestId = ''
    webView.postSilent({ type: WEBVIEW_MESSAGE_TYPES.stopGestureRecording })
  }

  function stopRecording() {
    if (!state.recordingHotkeyRequestId) {
      return
    }

    webView.postSilent({ type: WEBVIEW_MESSAGE_TYPES.stopHotkeyRecording })
    state.recordingHotkeyTarget = null
    state.recordingHotkeyRequestId = ''
  }

  function isRecordingHotkey(target) {
    return state.recordingHotkeyTarget === target
  }

  function applySelectedProgram(message) {
    const path = String(message.path ?? '').trim()
    if (!path) {
      return
    }

    state.gestureDraft.programPath = path
    state.gestureDraft.programName = String(message.displayName ?? message.name ?? '').trim()
    state.gestureDraft.programIcon = String(message.icon ?? '').trim()
    state.gestureDraft.programMissing = false
  }

  function openGestureEditor(mode, rule, scopeKind, scopeName) {
    setGesturePaused(true)
    state.gestureEditorMode = mode
    state.gestureEditorRuleId = rule?.id ?? ''
    state.gestureEditorScopeKind = scopeKind
    state.gestureEditorScopeName = scopeName
    state.gestureRecordingActive = false
    state.gestureRecordingRequestId = ''
    state.gestureDraft = {
      actionName: rule?.actionName ?? '',
      patternText: rule?.patternText ?? '',
      mouseButton: normalizeMouseButton(rule?.mouseButton),
      keysText: rule?.keysText ?? '',
      actionType: normalizeActionType(rule?.actionType),
      windowOperation: normalizeWindowOperation(rule?.windowOperation),
      volumeOperation: normalizeVolumeOperation(rule?.volumeOperation),
      brightnessOperation: normalizeBrightnessOperation(rule?.brightnessOperation),
      amount: normalizeAmount(rule?.amount),
      programPath: String(rule?.programPath ?? '').trim(),
      programArguments: normalizeProgramArguments(rule?.programArguments),
      programName: String(rule?.programName ?? '').trim(),
      programIcon: String(rule?.programIcon ?? '').trim(),
      programMissing: Boolean(rule?.programMissing)
    }
    state.gestureRecognitionMessage = '点击开始录制。再次点击可停止。'
    state.gestureEditorOpen = true
  }

  function applyRecordedGesture(message) {
    if (!state.gestureRecordingActive || message.requestId !== state.gestureRecordingRequestId) {
      return
    }

    const pattern = Array.isArray(message.pattern) ? message.pattern.filter(Boolean) : []
    state.gestureDraft.patternText = toPatternText(pattern)
    state.gestureDraft.mouseButton = normalizeMouseButton(message.button)
    state.gestureRecordingActive = false
    state.gestureRecordingRequestId = ''

    if (pattern.length === 0) {
      state.gestureRecognitionMessage = '未识别到有效手势。'
      return
    }

    if (findDuplicateGestureRule(pattern, state.gestureDraft.mouseButton)) {
      state.gestureRecognitionMessage = '已识别手势。⚠ 当前作用域已存在相同手势，请更换。'
      setMessage('当前作用域已存在相同手势，未新增规则。', 'error')
      return
    }

    state.gestureRecognitionMessage = '已识别手势。'
    persistGestureEditor()
  }

  function applyRecordedHotkey(message) {
    if (message.requestId !== state.recordingHotkeyRequestId) {
      return
    }

    const target = state.recordingHotkeyTarget
    state.recordingHotkeyTarget = null
    state.recordingHotkeyRequestId = ''

    if (!target) {
      return
    }

    const keys = Array.isArray(message.keys) ? message.keys.filter(Boolean) : []
    if (keys.length === 0) {
      setMessage('未录制到有效快捷键。', 'error')
      return
    }

    const keysText = keys.join(' + ')
    target.keysText = keysText
    if (target === state.gestureDraft) {
      state.gestureDraft.keysText = keysText
      state.gestureDraft.actionName = getActionName(state.gestureDraft)
    }

    if (state.gestureEditorMode === 'edit') {
      const rule = state.rules.find(item => item.id === state.gestureEditorRuleId)
      if (rule) {
        rule.keysText = keysText
      }
    }

    setMessage('已录制快捷键。', 'success')
    if (state.gestureEditorOpen) {
      persistGestureEditor()
    } else {
      scheduleSaveRules()
    }
  }

  function findDuplicateGestureRule(pattern, mouseButton) {
    const scopeKind = state.gestureEditorScopeKind
    const scopeName = state.gestureEditorScopeName
    const patternText = toPatternText(pattern)
    const normalizedMouseButton = normalizeMouseButton(mouseButton)

    return state.rules.find(
      rule =>
        rule.id !== state.gestureEditorRuleId &&
        rule.scopeKind === scopeKind &&
        rule.scopeName === scopeName &&
        normalizeMouseButton(rule.mouseButton) === normalizedMouseButton &&
        toPatternText(parsePattern(rule.patternText)) === patternText
    )
  }

  function getActionName(source) {
    const actionName = String(source?.actionName ?? '').trim()
    return actionName || getDefaultActionName(source)
  }

  function getDefaultActionName(source) {
    if (normalizeActionType(source?.actionType) === ACTION_TYPES.hotkey) {
      const keys = parseKeys(source?.keysText)
      return keys.length > 0 ? `快捷键：${keys.join(' + ')}` : '快捷键'
    }

    if (normalizeActionType(source?.actionType) === ACTION_TYPES.program) {
      return source?.programName || '运行程序'
    }

    return String(getActionLabel(source) || getGestureMnemonic(source)).trim()
  }

  return {
    addRule,
    applyRecordedGesture,
    applyRecordedHotkey,
    applySelectedProgram,
    closeGestureEditor,
    isRecordingHotkey,
    openAddRule,
    openEditRule,
    persistGestureEditor,
    removeRule,
    saveGestureEditor,
    startGestureRecording,
    startRecording,
    stopGestureRecording,
    stopRecording,
    updateRuleActionName
  }
}

function normalizeProgramArguments(argumentsList) {
  return (Array.isArray(argumentsList) ? argumentsList : [])
    .map(argument => String(argument ?? '').trim())
    .filter(Boolean)
}
