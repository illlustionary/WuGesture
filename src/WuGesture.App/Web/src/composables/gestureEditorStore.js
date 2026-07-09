import { computed, proxyRefs, reactive, ref } from "vue";
import { useToast } from "vue-toastification";
import {
  ACTION_TYPES,
  BRIGHTNESS_OPERATIONS,
  EDGE_LOCATIONS,
  SCOPE_KINDS,
  VOLUME_OPERATIONS,
  WEBVIEW_MESSAGE_TYPES,
  WINDOW_OPERATIONS
} from "../constants/gestureEditorOptions";
import {
  DEFAULT_APPLICATIONS,
  DEFAULT_RULES,
  DEFAULT_UI_SETTINGS
} from "../constants/gestureEditorDefaults";
import {
  cloneUiSettings,
  createDefaultUiSettings,
  isSameApplicationIdentity,
  normalizeExcludedApplication,
  normalizeExcludedApplications,
  normalizeActionType,
  normalizeAmount,
  normalizeBrightnessOperation,
  normalizeEdgeActionInPlace,
  normalizeEdgeActions,
  normalizeMouseButton,
  normalizeUiSettings,
  normalizeVolumeOperation,
  normalizeWindowOperation,
  parseKeys,
  parsePattern,
  toPatternText
} from "../utils/gestureEditorNormalizers";
import {
  getActionLabel,
  getEdgeActionLabel,
  getGestureMnemonic
} from "../utils/gestureEditorFormatters";
import {
  collectAppItems as collectAppItemsFromState,
  collectCategoryItems as collectCategoryItemsFromState
} from "../utils/gestureEditorCollections";
import {
  buildConfigPayload,
  getWebDavSignature as createWebDavSignature
} from "../utils/gestureEditorPayloads";
import {
  createEmptyGestureDraft,
  createRuleModel,
  toViewApplication,
  toViewRule as toViewRuleModel
} from "../utils/gestureEditorViewModels";
import { useGestureEditorApplicationPicker } from "./useGestureEditorApplicationPicker";
import { useCategoryApplications } from "./gestureEditor/useCategoryApplications";
import { useGestureEditorNotifications } from "./gestureEditor/useGestureEditorNotifications";
import { useGestureEditorWebViewBridge } from "./gestureEditor/useGestureEditorWebViewBridge";
import { useGestureApplications } from "./gestureEditor/useGestureApplications";
import { useGestureScopes } from "./gestureEditor/useGestureScopes";

const state = reactive({
  statusText: "启动中",
  statusState: "idle",
  configPath: "读取中",
  configMessage: "",
  configMessageState: "idle",
  rules: [],
  applications: [],
  edgeActions: [],
  uiSettings: createDefaultUiSettings(),
  nextId: 1,
  selectedCategory: "",
  selectedApp: "",
  applicationPickerOpen: false,
  applicationPickerCategory: "",
  applicationPickerScopeKind: "",
  applicationPickerTarget: "scope",
  recordingHotkeyTarget: null,
  recordingHotkeyRequestId: "",
  gestureEditorOpen: false,
  gestureEditorMode: "add",
  gestureEditorRuleId: "",
  gestureEditorScopeKind: SCOPE_KINDS.global,
  gestureEditorScopeName: "",
  gestureDraft: createEmptyGestureDraft(),
  gestureRecognitionMessage: "",
  gestureRecordingActive: false,
  gestureRecordingRequestId: "",
  webDavTesting: false,
  webDavTestedSignature: ""
});

const activeScope = ref(SCOPE_KINDS.global);
const initialized = ref(false);
let autoSaveTimer = 0;
let pendingAutoSaveOptions = {};
let toast = null;
let suppressNextConfigResultToast = false;
let preserveLocalEdgeActions = false;
let pendingWebDavTestSignature = "";

const notifications = useGestureEditorNotifications({
  state,
  getAutoSaveTimer: () => autoSaveTimer,
  getToast: () => toast
});

const webView = useGestureEditorWebViewBridge({ notifications });

export function useGestureEditorStore() {
  if (!toast) {
    toast = useToast();
  }

  const globalRules = computed(() => getRulesForScope(SCOPE_KINDS.global));
  const categoryRules = computed(() => getRulesByKind(SCOPE_KINDS.category));
  const appRules = computed(() => getRulesByKind(SCOPE_KINDS.app));
  const categoryItems = computed(() => collectCategoryItems());
  const appItems = computed(() => collectAppItems());

  const selectedScopeName = computed(() => getSelectedName(activeScope.value));
  const visibleRules = computed(() => getVisibleRules(activeScope.value));

  return proxyRefs({
    state,
    activeScope,
    globalRules,
    categoryRules,
    appRules,
    categoryItems,
    appItems,
    selectedScopeName,
    visibleRules,
    initialize,
    setActiveScope,
    selectScope,
    getRulesForScope,
    getScopeItems,
    getSelectedName,
    getApplicationsForCategory,
    getApplication,
    updateApplicationDisplayName,
    updateApplicationCategory,
    assignSelectedAppToCategory,
    removeAppFromCategory,
    addRule,
    removeRule,
    updateRuleActionName,
    openAddRule,
    openEditRule,
    closeGestureEditor,
    persistGestureEditor,
    saveGestureEditor,
    createScopeTarget,
    renameSelectedScope,
    deleteSelectedScope,
    openApplicationPicker,
    closeApplicationPicker,
    openExcludedApplicationPicker,
    selectApplication,
    pickApplicationWindow,
    saveRules,
    reloadRules,
    resetRules,
    updateEdgeAction,
    updateExcludedApplication,
    removeExcludedApplication,
    startGestureRecording,
    stopGestureRecording,
    startRecording,
    stopRecording,
    isRecordingHotkey,
    getGestureMnemonic,
    getActionLabel,
    getEdgeActionLabel,
    getUiSettingsSnapshot,
    saveUiSettings,
    resetUiSettings,
    testWebDavConnection,
    saveConfigToWebDav,
    restoreConfigFromWebDav,
    getWebDavSignature,
    isWebDavTested,
    windowOperations: WINDOW_OPERATIONS,
    volumeOperations: VOLUME_OPERATIONS,
    brightnessOperations: BRIGHTNESS_OPERATIONS,
    edgeLocations: EDGE_LOCATIONS
  });
}

function initialize() {
  if (initialized.value) {
    return;
  }

  initialized.value = true;

  if (!webView.isAvailable()) {
    state.statusText = "浏览器预览";
    state.statusState = "idle";
    state.configPath = "内置默认规则";
    replaceConfig(DEFAULT_RULES, DEFAULT_APPLICATIONS, DEFAULT_UI_SETTINGS);
    return;
  }

  webView.addMessageListener((event) => {
    handleMessage(event.data);
  });
  webView.postSilent(WEBVIEW_MESSAGE_TYPES.getStatus);
}

function handleMessage(message) {
  if (message.type === WEBVIEW_MESSAGE_TYPES.status) {
    state.statusText = message.status === "running" ? "运行中" : message.status;
    state.statusState = message.status === "running" ? "running" : "idle";
    return;
  }

  if (message.type === WEBVIEW_MESSAGE_TYPES.rules) {
    state.configPath = message.configPath;
    replaceConfig(
      message.rules ?? [],
      message.applications ?? [],
      message.uiSettings ?? DEFAULT_UI_SETTINGS,
      message.edgeActions ?? [],
      { preserveEdgeActions: preserveLocalEdgeActions }
    );
    return;
  }

  if (message.type === WEBVIEW_MESSAGE_TYPES.configResult) {
    const notify = !suppressNextConfigResultToast || !message.success;
    suppressNextConfigResultToast = false;
    if (message.success) {
      window.setTimeout(() => {
        preserveLocalEdgeActions = false;
      }, 500);
    } else {
      preserveLocalEdgeActions = false;
    }
    setConfigResultMessage(message.message, message.success, notify);
    return;
  }

  if (message.type === WEBVIEW_MESSAGE_TYPES.webDavResult) {
    handleWebDavResult(message);
    return;
  }

  if (message.type === WEBVIEW_MESSAGE_TYPES.applicationSelected) {
    addSelectedApplication(message);
    return;
  }

  if (message.type === WEBVIEW_MESSAGE_TYPES.gestureRecorded) {
    applyRecordedGesture(message);
    return;
  }

  if (message.type === WEBVIEW_MESSAGE_TYPES.hotkeyRecorded) {
    applyRecordedHotkey(message);
  }
}

function replaceConfig(rules, applications, uiSettings = DEFAULT_UI_SETTINGS, edgeActions = [], options = {}) {
  state.rules = rules.map((rule) => toViewRule(rule));
  state.applications = applications.map((application) => toViewApplication(application));
  if (!options.preserveEdgeActions) {
    state.edgeActions = normalizeEdgeActions(edgeActions);
  }
  state.uiSettings = normalizeUiSettings(uiSettings);
  setGesturePaused(state.uiSettings.appBehavior.gesturePaused);
  ensureSelection(SCOPE_KINDS.category);
  ensureSelection(SCOPE_KINDS.app);
}

let scopeActions;

const applicationActions = useGestureApplications({
  state,
  getSelectedName: (kind) => scopeActions.getSelectedName(kind),
  notifications,
  scheduleSaveRules
});

scopeActions = useGestureScopes({
  state,
  activeScope,
  collectAppItems: () => collectAppItemsFromState(scopeActions.getRulesByKind(SCOPE_KINDS.app), state.applications),
  collectCategoryItems: () => collectCategoryItemsFromState(scopeActions.getRulesByKind(SCOPE_KINDS.category), state.applications),
  createRule,
  notifications,
  scheduleSaveRules
});

const categoryApplicationActions = useCategoryApplications({
  state,
  ensureApplication: applicationActions.ensureApplication,
  getSelectedName: (kind) => scopeActions.getSelectedName(kind),
  notifications,
  scheduleSaveRules,
  setSelectedName: (kind, name) => scopeActions.setSelectedName(kind, name)
});

const {
  createScopeTarget,
  deleteSelectedScope,
  ensureSelection,
  getFirstScopeName,
  getRulesByKind,
  getRulesForScope,
  getScopeItems,
  getSelectedName,
  getVisibleRules,
  renameSelectedScope,
  selectScope,
  setActiveScope,
  setSelectedName
} = scopeActions;

const {
  ensureApplication,
  getApplication,
  getApplicationsForCategory,
  updateApplicationCategory,
  updateApplicationDisplayName
} = applicationActions;

const {
  assignSelectedAppToCategory,
  removeAppFromCategory
} = categoryApplicationActions;

const collectCategoryItems = () => getScopeItems(SCOPE_KINDS.category);
const collectAppItems = () => getScopeItems(SCOPE_KINDS.app);

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

const applicationPicker = useGestureEditorApplicationPicker({
  state,
  addExcludedApplication,
  closeApplicationPickerState,
  createRequestId,
  createRule,
  ensureApplication,
  getScopeItems,
  notifications,
  scheduleSaveRules,
  setSelectedName,
  webView
});

function openApplicationPicker(categoryName = "", scopeKind = SCOPE_KINDS.category) {
  applicationPicker.openApplicationPicker(categoryName, scopeKind);
}

function openExcludedApplicationPicker() {
  applicationPicker.openExcludedApplicationPicker();
}

function closeApplicationPicker() {
  applicationPicker.closeApplicationPicker();
}

function closeApplicationPickerState() {
  state.applicationPickerOpen = false;
  state.applicationPickerCategory = "";
  state.applicationPickerScopeKind = "";
  state.applicationPickerTarget = "scope";
}

function selectApplication(categoryName = "") {
  applicationPicker.selectApplication(categoryName);
}

function pickApplicationWindow(categoryName = "") {
  applicationPicker.pickApplicationWindow(categoryName);
}

function addSelectedApplication(message) {
  applicationPicker.addSelectedApplication(message);
}

function addExcludedApplication(message) {
  const exclusion = normalizeExcludedApplication(message);
  if (!exclusion.name && !exclusion.path) {
    return;
  }

  const excludedApplications = state.uiSettings.appBehavior.excludedApplications;
  const existing = excludedApplications.find((item) => isSameApplicationIdentity(item, exclusion));
  if (existing) {
    existing.name = exclusion.name || existing.name;
    existing.displayName = exclusion.displayName || existing.displayName;
    existing.path = exclusion.path || existing.path;
    existing.icon = exclusion.icon || existing.icon || "";
  } else {
    excludedApplications.push(exclusion);
  }

  saveRules({ notifyPreview: false, notifyResult: false });
  setMessage("已添加排除项。", "success");
}

function updateExcludedApplication(application, patch = {}) {
  if (!application) {
    return;
  }

  Object.assign(application, patch);
  const normalized = normalizeExcludedApplications(state.uiSettings.appBehavior.excludedApplications);
  state.uiSettings.appBehavior.excludedApplications = normalized;
  saveRules({ notifyPreview: false, notifyResult: false });
}

function removeExcludedApplication(index) {
  if (index < 0 || index >= state.uiSettings.appBehavior.excludedApplications.length) {
    return;
  }

  state.uiSettings.appBehavior.excludedApplications.splice(index, 1);
  saveRules({ notifyPreview: false, notifyResult: false });
  setMessage("已删除排除项。", "success");
}

function saveRules(options = {}) {
  markSaveActivity();

  if (autoSaveTimer) {
    clearTimeout(autoSaveTimer);
    autoSaveTimer = 0;
    pendingAutoSaveOptions = {};
  }

  const payload = getConfigPayload();

  if (payload.rules.length === 0) {
    setMessage("至少保留一条规则。", "error");
    return;
  }

  if (options.notifyResult === false) {
    suppressNextConfigResultToast = true;
  }

  postWebMessage(
    {
      type: WEBVIEW_MESSAGE_TYPES.saveRules,
      ...payload
    },
    { notifyPreview: options.notifyPreview !== false }
  );
}

function scheduleSaveRules(options = {}) {
  markSaveActivity();

  if (!initialized.value) {
    return;
  }

  if (!webView.isAvailable()) {
    return;
  }

  pendingAutoSaveOptions = {
    ...pendingAutoSaveOptions,
    ...options
  };

  if (autoSaveTimer) {
    clearTimeout(autoSaveTimer);
  }

  autoSaveTimer = window.setTimeout(() => {
    const options = pendingAutoSaveOptions;
    autoSaveTimer = 0;
    pendingAutoSaveOptions = {};
    saveRules(options);
  }, 250);
}

function reloadRules() {
  preserveLocalEdgeActions = false;
  postWebMessage({ type: WEBVIEW_MESSAGE_TYPES.reloadRules });
}

function resetRules() {
  preserveLocalEdgeActions = false;
  postWebMessage({ type: WEBVIEW_MESSAGE_TYPES.resetRules });
}

function getUiSettingsSnapshot() {
  return cloneUiSettings(state.uiSettings);
}

function saveUiSettings(nextSettings, options = {}) {
  const previousPaused = Boolean(state.uiSettings.appBehavior.gesturePaused);
  state.uiSettings = normalizeUiSettings(nextSettings);
  const nextPaused = Boolean(state.uiSettings.appBehavior.gesturePaused);
  if (previousPaused !== nextPaused) {
    setGesturePaused(nextPaused);
  }
  saveRules({
    notifyPreview: Boolean(options.notify),
    notifyResult: Boolean(options.notify)
  });
  if (options.notify) {
    setMessage("已保存设置。", "success");
  }
}

function resetUiSettings() {
  state.uiSettings = createDefaultUiSettings();
  setGesturePaused(false);
  state.webDavTestedSignature = "";
  pendingWebDavTestSignature = "";
  saveRules({ notifyPreview: false, notifyResult: false });
  setMessage("已恢复默认设置。", "success");
}

function testWebDavConnection() {
  const payload = getConfigPayload();
  const signature = getWebDavSignature(payload.uiSettings.webDav);
  if (!payload.uiSettings.webDav.address) {
    setMessage("请先填写 WebDAV 地址。", "error");
    return;
  }

  if (!webView.isAvailable()) {
    setMessage("浏览器预览中无法测试 WebDAV。", "error");
    return;
  }

  state.webDavTesting = true;
  state.webDavTestedSignature = "";
  pendingWebDavTestSignature = signature;
  postWebMessage({
    type: WEBVIEW_MESSAGE_TYPES.webDavTest,
    ...payload
  });
}

function saveConfigToWebDav() {
  const payload = getConfigPayload();
  if (payload.rules.length === 0) {
    setMessage("至少保留一条规则。", "error");
    return;
  }

  if (!isWebDavTested(payload.uiSettings.webDav)) {
    setMessage("请先测试 WebDAV 连接。", "error");
    return;
  }

  postWebMessage({
    type: WEBVIEW_MESSAGE_TYPES.webDavSave,
    ...payload
  });
}

function restoreConfigFromWebDav() {
  const payload = getConfigPayload();
  if (!isWebDavTested(payload.uiSettings.webDav)) {
    setMessage("请先测试 WebDAV 连接。", "error");
    return;
  }

  postWebMessage({
    type: WEBVIEW_MESSAGE_TYPES.webDavRestore,
    ...payload
  });
}

function updateEdgeAction(action, patch = {}, options = {}) {
  if (!action) {
    return;
  }

  preserveLocalEdgeActions = true;
  Object.assign(action, patch);
  normalizeEdgeActionInPlace(action);
  scheduleSaveRules(options);
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
  postWebMessageSilently({
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
  postWebMessageSilently({ type: WEBVIEW_MESSAGE_TYPES.stopGestureRecording });
}

function stopRecording() {
  if (!state.recordingHotkeyRequestId) {
    return;
  }

  postWebMessageSilently({ type: WEBVIEW_MESSAGE_TYPES.stopHotkeyRecording });
  state.recordingHotkeyTarget = null;
  state.recordingHotkeyRequestId = "";
}

function isRecordingHotkey(target) {
  return state.recordingHotkeyTarget === target;
}

function setMessage(message, stateName = "idle", options = {}) {
  notifications.show(message, stateName, options);
}

function setConfigResultMessage(message, success, notify) {
  notifications.showConfigResult(message, success, notify);
}

function markSaveActivity() {
  notifications.markSaveActivity();
}

function postWebMessage(message, options = {}) {
  webView.post(message, options);
}

function postWebMessageSilently(message) {
  webView.postSilent(message);
}

function setGesturePaused(paused) {
  postWebMessageSilently({
    type: WEBVIEW_MESSAGE_TYPES.setGesturePaused,
    paused
  });
}

function toViewRule(rule) {
  return toViewRuleModel(rule, createRuleId(state.nextId++));
}

function createRule(scopeKind, scopeName, values = {}) {
  if (scopeKind === SCOPE_KINDS.app) {
    ensureApplication(scopeName);
  }

  return createRuleModel(scopeKind, scopeName, values, createRuleId(state.nextId++));
}

function getConfigPayload() {
  return buildConfigPayload(state);
}

function handleWebDavResult(message) {
  if (message.operation === "test") {
    state.webDavTesting = false;
    if (message.success) {
      state.webDavTestedSignature = pendingWebDavTestSignature;
    } else {
      state.webDavTestedSignature = "";
    }
    pendingWebDavTestSignature = "";
  }

  setMessage(message.message, message.success ? "success" : "error");
}

function isWebDavTested(settings = state.uiSettings.webDav) {
  const signature = getWebDavSignature(settings);
  return Boolean(signature && signature === state.webDavTestedSignature);
}

function getWebDavSignature(settings = state.uiSettings.webDav) {
  return createWebDavSignature(settings);
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

function createRuleId(seed) {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  return `rule-${seed}-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function createRequestId() {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  return `request-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}
