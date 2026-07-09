import { ACTION_TYPES, SCOPE_KINDS, WEBVIEW_MESSAGE_TYPES } from "../../constants/gestureEditorOptions";
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
} from "../../utils/gestureEditorNormalizers";
import { getActionLabel, getGestureMnemonic } from "../../utils/gestureEditorFormatters";
import { createEmptyGestureDraft } from "../../utils/gestureEditorViewModels";

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
    openAddRule(kind, name);
  }

  function openAddRule(kind = activeScope.value, name = getSelectedName(kind)) {
    if (kind === SCOPE_KINDS.global) {
      openGestureEditor("add", null, SCOPE_KINDS.global, "");
      return;
    }

    const scopeName = String(name || getFirstScopeName(kind)).trim();
    if (!scopeName) {
      setMessage(kind === SCOPE_KINDS.category ? "先新增或选择一个分类。" : "先新增或选择一个程序。", "error");
      return;
    }

    setSelectedName(kind, scopeName);
    openGestureEditor("add", null, kind, scopeName);
  }

  function openEditRule(ruleId) {
    const rule = state.rules.find((item) => item.id === ruleId);
    if (!rule) {
      return;
    }

    openGestureEditor("edit", rule, rule.scopeKind, rule.scopeName);
  }

  function closeGestureEditor() {
    stopGestureRecording();
    stopRecording();
    state.gestureEditorOpen = false;
    state.gestureRecognitionMessage = "";
    state.gestureDraft = createEmptyGestureDraft();
    setGesturePaused(false);
  }

  function persistGestureEditor() {
    return commitGestureEditor(false);
  }

  function saveGestureEditor() {
    return commitGestureEditor(true);
  }

  function commitGestureEditor(closeAfterSave) {
    const draft = state.gestureDraft;
    const pattern = parsePattern(draft.patternText);
    const actionType = normalizeActionType(draft.actionType);

    if (pattern.length === 0) {
      if (closeAfterSave) {
        state.gestureRecognitionMessage = "请先录制手势。";
      }
      return false;
    }

    if (actionType === ACTION_TYPES.hotkey && parseKeys(draft.keysText).length === 0) {
      if (closeAfterSave) {
        state.gestureRecognitionMessage = "请先录入快捷键。";
      }
      return false;
    }

    if (actionType === ACTION_TYPES.window && !normalizeWindowOperation(draft.windowOperation)) {
      if (closeAfterSave) {
        state.gestureRecognitionMessage = "请选择窗口控制操作。";
      }
      return false;
    }

    const actionName = getCommandActionName(draft);
    draft.actionName = actionName;

    let rule = null;
    if (state.gestureEditorMode === "edit") {
      rule = state.rules.find((item) => item.id === state.gestureEditorRuleId);
      if (!rule) {
        if (closeAfterSave) {
          closeGestureEditor();
        }
        return false;
      }
    } else {
      rule = createRule(
        state.gestureEditorScopeKind,
        state.gestureEditorScopeName,
        {
          actionName,
          patternText: toPatternText(pattern),
          mouseButton: normalizeMouseButton(draft.mouseButton),
          keysText: draft.keysText,
          actionType,
          windowOperation: normalizeWindowOperation(draft.windowOperation),
          volumeOperation: normalizeVolumeOperation(draft.volumeOperation),
          brightnessOperation: normalizeBrightnessOperation(draft.brightnessOperation),
          amount: normalizeAmount(draft.amount)
        }
      );
      state.rules.push(rule);
      state.gestureEditorRuleId = rule.id;
      state.gestureEditorMode = "edit";
    }

    rule.actionName = actionName;
    rule.patternText = toPatternText(pattern);
    rule.mouseButton = normalizeMouseButton(draft.mouseButton);
    rule.keysText = draft.keysText;
    rule.actionType = actionType;
    rule.windowOperation = normalizeWindowOperation(draft.windowOperation);
    rule.volumeOperation = normalizeVolumeOperation(draft.volumeOperation);
    rule.brightnessOperation = normalizeBrightnessOperation(draft.brightnessOperation);
    rule.amount = normalizeAmount(draft.amount);
    scheduleSaveRules();

    if (closeAfterSave) {
      closeGestureEditor();
    }

    return true;
  }

  function removeRule(id) {
    state.rules = state.rules.filter((rule) => rule.id !== id);
    ensureSelection(SCOPE_KINDS.category);
    ensureSelection(SCOPE_KINDS.app);
    scheduleSaveRules();
  }

  function updateRuleActionName(rule, actionName) {
    if (!rule) {
      return;
    }

    rule.actionName = String(actionName ?? "").trim();
    scheduleSaveRules();
  }

  function startRecording(target) {
    if (normalizeActionType(target?.actionType) !== ACTION_TYPES.hotkey) {
      setMessage("只有快捷键命令需要录入快捷键。", "error");
      return;
    }

    stopRecording();

    if (!webView.isAvailable()) {
      setMessage("浏览器预览无法拦截系统快捷键，请在桌面应用中录制。", "error");
      return;
    }

    const requestId = createRequestId();
    state.recordingHotkeyTarget = target;
    state.recordingHotkeyRequestId = requestId;
    webView.postSilent({
      type: WEBVIEW_MESSAGE_TYPES.startHotkeyRecording,
      requestId
    });
    setMessage("正在录制快捷键，松开所有按键后完成。");
  }

  function startGestureRecording() {
    if (state.gestureRecordingActive) {
      stopGestureRecording();
      state.gestureRecognitionMessage = "已停止录制。";
      return;
    }

    if (!webView.isAvailable()) {
      setMessage("浏览器预览无法录制系统鼠标手势，请在桌面应用中录制。", "error");
      return;
    }

    const requestId = createRequestId();
    state.gestureRecordingActive = true;
    state.gestureRecordingRequestId = requestId;
    state.gestureDraft.patternText = "";
    state.gestureRecognitionMessage = "录制中，再点一次停止。按住右键或中键绘制手势。";
    webView.postSilent({
      type: WEBVIEW_MESSAGE_TYPES.startGestureRecording,
      requestId
    });
  }

  function stopGestureRecording() {
    if (!state.gestureRecordingActive) {
      return;
    }

    state.gestureRecordingActive = false;
    state.gestureRecordingRequestId = "";
    webView.postSilent({ type: WEBVIEW_MESSAGE_TYPES.stopGestureRecording });
  }

  function stopRecording() {
    if (!state.recordingHotkeyRequestId) {
      return;
    }

    webView.postSilent({ type: WEBVIEW_MESSAGE_TYPES.stopHotkeyRecording });
    state.recordingHotkeyTarget = null;
    state.recordingHotkeyRequestId = "";
  }

  function isRecordingHotkey(target) {
    return state.recordingHotkeyTarget === target;
  }

  function openGestureEditor(mode, rule, scopeKind, scopeName) {
    setGesturePaused(true);
    state.gestureEditorMode = mode;
    state.gestureEditorRuleId = rule?.id ?? "";
    state.gestureEditorScopeKind = scopeKind;
    state.gestureEditorScopeName = scopeName;
    state.gestureRecordingActive = false;
    state.gestureRecordingRequestId = "";
    state.gestureDraft = {
      actionName: rule?.actionName ?? "",
      patternText: rule?.patternText ?? "",
      mouseButton: normalizeMouseButton(rule?.mouseButton),
      keysText: rule?.keysText ?? "",
      actionType: normalizeActionType(rule?.actionType),
      windowOperation: normalizeWindowOperation(rule?.windowOperation),
      volumeOperation: normalizeVolumeOperation(rule?.volumeOperation),
      brightnessOperation: normalizeBrightnessOperation(rule?.brightnessOperation),
      amount: normalizeAmount(rule?.amount)
    };
    state.gestureRecognitionMessage = "点击开始录制。再次点击可停止。";
    state.gestureEditorOpen = true;
  }

  function applyRecordedGesture(message) {
    if (!state.gestureRecordingActive || message.requestId !== state.gestureRecordingRequestId) {
      return;
    }

    const pattern = Array.isArray(message.pattern) ? message.pattern.filter(Boolean) : [];
    state.gestureDraft.patternText = toPatternText(pattern);
    state.gestureDraft.mouseButton = normalizeMouseButton(message.button);
    state.gestureRecognitionMessage = pattern.length > 0 ? "已识别手势。" : "未识别到有效手势。";
    state.gestureRecordingActive = false;
    state.gestureRecordingRequestId = "";
    persistGestureEditor();
  }

  function applyRecordedHotkey(message) {
    if (message.requestId !== state.recordingHotkeyRequestId) {
      return;
    }

    const target = state.recordingHotkeyTarget;
    state.recordingHotkeyTarget = null;
    state.recordingHotkeyRequestId = "";

    if (!target) {
      return;
    }

    const keys = Array.isArray(message.keys) ? message.keys.filter(Boolean) : [];
    if (keys.length === 0) {
      setMessage("未录制到有效快捷键。", "error");
      return;
    }

    const keysText = keys.join(" + ");
    target.keysText = keysText;
    if (target === state.gestureDraft) {
      state.gestureDraft.keysText = keysText;
      state.gestureDraft.actionName = getCommandActionName(state.gestureDraft);
    }

    if (state.gestureEditorMode === "edit") {
      const rule = state.rules.find((item) => item.id === state.gestureEditorRuleId);
      if (rule) {
        rule.keysText = keysText;
      }
    }

    setMessage("已录制快捷键。", "success");
    if (state.gestureEditorOpen) {
      persistGestureEditor();
    } else {
      scheduleSaveRules();
    }
  }

  function getCommandActionName(source) {
    return String(getActionLabel(source) || getGestureMnemonic(source)).trim();
  }

  return {
    addRule,
    applyRecordedGesture,
    applyRecordedHotkey,
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
  };
}
