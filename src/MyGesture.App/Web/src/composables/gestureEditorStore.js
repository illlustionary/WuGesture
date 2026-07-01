import { computed, proxyRefs, reactive, ref } from "vue";

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

const state = reactive({
  statusText: "启动中",
  statusState: "idle",
  configPath: "读取中",
  configMessage: "",
  configMessageState: "idle",
  rules: [],
  applications: [],
  nextId: 1,
  selectedCategory: "",
  selectedApp: "",
  applicationPickerOpen: false,
  applicationPickerCategory: "",
  recordingHotkeyTarget: null,
  recordingHotkeyRequestId: "",
  gestureEditorOpen: false,
  gestureEditorMode: "add",
  gestureEditorRuleId: "",
  gestureEditorScopeKind: "global",
  gestureEditorScopeName: "",
  gestureDraft: createEmptyGestureDraft(),
  gestureRecognitionMessage: ""
});

const activeScope = ref("global");
const initialized = ref(false);
const pendingGestureRequests = new Map();
let autoSaveTimer = 0;

export function useGestureEditorStore() {
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
    saveGestureEditor,
    recordGesturePoints,
    createScopeTarget,
    renameSelectedScope,
    deleteSelectedScope,
    openApplicationPicker,
    closeApplicationPicker,
    selectApplication,
    pickApplicationWindow,
    saveRules,
    reloadRules,
    resetRules,
    startRecording,
    stopRecording,
    isRecordingHotkey,
    getGestureMnemonic
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
    replaceConfig(DEFAULT_RULES, DEFAULT_APPLICATIONS);
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
    replaceConfig(message.rules ?? [], message.applications ?? []);
    return;
  }

  if (message.type === "config-result") {
    setMessage(message.message, message.success ? "success" : "error");
    return;
  }

  if (message.type === "application-selected") {
    addSelectedApplication(message);
    return;
  }

  if (message.type === "gesture-pattern-recognized") {
    applyRecognizedPattern(message);
    return;
  }

  if (message.type === "hotkey-recorded") {
    applyRecordedHotkey(message);
  }
}

function replaceConfig(rules, applications) {
  state.rules = rules.map((rule) => toViewRule(rule));
  state.applications = applications.map((application) => toViewApplication(application));
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
  stopRecording();
  state.gestureEditorOpen = false;
  state.gestureRecognitionMessage = "";
  state.gestureDraft = createEmptyGestureDraft();
  setGesturePaused(false);
}

function saveGestureEditor() {
  const draft = state.gestureDraft;
  const pattern = parsePattern(draft.patternText);
  const keys = parseKeys(draft.keysText);
  const actionName = String(draft.actionName ?? "").trim() || getGestureMnemonic(draft);

  if (pattern.length === 0) {
    state.gestureRecognitionMessage = "请先在录制区域绘制手势。";
    return;
  }

  if (state.gestureEditorMode === "edit") {
    const rule = state.rules.find((item) => item.id === state.gestureEditorRuleId);
    if (!rule) {
      closeGestureEditor();
      return;
    }

    rule.actionName = actionName;
    rule.patternText = toPatternText(pattern);
    rule.mouseButton = normalizeMouseButton(draft.mouseButton);
    rule.keysText = keys.join(" + ");
    rule.actionType = draft.actionType || "hotkey";
    closeGestureEditor();
    scheduleSaveRules();
    return;
  }

  state.rules.push(createRule(
    state.gestureEditorScopeKind,
    state.gestureEditorScopeName,
    {
      actionName,
      patternText: toPatternText(pattern),
      mouseButton: normalizeMouseButton(draft.mouseButton),
      keysText: keys.join(" + "),
      actionType: draft.actionType || "hotkey"
    }));
  closeGestureEditor();
  scheduleSaveRules();
}

function recordGesturePoints(recording) {
  const normalizedPoints = normalizeGesturePoints(recording?.points ?? recording);
  if (normalizedPoints.length < 2) {
    state.gestureDraft.patternText = "";
    state.gestureRecognitionMessage = "移动距离太短。";
    return;
  }

  state.gestureDraft.mouseButton = normalizeMouseButton(recording?.button);
  const requestId = createRequestId();
  pendingGestureRequests.set(requestId, true);
  state.gestureRecognitionMessage = "正在识别...";

  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({
      type: "recognize-gesture",
      requestId,
      points: normalizedPoints
    });
    return;
  }

  applyRecognizedPattern({
    type: "gesture-pattern-recognized",
    requestId,
    pattern: recognizeGestureInBrowser(normalizedPoints)
  });
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

function renameSelectedScope(kind, nextName) {
  const name = String(nextName ?? "").trim();
  const currentName = getSelectedName(kind);
  if (!name || !currentName || currentName === name) {
    return;
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

function openApplicationPicker(categoryName = "") {
  state.applicationPickerCategory = String(categoryName ?? "").trim();
  state.applicationPickerOpen = true;
}

function closeApplicationPicker() {
  state.applicationPickerOpen = false;
  state.applicationPickerCategory = "";
}

function selectApplication(categoryName = "") {
  const category = String(categoryName || state.applicationPickerCategory || "").trim();
  closeApplicationPicker();
  postWebMessage({
    type: "select-application",
    requestId: createRequestId(),
    category
  });
}

function pickApplicationWindow(categoryName = "") {
  const category = String(categoryName || state.applicationPickerCategory || "").trim();
  closeApplicationPicker();
  postWebMessage({
    type: "pick-application-window",
    requestId: createRequestId(),
    category
  });
}

function addSelectedApplication(message) {
  const name = String(message.name ?? "").trim();
  if (!name) {
    return;
  }

  const application = ensureApplication(name);
  application.displayName = String(message.displayName ?? application.displayName ?? name).trim();
  application.path = String(message.path ?? "").trim();
  application.category = String(message.category ?? application.category ?? "").trim();
  application.icon = String(message.icon ?? application.icon ?? "").trim();
  setSelectedName("app", application.name);
  if (application.category) {
    setSelectedName("category", application.category);
  }

  setMessage("已添加程序。", "success");
  scheduleSaveRules();
}

function saveRules() {
  if (autoSaveTimer) {
    clearTimeout(autoSaveTimer);
    autoSaveTimer = 0;
  }

  const payloadRules = [];
  for (const rule of state.rules) {
    const parsed = toPayloadRule(rule);
    if (!parsed) {
      setMessage("存在无效规则，请检查方向和快捷键。", "error");
      return;
    }

    payloadRules.push(parsed);
  }

  if (payloadRules.length === 0) {
    setMessage("至少保留一条有效规则。", "error");
    return;
  }

  postWebMessage({
    type: "save-rules",
    rules: payloadRules,
    applications: state.applications.map(toPayloadApplication)
  });
}

function scheduleSaveRules() {
  if (!initialized.value) {
    return;
  }

  if (!window.chrome?.webview) {
    return;
  }

  if (autoSaveTimer) {
    clearTimeout(autoSaveTimer);
  }

  autoSaveTimer = window.setTimeout(() => {
    autoSaveTimer = 0;
    saveRules();
  }, 250);
}

function reloadRules() {
  postWebMessage({ type: "reload-rules" });
}

function resetRules() {
  postWebMessage({ type: "reset-rules" });
}

function startRecording(target) {
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

function setMessage(message, stateName = "idle") {
  state.configMessage = message;
  state.configMessageState = stateName;
}

function postWebMessage(message) {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage(message);
    return;
  }

  setMessage("浏览器预览中不会写入本机配置。", "idle");
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
    actionType: rule.actionType ?? "hotkey"
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
    actionType: values.actionType ?? "hotkey"
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
  const pattern = parsePattern(rule.patternText);
  const keys = parseKeys(rule.keysText);
  const scope = buildScope(rule.scopeKind, rule.scopeName);
  if (pattern.length === 0 || !scope) {
    return null;
  }

  return {
    scope,
    mouseButton: normalizeMouseButton(rule.mouseButton),
    pattern,
    actionName: rule.actionName.trim() || getGestureMnemonic(rule),
    action: {
      type: rule.actionType || "hotkey",
      keys
    }
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
  state.gestureDraft = {
    actionName: rule?.actionName ?? "",
    patternText: rule?.patternText ?? "",
    mouseButton: normalizeMouseButton(rule?.mouseButton),
    keysText: rule?.keysText ?? "",
    actionType: rule?.actionType ?? "hotkey"
  };
  state.gestureRecognitionMessage = "";
  state.gestureEditorOpen = true;
}

function applyRecognizedPattern(message) {
  if (!pendingGestureRequests.has(message.requestId)) {
    return;
  }

  pendingGestureRequests.delete(message.requestId);
  const pattern = Array.isArray(message.pattern) ? message.pattern.filter(Boolean) : [];
  state.gestureDraft.patternText = toPatternText(pattern);
  if (!String(state.gestureDraft.actionName ?? "").trim()) {
    state.gestureDraft.actionName = getGestureMnemonic(state.gestureDraft);
  }
  state.gestureRecognitionMessage = pattern.length > 0 ? "已识别手势。" : "未识别到有效手势。";
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
  state.gestureDraft.keysText = keysText;
  target.keysText = keysText;

  if (state.gestureEditorMode === "edit") {
    const rule = state.rules.find((item) => item.id === state.gestureEditorRuleId);
    if (rule) {
      rule.keysText = keysText;
    }
  }

  setMessage("已录制快捷键。", "success");
  scheduleSaveRules();
}

function normalizeGesturePoints(points) {
  if (!Array.isArray(points)) {
    return [];
  }

  return points
    .map((point) => ({
      x: Math.round(Number(point?.x ?? 0)),
      y: Math.round(Number(point?.y ?? 0))
    }))
    .filter((point) => Number.isFinite(point.x) && Number.isFinite(point.y));
}

function createEmptyGestureDraft() {
  return {
    actionName: "",
    patternText: "",
    mouseButton: "right",
    keysText: "",
    actionType: "hotkey"
  };
}

function recognizeGestureInBrowser(points) {
  const cleaned = [];
  for (const point of points) {
    const previous = cleaned[cleaned.length - 1];
    if (!previous || distance(previous, point) >= 8) {
      cleaned.push(point);
    }
  }

  if (cleaned.length < 2) {
    return [];
  }

  const result = [];
  for (let index = 1; index < cleaned.length; index += 1) {
    const previous = cleaned[index - 1];
    const current = cleaned[index];
    if (distance(previous, current) < 18) {
      continue;
    }

    const direction = toDirection(current.x - previous.x, current.y - previous.y);
    if (result[result.length - 1] !== direction) {
      result.push(direction);
    }
  }

  return result.slice(0, 12);
}

function toDirection(dx, dy) {
  let angle = Math.atan2(dy, dx) * 180 / Math.PI;
  if (angle < 0) {
    angle += 360;
  }

  const sector = Math.round(angle / 45) % 8;
  return ["Right", "DownRight", "Down", "DownLeft", "Left", "UpLeft", "Up", "UpRight"][sector];
}

function distance(a, b) {
  const dx = a.x - b.x;
  const dy = a.y - b.y;
  return Math.sqrt(dx * dx + dy * dy);
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
