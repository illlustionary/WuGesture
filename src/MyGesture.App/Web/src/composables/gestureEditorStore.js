import { computed, proxyRefs, reactive, ref } from "vue";
import { useToast } from "vue-toastification";

const DEFAULT_RULES = [
  {
    scope: "global",
    pattern: ["Left"],
    actionName: "Back",
    actionType: "hotkey",
    keys: ["Alt", "Left"]
  },
  {
    scope: "global",
    pattern: ["Right"],
    actionName: "Forward",
    actionType: "hotkey",
    keys: ["Alt", "Right"]
  },
  {
    scope: "global",
    pattern: ["Down", "Right"],
    actionName: "Close Tab",
    actionType: "hotkey",
    keys: ["Control", "W"]
  }
];

const DEFAULT_APPLICATIONS = [
  { name: "msedge", displayName: "Microsoft Edge", path: "", category: "浏览器", icon: "" },
  { name: "chrome", displayName: "Google Chrome", path: "", category: "浏览器", icon: "" }
];

const MOUSE_BUTTON_SYMBOLS = {
  right: "◑",
  middle: "●"
};

const DIRECTION_SYMBOLS = {
  Up: "↑",
  Down: "↓",
  Left: "←",
  Right: "→",
  UpLeft: "↖",
  UpRight: "↗",
  DownLeft: "↙",
  DownRight: "↘"
};

const WINDOW_OPERATIONS = [
  { value: "toggle-topmost", label: "置顶/取消置顶" },
  { value: "toggle-maximize", label: "最大化/还原" },
  { value: "minimize", label: "最小化" },
  { value: "close", label: "关闭窗口" }
];

const VOLUME_OPERATIONS = [
  { value: "increase", label: "音量 +" },
  { value: "decrease", label: "音量 -" },
  { value: "mute", label: "静音" }
];

const BRIGHTNESS_OPERATIONS = [
  { value: "increase", label: "亮度 +" },
  { value: "decrease", label: "亮度 -" }
];

const EDGE_LOCATIONS = {
  corner: [
    { value: "top-left", label: "左上角" },
    { value: "top-right", label: "右上角" },
    { value: "bottom-left", label: "左下角" },
    { value: "bottom-right", label: "右下角" }
  ],
  edge: [
    { value: "left", label: "左边" },
    { value: "right", label: "右边" },
    { value: "top", label: "上边" },
    { value: "bottom", label: "下边" }
  ]
};

const DEFAULT_EDGE_ACTIONS = [
  createDefaultEdgeAction("corner", "top-left"),
  createDefaultEdgeAction("corner", "top-right"),
  createDefaultEdgeAction("corner", "bottom-left"),
  createDefaultEdgeAction("corner", "bottom-right"),
  ...EDGE_LOCATIONS.edge.map((edge) => createDefaultEdgeAction("friction", edge.value)),
  ...EDGE_LOCATIONS.edge.flatMap((edge) => [
    createDefaultEdgeAction("wheel", edge.value, "up"),
    createDefaultEdgeAction("wheel", edge.value, "down")
  ])
];

const DEFAULT_UI_SETTINGS = {
  mouseTrail: {
    inactiveColor: "#AAAAAA",
    activeColor: "#87CEEB",
    inactiveThickness: 3,
    activeThickness: 3,
    thickness: 3,
    inactiveOpacity: 74,
    activeOpacity: 100
  },
  gestureHint: {
    fontFamily: "Segoe UI Semibold",
    fontSize: 22,
    textColor: "#FFFFFF",
    backgroundColor: "#12181F",
    backgroundOpacity: 90,
    width: 540,
    widthPercent: 28,
    autoWidth: true,
    height: 120,
    heightPercent: 11,
    cornerRadius: 28,
    bottomOffset: 140,
    bottomOffsetPercent: 13
  },
  appBehavior: {
    launchAtStartup: false,
    runAsAdministrator: false,
    closeButtonBehavior: "minimize-to-tray",
    gesturePaused: false,
    excludedApplications: []
  },
  webDav: {
    address: "",
    userName: "",
    password: "",
    remotePath: ""
  }
};

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
const pendingApplicationPickerRequests = new Map();
let autoSaveTimer = 0;
let pendingAutoSaveOptions = {};
let toast = null;
let suppressNextConfigResultToast = false;
let preserveLocalEdgeActions = false;
let pendingWebDavTestSignature = "";

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
    setMessage(message.message, message.success ? "success" : "error", { notify });
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

function openApplicationPicker(categoryName = "", scopeKind = "category") {
  state.applicationPickerCategory = String(categoryName ?? "").trim();
  state.applicationPickerScopeKind = scopeKind === "app" ? "app" : "category";
  state.applicationPickerTarget = "scope";
  state.applicationPickerOpen = true;
}

function openExcludedApplicationPicker() {
  state.applicationPickerCategory = "";
  state.applicationPickerScopeKind = "";
  state.applicationPickerTarget = "exclusion";
  state.applicationPickerOpen = true;
}

function closeApplicationPicker() {
  state.applicationPickerOpen = false;
  state.applicationPickerCategory = "";
  state.applicationPickerScopeKind = "";
  state.applicationPickerTarget = "scope";
}

function selectApplication(categoryName = "") {
  const category = String(categoryName || state.applicationPickerCategory || "").trim();
  const requestId = createRequestId();
  pendingApplicationPickerRequests.set(requestId, {
    target: state.applicationPickerTarget,
    scopeKind: state.applicationPickerScopeKind,
    category
  });
  closeApplicationPicker();
  postWebMessage({
    type: "select-application",
    requestId,
    category
  });
}

function pickApplicationWindow(categoryName = "") {
  const category = String(categoryName || state.applicationPickerCategory || "").trim();
  const requestId = createRequestId();
  pendingApplicationPickerRequests.set(requestId, {
    target: state.applicationPickerTarget,
    scopeKind: state.applicationPickerScopeKind,
    category
  });
  closeApplicationPicker();
  postWebMessage({
    type: "pick-application-window",
    requestId,
    category
  });
}

function addSelectedApplication(message) {
  const name = String(message.name ?? "").trim();
  if (!name) {
    return;
  }

  const requestContext = pendingApplicationPickerRequests.get(message.requestId) ?? null;
  pendingApplicationPickerRequests.delete(message.requestId);
  if (requestContext?.target === "exclusion") {
    addExcludedApplication(message);
    return;
  }

  const application = ensureApplication(name);
  application.displayName = String(message.displayName ?? application.displayName ?? name).trim();
  application.path = String(message.path ?? "").trim();
  const selectedCategory = String(message.category ?? "").trim();
  if (selectedCategory || requestContext?.scopeKind !== "app") {
    application.category = selectedCategory;
  }
  application.icon = String(message.icon ?? application.icon ?? "").trim();

  if (requestContext?.scopeKind === "app" && !getScopeItems("app").some((item) => item.name === application.name)) {
    state.rules.push(createRule("app", application.name));
  }

  setSelectedName("app", application.name);
  if (application.category) {
    setSelectedName("category", application.category);
  }

  setMessage("已添加程序。", "success");
  scheduleSaveRules();
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
  const scope = parseScope(rule.scope);
  return {
    id: createRuleId(state.nextId++),
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

function toViewApplication(application) {
  return {
    name: String(application.name ?? "").trim(),
    displayName: String(application.displayName ?? application.name ?? "").trim(),
    path: String(application.path ?? "").trim(),
    category: String(application.category ?? "").trim(),
    icon: String(application.icon ?? "").trim()
  };
}

function createRule(scopeKind, scopeName, values = {}) {
  if (scopeKind === "app") {
    ensureApplication(scopeName);
  }

  return {
    id: createRuleId(state.nextId++),
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

function ensureApplication(name) {
  const trimmed = String(name ?? "").trim();
  let application = state.applications.find((item) => item.name === trimmed);
  if (!application) {
    application = { name: trimmed, displayName: trimmed, path: "", category: "", icon: "" };
    state.applications.push(application);
  }

  return application;
}

function toPayloadRule(rule) {
  return {
    scope: buildScope(rule.scopeKind, rule.scopeName),
    mouseButton: normalizeMouseButton(rule.mouseButton),
    pattern: parsePattern(rule.patternText),
    actionName: String(rule.actionName ?? "").trim() || getGestureMnemonic(rule),
    action: toPayloadAction(rule)
  };
}

function toPayloadAction(rule) {
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

function toPayloadApplication(application) {
  return {
    name: application.name.trim(),
    displayName: String(application.displayName || application.name || "").trim(),
    path: application.path.trim(),
    category: application.category.trim()
  };
}

function toPayloadEdgeAction(action) {
  normalizeEdgeActionInPlace(action);
  return {
    enabled: Boolean(action.enabled),
    triggerType: action.triggerType,
    location: action.location,
    wheelDirection: action.wheelDirection,
    frictionCount: normalizeFrictionCount(action.frictionCount),
    action: toPayloadAction(action)
  };
}

function toPayloadUiSettings(settings) {
  return normalizeUiSettings(settings);
}

function getConfigPayload() {
  return {
    rules: state.rules.map(toPayloadRule),
    applications: state.applications.map(toPayloadApplication),
    edgeActions: state.edgeActions.map(toPayloadEdgeAction),
    uiSettings: toPayloadUiSettings(state.uiSettings)
  };
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
  const normalized = normalizeWebDavSettings(settings);
  return JSON.stringify(normalized);
}

function collectCategoryItems() {
  const counts = new Map();

  for (const rule of categoryRulesSnapshot()) {
    if (rule.scopeName) {
      counts.set(rule.scopeName, (counts.get(rule.scopeName) ?? 0) + 1);
    }
  }

  for (const application of state.applications) {
    if (application.category) {
      counts.set(application.category, counts.get(application.category) ?? 0);
    }
  }

  return [...counts.entries()]
    .map(([name, count]) => {
      const application = state.applications.find((item) => item.name === name);
      return {
        name,
        count,
        displayName: application?.displayName || name,
        icon: application?.icon || ""
      };
    })
    .sort((left, right) => left.name.localeCompare(right.name, "zh-Hans-CN"));
}

function collectAppItems() {
  const counts = new Map();

  for (const rule of appRulesSnapshot()) {
    if (rule.scopeName) {
      counts.set(rule.scopeName, (counts.get(rule.scopeName) ?? 0) + 1);
    }
  }

  for (const application of state.applications) {
    if (application.name) {
      counts.set(application.name, counts.get(application.name) ?? 0);
    }
  }

  return [...counts.entries()]
    .map(([name, count]) => {
      const application = state.applications.find((item) => item.name === name);
      return {
        name,
        count,
        displayName: application?.displayName || name,
        icon: application?.icon || ""
      };
    })
    .sort((left, right) => left.name.localeCompare(right.name, "zh-Hans-CN"));
}

function parseScope(scope) {
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

function buildScope(kind, name) {
  if (kind === "category" || kind === "app") {
    const trimmed = String(name ?? "").trim();
    return trimmed ? `${kind}:${trimmed}` : "";
  }

  return "global";
}

function parsePattern(text) {
  return String(text ?? "")
    .split(/[\s,，]+/)
    .map((part) => part.trim())
    .filter(Boolean);
}

function parseKeys(text) {
  return String(text ?? "")
    .split("+")
    .map((part) => part.trim())
    .filter(Boolean);
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

function createEmptyGestureDraft() {
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

function createDefaultUiSettings() {
  return cloneUiSettings(DEFAULT_UI_SETTINGS);
}

function cloneUiSettings(settings) {
  const source = normalizeObjectKeys(settings);
  return {
    mouseTrail: normalizeMouseTrailSettings(source.mouseTrail),
    gestureHint: normalizeGestureHintSettings(source.gestureHint),
    appBehavior: normalizeAppBehaviorSettings(source.appBehavior),
    webDav: normalizeWebDavSettings(source.webDav)
  };
}

function normalizeUiSettings(settings) {
  return cloneUiSettings(settings);
}

function normalizeMouseTrailSettings(settings) {
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

function normalizeGestureHintSettings(settings) {
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

function normalizeAppBehaviorSettings(settings) {
  settings = normalizeObjectKeys(settings);
  return {
    launchAtStartup: Boolean(settings?.launchAtStartup ?? DEFAULT_UI_SETTINGS.appBehavior.launchAtStartup),
    runAsAdministrator: Boolean(settings?.runAsAdministrator ?? DEFAULT_UI_SETTINGS.appBehavior.runAsAdministrator),
    closeButtonBehavior: normalizeCloseButtonBehavior(settings?.closeButtonBehavior),
    gesturePaused: Boolean(settings?.gesturePaused ?? DEFAULT_UI_SETTINGS.appBehavior.gesturePaused),
    excludedApplications: normalizeExcludedApplications(settings?.excludedApplications)
  };
}

function normalizeExcludedApplications(applications) {
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

function normalizeExcludedApplication(application) {
  application = normalizeObjectKeys(application);
  const name = String(application?.name ?? "").trim();
  const path = String(application?.path ?? "").trim();
  return {
    name,
    displayName: String(application?.displayName ?? name).trim() || name || path,
    path,
    disableEdgeActions: Boolean(application?.disableEdgeActions ?? false)
  };
}

function isSameApplicationIdentity(left, right) {
  const leftPath = String(left?.path ?? "").trim().toLowerCase();
  const rightPath = String(right?.path ?? "").trim().toLowerCase();
  if (leftPath && rightPath) {
    return leftPath === rightPath;
  }

  return String(left?.name ?? "").trim().toLowerCase() === String(right?.name ?? "").trim().toLowerCase();
}

function normalizeWebDavSettings(settings) {
  settings = normalizeObjectKeys(settings);
  return {
    address: String(settings?.address ?? DEFAULT_UI_SETTINGS.webDav.address).trim(),
    userName: String(settings?.userName ?? DEFAULT_UI_SETTINGS.webDav.userName).trim(),
    password: String(settings?.password ?? DEFAULT_UI_SETTINGS.webDav.password),
    remotePath: String(settings?.remotePath ?? DEFAULT_UI_SETTINGS.webDav.remotePath).trim()
  };
}

function normalizeCloseButtonBehavior(value) {
  const normalized = String(value ?? "").trim();
  return ["minimize-to-tray", "minimize-to-taskbar", "exit"].includes(normalized)
    ? normalized
    : DEFAULT_UI_SETTINGS.appBehavior.closeButtonBehavior;
}

function normalizeObjectKeys(source) {
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

function clampInteger(value, min, max, fallback) {
  const parsed = Number.parseInt(value, 10);
  if (!Number.isFinite(parsed)) {
    return fallback;
  }

  return Math.min(max, Math.max(min, parsed));
}

function clampFloat(value, min, max, fallback) {
  const parsed = Number.parseFloat(value);
  if (!Number.isFinite(parsed)) {
    return fallback;
  }

  return Math.min(max, Math.max(min, parsed));
}

function toPatternText(pattern) {
  if (!Array.isArray(pattern) || pattern.length === 0) {
    return "";
  }

  return pattern.join(", ");
}

function getGestureMnemonic(source) {
  const pattern = Array.isArray(source?.pattern)
    ? source.pattern
    : parsePattern(source?.patternText);
  if (pattern.length === 0) {
    return "";
  }

  const button = MOUSE_BUTTON_SYMBOLS[normalizeMouseButton(source?.mouseButton)] ?? MOUSE_BUTTON_SYMBOLS.right;
  const directions = pattern
    .map((direction) => DIRECTION_SYMBOLS[direction] ?? "")
    .join("");
  return directions ? `${button}${directions}` : "";
}

function normalizeMouseButton(button) {
  return String(button ?? "").toLowerCase() === "middle" ? "middle" : "right";
}

function normalizeActionType(actionType) {
  const normalized = String(actionType ?? "").toLowerCase();
  return ["hotkey", "window", "volume", "brightness"].includes(normalized) ? normalized : "hotkey";
}

function normalizeWindowOperation(operation) {
  const normalized = String(operation ?? "").trim();
  return WINDOW_OPERATIONS.some((item) => item.value === normalized)
    ? normalized
    : "toggle-maximize";
}

function normalizeVolumeOperation(operation) {
  const normalized = String(operation ?? "").trim();
  return VOLUME_OPERATIONS.some((item) => item.value === normalized)
    ? normalized
    : "increase";
}

function normalizeBrightnessOperation(operation) {
  const normalized = String(operation ?? "").trim();
  return BRIGHTNESS_OPERATIONS.some((item) => item.value === normalized)
    ? normalized
    : "increase";
}

function normalizeAmount(value) {
  return clampInteger(value, 1, 100, 5);
}

function normalizeFrictionCount(value) {
  return clampInteger(value, 1, 20, 4);
}

function normalizeEdgeActions(edgeActions) {
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

function normalizeEdgeAction(action) {
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

function normalizeEdgeActionInPlace(action) {
  Object.assign(action, normalizeEdgeAction(action));
}

function normalizeEdgeTriggerType(triggerType) {
  const normalized = String(triggerType ?? "").toLowerCase();
  return ["corner", "friction", "wheel"].includes(normalized) ? normalized : "corner";
}

function normalizeEdgeLocation(location, triggerType) {
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

function migrateLegacyFrictionLocation(location) {
  return {
    "top-left": "left",
    "top-right": "top",
    "bottom-left": "bottom",
    "bottom-right": "right"
  }[location] ?? "";
}

function normalizeWheelDirection(direction) {
  return String(direction ?? "").toLowerCase() === "down" ? "down" : "up";
}

function createDefaultEdgeAction(triggerType, location, wheelDirection = "") {
  return {
    enabled: false,
    triggerType,
    location,
    wheelDirection,
    frictionCount: 4,
    keysText: "",
    actionType: "hotkey",
    windowOperation: "toggle-maximize",
    volumeOperation: "increase",
    brightnessOperation: "increase",
    amount: 5
  };
}

function getEdgeActionKey(action) {
  return `${action.triggerType}:${action.location}:${action.wheelDirection || ""}`;
}

function getActionLabel(rule) {
  const actionType = normalizeActionType(rule?.actionType);
  if (actionType === "window") {
    const operation = WINDOW_OPERATIONS.find((item) => item.value === normalizeWindowOperation(rule?.windowOperation));
    return operation ? `窗口控制：${operation.label}` : "窗口控制";
  }

  if (actionType === "volume") {
    const operation = VOLUME_OPERATIONS.find((item) => item.value === normalizeVolumeOperation(rule?.volumeOperation));
    return operation?.value === "mute" ? "音量控制：静音" : `音量控制：${operation?.label ?? "音量 +" } ${normalizeAmount(rule?.amount)}`;
  }

  if (actionType === "brightness") {
    const operation = BRIGHTNESS_OPERATIONS.find((item) => item.value === normalizeBrightnessOperation(rule?.brightnessOperation));
    return `亮度控制：${operation?.label ?? "亮度 +" } ${normalizeAmount(rule?.amount)}`;
  }

  return rule?.keysText || "点击设置";
}

function getEdgeActionLabel(action) {
  const triggerLabel = {
    corner: "触发角",
    friction: "摩擦边",
    wheel: "边缘滚动"
  }[normalizeEdgeTriggerType(action?.triggerType)];
  const allLocations = [...EDGE_LOCATIONS.corner, ...EDGE_LOCATIONS.edge];
  const location = allLocations.find((item) => item.value === action?.location)?.label ?? "";
  const wheel = action?.wheelDirection ? (action.wheelDirection === "down" ? "滚轮下" : "滚轮上") : "";
  return [triggerLabel, location, wheel].filter(Boolean).join(" ");
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
