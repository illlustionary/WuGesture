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
  recordingInput: null
});

const activeScope = ref("global");
const initialized = ref(false);

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
    stopRecording
  });
}

function initialize() {
  if (initialized.value) {
    return;
  }

  initialized.value = true;
  installKeyboardRecorder();

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
  if (kind === "global") {
    state.rules.push(createRule("global", ""));
    return;
  }

  const scopeName = String(name || getFirstScopeName(kind)).trim();
  if (!scopeName) {
    setMessage(kind === "category" ? "先新增或选择一个分类。" : "先新增或选择一个 App。", "error");
    return;
  }

  state.rules.push(createRule(kind, scopeName));
  setSelectedName(kind, scopeName);
}

function removeRule(id) {
  state.rules = state.rules.filter((rule) => rule.id !== id);
  ensureSelection("category");
  ensureSelection("app");
}

function createScopeTarget(kind, name) {
  const trimmed = String(name ?? "").trim();
  if (!trimmed) {
    setMessage("请输入名称后再新增。", "error");
    return;
  }

  if (!getScopeItems(kind).some((item) => item.name === trimmed)) {
    state.rules.push(createRule(kind, trimmed));
  }

  setSelectedName(kind, trimmed);
  setMessage("已新增。", "success");
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
    setMessage("请先选择一个 App。", "error");
    return;
  }

  const application = ensureApplication(name);
  application.displayName = String(displayName ?? "").trim();
}

function updateApplicationCategory(appName = getSelectedName("app"), categoryName = "") {
  const name = String(appName ?? "").trim();
  if (!name) {
    setMessage("请先选择一个 App。", "error");
    return;
  }

  const application = ensureApplication(name);
  application.category = String(categoryName ?? "").trim();
  setMessage(application.category ? "已设置 App 分类。" : "已清除 App 分类。", "success");
}

function assignSelectedAppToCategory(categoryName = getSelectedName("category"), appName = getSelectedName("app")) {
  const category = String(categoryName ?? "").trim();
  const name = String(appName ?? "").trim();
  if (!category || !name) {
    setMessage("请先选择分类和 App。", "error");
    return;
  }

  const application = ensureApplication(name);
  application.category = category;
  setSelectedName("category", category);
  setMessage("已关联 App 到分类。", "success");
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
}

function saveRules() {
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

function reloadRules() {
  postWebMessage({ type: "reload-rules" });
}

function resetRules() {
  postWebMessage({ type: "reset-rules" });
}

function startRecording(input) {
  stopRecording();
  state.recordingInput = input;
  state.recordingInput.placeholder = "按组合键或手动输入";
  state.recordingInput.classList.add("is-recording");
  setMessage("快捷键输入框已监听组合键，也可手动输入。");
}

function stopRecording() {
  if (!state.recordingInput) {
    return;
  }

  state.recordingInput.placeholder = "";
  state.recordingInput.classList.remove("is-recording");
  state.recordingInput = null;
}

function installKeyboardRecorder() {
  document.addEventListener("keydown", (event) => {
    const input = state.recordingInput;
    if (!input || event.target !== input) {
      return;
    }

    if (event.key === "Escape") {
      event.preventDefault();
      event.stopPropagation();
      stopRecording();
      return;
    }

    if (isModifierOnly(event.key)) {
      event.preventDefault();
      event.stopPropagation();
      input.value = getModifierKeys(event).join(" + ");
      input.dispatchEvent(new Event("input", { bubbles: true }));
      return;
    }

    if (!shouldRecordKey(event)) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();

    const keys = [...getModifierKeys(event), normalizeKey(event.key)];
    input.value = [...new Set(keys)].join(" + ");
    input.dispatchEvent(new Event("input", { bubbles: true }));
    stopRecording();
  }, true);

  document.addEventListener("keyup", (event) => {
    const input = state.recordingInput;
    if (!input || event.target !== input || !isModifierOnly(event.key)) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
  }, true);
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

function toViewRule(rule) {
  const scope = parseScope(rule.scope);
  return {
    id: createRuleId(state.nextId++),
    scopeKind: scope.kind,
    scopeName: scope.name,
    patternText: toPatternText(rule.pattern),
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

function createRule(scopeKind, scopeName) {
  if (scopeKind === "app") {
    ensureApplication(scopeName);
  }

  return {
    id: createRuleId(state.nextId++),
    scopeKind,
    scopeName,
    patternText: "Left",
    actionName: "Back",
    keysText: "Alt + Left",
    actionType: "hotkey"
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
  if (pattern.length === 0 || keys.length === 0 || !scope) {
    return null;
  }

  return {
    scope,
    pattern,
    actionName: rule.actionName.trim(),
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
    .map(([name, count]) => ({ name, count }))
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

function toPatternText(pattern) {
  if (!Array.isArray(pattern) || pattern.length === 0) {
    return "";
  }

  return pattern.join(", ");
}

function getModifierKeys(event) {
  const keys = [];
  if (event.ctrlKey) {
    keys.push("Control");
  }

  if (event.altKey) {
    keys.push("Alt");
  }

  if (event.shiftKey) {
    keys.push("Shift");
  }

  if (event.metaKey) {
    keys.push("Win");
  }

  return keys;
}

function isModifierOnly(key) {
  return ["Control", "Alt", "Shift", "Meta"].includes(key);
}

function shouldRecordKey(event) {
  return event.ctrlKey || event.altKey || event.shiftKey || event.metaKey || isSpecialKey(event.key);
}

function isSpecialKey(key) {
  return key.length > 1 && !isModifierOnly(key);
}

function normalizeKey(key) {
  if (key === " ") {
    return "Space";
  }

  const aliases = {
    ArrowLeft: "Left",
    ArrowRight: "Right",
    ArrowUp: "Up",
    ArrowDown: "Down",
    Escape: "Esc",
    Delete: "Delete",
    Insert: "Insert",
    Home: "Home",
    End: "End",
    PageUp: "PageUp",
    PageDown: "PageDown",
    Backspace: "Back",
    Enter: "Enter",
    Tab: "Tab"
  };

  if (aliases[key]) {
    return aliases[key];
  }

  if (key.length === 1) {
    return key.toUpperCase();
  }

  return key;
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
