import { computed, proxyRefs, reactive, ref } from "vue";
import {
  BRIGHTNESS_OPERATIONS,
  EDGE_LOCATIONS,
  SCOPE_KINDS,
  VOLUME_OPERATIONS,
  WEBVIEW_MESSAGE_TYPES,
  WINDOW_OPERATIONS
} from "@/constants/gestureEditorOptions";
import {
  DEFAULT_APPLICATIONS,
  DEFAULT_RULES,
  DEFAULT_UI_SETTINGS
} from "@/constants/gestureEditorDefaults";
import {
  createDefaultUiSettings,
  isSameApplicationIdentity,
  normalizeExcludedApplication,
  normalizeExcludedApplications,
  normalizeEdgeActions,
  normalizeUiSettings
} from "@/utils/gestureEditorNormalizers";
import {
  getActionLabel,
  getEdgeActionLabel,
  getGestureMnemonic
} from "@/utils/gestureEditorFormatters";
import {
  collectAppItems as collectAppItemsFromState,
  collectCategoryItems as collectCategoryItemsFromState
} from "@/utils/gestureEditorCollections";
import {
  createEmptyGestureDraft,
  createRuleModel,
  toViewApplication,
  toViewRule as toViewRuleModel
} from "@/utils/gestureEditorViewModels";
import { useGestureEditorApplicationPicker } from "@/gestureEditor/modules/useGestureEditorApplicationPicker";
import { useCategoryApplications } from "@/gestureEditor/modules/useCategoryApplications";
import { useGestureApplications } from "@/gestureEditor/modules/useGestureApplications";
import { useGestureConfigPersistence } from "@/gestureEditor/modules/useGestureConfigPersistence";
import { useGestureEditorNotifications } from "@/gestureEditor/modules/useGestureEditorNotifications";
import { useGestureEditorWebViewBridge } from "@/gestureEditor/modules/useGestureEditorWebViewBridge";
import { useGestureRuleEditor } from "@/gestureEditor/modules/useGestureRuleEditor";
import { useGestureScopes } from "@/gestureEditor/modules/useGestureScopes";

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
  webDavTestState: "idle",
  webDavTestedSignature: ""
});

const activeScope = ref(SCOPE_KINDS.global);
const initialized = ref(false);
let autoSaveTimer = 0;

const notifications = useGestureEditorNotifications({
  state
});

const webView = useGestureEditorWebViewBridge({ notifications });

const persistenceActions = useGestureConfigPersistence({
  getAutoSaveTimer: () => autoSaveTimer,
  initialized,
  setAutoSaveTimer: (timer) => {
    autoSaveTimer = timer;
  },
  setConfigResultMessage,
  setGesturePaused,
  setMessage,
  state,
  webView
});

const {
  exportConfigToLocal,
  getUiSettingsSnapshot,
  getWebDavSignature,
  handleConfigResult,
  handleWebDavResult,
  importConfigFromLocal,
  isWebDavTested,
  reloadRules,
  resetRules,
  resetUiSettings,
  previewLevelOsd,
  restoreConfigFromWebDav,
  saveConfigToWebDav,
  saveRules,
  saveUiSettings,
  scheduleSaveRules,
  shouldPreserveLocalEdgeActions,
  testWebDavConnection,
  updateEdgeAction
} = persistenceActions;

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
  getCategoriesForApplication,
  moveApplicationCategory,
  setApplicationCategories,
  updateApplicationCategory,
  updateApplicationDisplayName
} = applicationActions;

const {
  assignSelectedAppToCategory,
  removeAppFromCategory
} = categoryApplicationActions;

const collectCategoryItems = () => getScopeItems(SCOPE_KINDS.category);
const collectAppItems = () => getScopeItems(SCOPE_KINDS.app);

const ruleEditorActions = useGestureRuleEditor({
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
  state,
  webView
});

const {
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
} = ruleEditorActions;

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

export function useGestureEditorContext() {
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
    toggleUserPaused,
    setActiveScope,
    selectScope,
    getRulesForScope,
    getScopeItems,
    getSelectedName,
    getApplicationsForCategory,
    getCategoriesForApplication,
    getApplication,
    moveApplicationCategory,
    setApplicationCategories,
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
    previewLevelOsd,
    testWebDavConnection,
    saveConfigToWebDav,
    restoreConfigFromWebDav,
    exportConfigToLocal,
    importConfigFromLocal,
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
    if (message.status === "running") {
      state.statusText = "运行中";
      state.statusState = "running";
    } else if (message.status === "paused") {
      state.statusText = "已暂停";
      state.statusState = "paused";
    } else {
      state.statusText = message.status;
      state.statusState = "idle";
    }
    return;
  }

  if (message.type === WEBVIEW_MESSAGE_TYPES.rules) {
    state.configPath = message.configPath;
    replaceConfig(
      message.rules ?? [],
      message.applications ?? [],
      message.uiSettings ?? DEFAULT_UI_SETTINGS,
      message.edgeActions ?? [],
      { preserveEdgeActions: shouldPreserveLocalEdgeActions() }
    );
    return;
  }

  if (message.type === WEBVIEW_MESSAGE_TYPES.configResult) {
    handleConfigResult(message);
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
  const currentRuleIds = new Map(
    state.rules.map((rule) => [getRuleIdentity(rule), rule.id])
  );

  state.rules = rules.map((sourceRule) => {
    const rule = toViewRule(sourceRule);
    return {
      ...rule,
      id: currentRuleIds.get(getRuleIdentity(rule)) ?? rule.id
    };
  });
  state.applications = applications.map((application) => toViewApplication(application));
  if (!options.preserveEdgeActions) {
    state.edgeActions = normalizeEdgeActions(edgeActions);
  }
  state.uiSettings = normalizeUiSettings(uiSettings);
  setGesturePaused(state.uiSettings.appBehavior.gesturePaused);
  ensureSelection(SCOPE_KINDS.category);
  ensureSelection(SCOPE_KINDS.app);
}

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

  saveRules({ notifyPreview: false });
  setMessage("已添加排除项。", "success");
}

function updateExcludedApplication(application, patch = {}) {
  if (!application) {
    return;
  }

  Object.assign(application, patch);
  const normalized = normalizeExcludedApplications(state.uiSettings.appBehavior.excludedApplications);
  state.uiSettings.appBehavior.excludedApplications = normalized;
  saveRules({ notifyPreview: false });
}

function removeExcludedApplication(index) {
  if (index < 0 || index >= state.uiSettings.appBehavior.excludedApplications.length) {
    return;
  }

  state.uiSettings.appBehavior.excludedApplications.splice(index, 1);
  saveRules({ notifyPreview: false });
  setMessage("已删除排除项。", "success");
}

function setMessage(message, stateName = "idle", options = {}) {
  notifications.show(message, stateName, options);
}

function setConfigResultMessage(message, success, notify) {
  notifications.showConfigResult(message, success, notify);
}

function postWebMessageSilently(message) {
  webView.postSilent(message);
}

function toggleUserPaused() {
  webView.postSilent({
    type: WEBVIEW_MESSAGE_TYPES.setUserPaused,
    paused: state.statusState !== "paused"
  });
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

function getRuleIdentity(rule) {
  return [
    String(rule?.scopeKind ?? ""),
    String(rule?.scopeName ?? ""),
    String(rule?.mouseButton ?? ""),
    String(rule?.patternText ?? "")
  ].join("\u0000");
}

function createRule(scopeKind, scopeName, values = {}) {
  if (scopeKind === SCOPE_KINDS.app) {
    ensureApplication(scopeName);
  }

  return createRuleModel(scopeKind, scopeName, values, createRuleId(state.nextId++));
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
