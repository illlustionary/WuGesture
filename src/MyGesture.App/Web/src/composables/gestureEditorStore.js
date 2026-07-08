import { computed, proxyRefs, reactive, ref } from "vue";
import { useToast } from "vue-toastification";
import {
  BRIGHTNESS_OPERATIONS,
  EDGE_LOCATIONS,
  VOLUME_OPERATIONS,
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
  gestureEditorScopeKind: "global",
  gestureEditorScopeName: "",
  gestureDraft: createEmptyGestureDraft(),
  gestureRecognitionMessage: "",
  gestureRecordingActive: false,
  gestureRecordingRequestId: "",
  webDavTesting: false,
  webDavTestedSignature: ""
});

const activeScope = ref("global");
const initialized = ref(false);
let autoSaveTimer = 0;
let pendingAutoSaveOptions = {};
let toast = null;
let suppressNextConfigResultToast = false;
let preserveLocalEdgeActions = false;
let pendingWebDavTestSignature = "";
let pendingConfigResultToastTimer = 0;
let saveActivityVersion = 0;

export function useGestureEditorStore() {
  if (!toast) {
    toast = useToast();
  }

  const globalRules = computed(() => getRulesForScope("global"));
  const categoryRules = computed(() => getRulesByKind("category"));
  const appRules = computed(() => getRulesByKind("app"));
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

  if (!window.chrome?.webview) {
    state.statusText = "浏览器预览";
    state.statusState = "idle";
    state.configPath = "内置默认规则";
    replaceConfig(DEFAULT_RULES, DEFAULT_APPLICATIONS, DEFAULT_UI_SETTINGS);
    return;
  }

  window.chrome.webview.addEventListener("message", (event) => {
    handleMessage(event.data);
  });
  window.chrome.webview.postMessage("get-status");
}

function handleMessage(message) {
  if (message.type === "status") {
    state.statusText = message.status === "running" ? "运行中" : message.status;
    state.statusState = message.status === "running" ? "running" : "idle";
    return;
  }

  if (message.type === "rules") {
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

  if (message.type === "config-result") {
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

  if (message.type === "webdav-result") {
    handleWebDavResult(message);
    return;
  }

  if (message.type === "application-selected") {
    addSelectedApplication(message);
    return;
  }

  if (message.type === "gesture-recorded") {
    applyRecordedGesture(message);
    return;
  }

  if (message.type === "hotkey-recorded") {
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
  ensureSelection("category");
  ensureSelection("app");
}

function setActiveScope(scope) {
  activeScope.value = scope;
  ensureSelection(scope);
}

function selectScope(kind, name) {
  const trimmed = String(name ?? "").trim();
  if (!trimmed) {
    return;
  }

  setSelectedName(kind, trimmed);
}

function getRulesForScope(kind, name = "") {
  if (kind === "global") {
    return getRulesByKind("global");
  }

  const scopeName = String(name || getSelectedName(kind)).trim();
  if (!scopeName) {
    return [];
  }

  return state.rules.filter((rule) => rule.scopeKind === kind && rule.scopeName === scopeName);
}

function getVisibleRules(scope) {
  return getRulesForScope(scope, getSelectedName(scope));
}

function getRulesByKind(kind) {
  return state.rules.filter((rule) => rule.scopeKind === kind);
}

function getScopeItems(kind) {
  return kind === "category" ? collectCategoryItems() : collectAppItems();
}

function categoryRulesSnapshot() {
  return getRulesByKind("category");
}

function appRulesSnapshot() {
  return getRulesByKind("app");
}

function addRule(kind = activeScope.value, name = getSelectedName(kind)) {
  openAddRule(kind, name);
}

function openAddRule(kind = activeScope.value, name = getSelectedName(kind)) {
  if (kind === "global") {
    openGestureEditor("add", null, "global", "");
    return;
  }

  const scopeName = String(name || getFirstScopeName(kind)).trim();
  if (!scopeName) {
    setMessage(kind === "category" ? "先新增或选择一个分类。" : "先新增或选择一个程序。", "error");
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
  const actionName = String(draft.actionName ?? "").trim() || getGestureMnemonic(draft);
  const actionType = normalizeActionType(draft.actionType);

  if (pattern.length === 0) {
    if (closeAfterSave) {
      state.gestureRecognitionMessage = "请先录制手势。";
    }
    return false;
  }

  if (actionType === "hotkey" && parseKeys(draft.keysText).length === 0) {
    if (closeAfterSave) {
      state.gestureRecognitionMessage = "请先录入快捷键。";
    }
    return false;
  }

  if (actionType === "window" && !normalizeWindowOperation(draft.windowOperation)) {
    if (closeAfterSave) {
      state.gestureRecognitionMessage = "请选择窗口控制操作。";
    }
    return false;
  }

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
  ensureSelection("category");
  ensureSelection("app");
  scheduleSaveRules();
}

function updateRuleActionName(rule, actionName) {
  if (!rule) {
    return;
  }

  rule.actionName = String(actionName ?? "").trim();
  scheduleSaveRules();
}

function createScopeTarget(kind, name) {
  const trimmed = String(name ?? "").trim();
  if (!trimmed) {
    setMessage("请输入名称后再新增。", "error");
    return false;
  }

  if (!getScopeItems(kind).some((item) => item.name === trimmed)) {
    state.rules.push(createRule(kind, trimmed));
  }

  setSelectedName(kind, trimmed);
  setMessage("已新增。", "success");
  scheduleSaveRules();
  return true;
}

function renameSelectedScope(kind, nextName, sourceName = getSelectedName(kind)) {
  const name = String(nextName ?? "").trim();
  const currentName = String(sourceName ?? "").trim() || getSelectedName(kind);
  if (!name || !currentName || currentName === name) {
    return false;
  }

  const scopeItems = getScopeItems(kind);
  if (scopeItems.some((item) => item.name === name && item.name !== currentName)) {
    setMessage("名称已存在，请换一个分类名称。", "error");
    return false;
  }

  for (const rule of state.rules) {
    if (rule.scopeKind === kind && rule.scopeName === currentName) {
      rule.scopeName = name;
    }
  }

  if (kind === "category") {
    for (const application of state.applications) {
      if (application.category === currentName) {
        application.category = name;
      }
    }
  } else if (kind === "app") {
    const application = state.applications.find((item) => item.name === currentName);
    if (application) {
      application.name = name;
    }
  }

  setSelectedName(kind, name);
  scheduleSaveRules();
  setMessage(kind === "category" ? "已更新分类名称。" : "已更新程序名称。", "success");
  return true;
}

function deleteSelectedScope(kind = activeScope.value) {
  const name = getSelectedName(kind);
  if (!name) {
    return;
  }

  state.rules = state.rules.filter((rule) => !(rule.scopeKind === kind && rule.scopeName === name));
  if (kind === "category") {
    for (const application of state.applications) {
      if (application.category === name) {
        application.category = "";
      }
    }
  } else if (kind === "app") {
    state.applications = state.applications.filter((application) => application.name !== name);
  }

  ensureSelection(kind);
  setMessage("已删除当前项。", "success");
  scheduleSaveRules();
}

function getApplicationsForCategory(categoryName = getSelectedName("category")) {
  const trimmed = String(categoryName ?? "").trim();
  if (!trimmed) {
    return [];
  }

  return state.applications.filter((application) => application.category === trimmed);
}

function getApplication(appName = getSelectedName("app")) {
  const name = String(appName ?? "").trim();
  if (!name) {
    return null;
  }

  return state.applications.find((application) => application.name === name) ?? null;
}

function updateApplicationDisplayName(appName = getSelectedName("app"), displayName = "") {
  const name = String(appName ?? "").trim();
  if (!name) {
    setMessage("请先选择一个程序。", "error");
    return;
  }

  const application = ensureApplication(name);
  application.displayName = String(displayName ?? "").trim();
  scheduleSaveRules();
}

function updateApplicationCategory(appName = getSelectedName("app"), categoryName = "") {
  const name = String(appName ?? "").trim();
  if (!name) {
    setMessage("请先选择一个程序。", "error");
    return;
  }

  const application = ensureApplication(name);
  application.category = String(categoryName ?? "").trim();
  setMessage(application.category ? "已设置程序分类。" : "已清除程序分类。", "success");
  scheduleSaveRules();
}

function assignSelectedAppToCategory(categoryName = getSelectedName("category"), appName = getSelectedName("app")) {
  const category = String(categoryName ?? "").trim();
  const name = String(appName ?? "").trim();
  if (!category || !name) {
    setMessage("请先选择分类和程序。", "error");
    return;
  }

  const application = ensureApplication(name);
  application.category = category;
  setSelectedName("category", category);
  setMessage("已关联程序到分类。", "success");
  scheduleSaveRules();
}

function removeAppFromCategory(appName, categoryName = getSelectedName("category")) {
  const category = String(categoryName ?? "").trim();
  const name = String(appName ?? "").trim();
  if (!category || !name) {
    return;
  }

  const application = state.applications.find((item) => item.name === name && item.category === category);
  if (application) {
    application.category = "";
    setMessage("已移除分类关联。", "success");
    scheduleSaveRules();
  }
}

const applicationPicker = useGestureEditorApplicationPicker({
  state,
  addExcludedApplication,
  closeApplicationPickerState,
  createRequestId,
  createRule,
  ensureApplication,
  getScopeItems,
  postWebMessage,
  scheduleSaveRules,
  setMessage,
  setSelectedName
});

function openApplicationPicker(categoryName = "", scopeKind = "category") {
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
      type: "save-rules",
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

  if (!window.chrome?.webview) {
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
  postWebMessage({ type: "reload-rules" });
}

function resetRules() {
  preserveLocalEdgeActions = false;
  postWebMessage({ type: "reset-rules" });
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

  if (!window.chrome?.webview) {
    setMessage("浏览器预览中无法测试 WebDAV。", "error");
    return;
  }

  state.webDavTesting = true;
  state.webDavTestedSignature = "";
  pendingWebDavTestSignature = signature;
  postWebMessage({
    type: "webdav-test",
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
    type: "webdav-save",
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
    type: "webdav-restore",
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
  if (normalizeActionType(target?.actionType) !== "hotkey") {
    setMessage("只有快捷键命令需要录入快捷键。", "error");
    return;
  }

  stopRecording();

  if (!window.chrome?.webview) {
    setMessage("浏览器预览无法拦截系统快捷键，请在桌面应用中录制。", "error");
    return;
  }

  const requestId = createRequestId();
  state.recordingHotkeyTarget = target;
  state.recordingHotkeyRequestId = requestId;
  postWebMessageSilently({
    type: "start-hotkey-recording",
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

  if (!window.chrome?.webview) {
    setMessage("浏览器预览无法录制系统鼠标手势，请在桌面应用中录制。", "error");
    return;
  }

  const requestId = createRequestId();
  state.gestureRecordingActive = true;
  state.gestureRecordingRequestId = requestId;
  state.gestureDraft.patternText = "";
  state.gestureRecognitionMessage = "录制中，再点一次停止。按住右键或中键绘制手势。";
  window.chrome.webview.postMessage({
    type: "start-gesture-recording",
    requestId
  });
}

function stopGestureRecording() {
  if (!state.gestureRecordingActive) {
    return;
  }

  state.gestureRecordingActive = false;
  state.gestureRecordingRequestId = "";
  postWebMessageSilently({ type: "stop-gesture-recording" });
}

function stopRecording() {
  if (!state.recordingHotkeyRequestId) {
    return;
  }

  postWebMessageSilently({ type: "stop-hotkey-recording" });
  state.recordingHotkeyTarget = null;
  state.recordingHotkeyRequestId = "";
}

function isRecordingHotkey(target) {
  return state.recordingHotkeyTarget === target;
}

function ensureSelection(kind) {
  if (kind !== "category" && kind !== "app") {
    return;
  }

  const selectedName = getSelectedName(kind);
  const items = getScopeItems(kind);
  if (!items.some((item) => item.name === selectedName)) {
    setSelectedName(kind, items[0]?.name ?? "");
  }
}

function getSelectedName(kind) {
  if (kind === "category") {
    return state.selectedCategory;
  }

  if (kind === "app") {
    return state.selectedApp;
  }

  return "";
}

function setSelectedName(kind, name) {
  if (kind === "category") {
    state.selectedCategory = name;
  } else if (kind === "app") {
    state.selectedApp = name;
  }
}

function getFirstScopeName(kind) {
  return getScopeItems(kind)[0]?.name ?? "";
}

function setMessage(message, stateName = "idle", options = {}) {
  state.configMessage = message;
  state.configMessageState = stateName;
  if (options.notify !== false) {
    showToast(message, stateName);
  }
}

function setConfigResultMessage(message, success, notify) {
  const stateName = success ? "success" : "error";
  if (!success || !notify) {
    setMessage(message, stateName, { notify });
    return;
  }

  state.configMessage = message;
  state.configMessageState = stateName;
  scheduleConfigResultToast(message, stateName);
}

function markSaveActivity() {
  saveActivityVersion += 1;
  cancelPendingConfigResultToast();
}

function scheduleConfigResultToast(message, stateName) {
  const version = saveActivityVersion;
  cancelPendingConfigResultToast();
  pendingConfigResultToastTimer = window.setTimeout(() => {
    pendingConfigResultToastTimer = 0;
    if (version === saveActivityVersion && !autoSaveTimer) {
      showToast(message, stateName);
    }
  }, 1000);
}

function cancelPendingConfigResultToast() {
  if (!pendingConfigResultToastTimer) {
    return;
  }

  clearTimeout(pendingConfigResultToastTimer);
  pendingConfigResultToastTimer = 0;
}

function showToast(message, stateName = "idle") {
  const content = String(message ?? "").trim();
  if (!content || !toast) {
    return;
  }

  if (stateName === "success") {
    toast.success(content);
    return;
  }

  if (stateName === "error") {
    toast.error(content);
    return;
  }

  toast.info(content);
}

function postWebMessage(message, options = {}) {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage(message);
    return;
  }

  setMessage("浏览器预览中不会写入本机配置。", "idle", {
    notify: options.notifyPreview !== false
  });
}

function postWebMessageSilently(message) {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage(message);
  }
}

function setGesturePaused(paused) {
  postWebMessageSilently({
    type: "set-gesture-paused",
    paused
  });
}

function toViewRule(rule) {
  return toViewRuleModel(rule, createRuleId(state.nextId++));
}

function createRule(scopeKind, scopeName, values = {}) {
  if (scopeKind === "app") {
    ensureApplication(scopeName);
  }

  return createRuleModel(scopeKind, scopeName, values, createRuleId(state.nextId++));
}

function ensureApplication(name) {
  const trimmed = String(name ?? "").trim();
  let application = state.applications.find((item) => item.name === trimmed);
  if (!application) {
    application = { name: trimmed, displayName: trimmed, path: "", category: "", icon: "" };
    state.applications.push(application);
  }

  return application;
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

function collectCategoryItems() {
  return collectCategoryItemsFromState(categoryRulesSnapshot(), state.applications);
}

function collectAppItems() {
  return collectAppItemsFromState(appRulesSnapshot(), state.applications);
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
  if (!String(state.gestureDraft.actionName ?? "").trim()) {
    state.gestureDraft.actionName = getGestureMnemonic(state.gestureDraft);
  }
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
