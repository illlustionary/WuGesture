import { computed, onMounted, ref, watch } from "vue";

export function useGestureEditor() {
  const activeTab = ref("global");
  const statusText = ref("启动中");
  const statusState = ref("idle");
  const configPath = ref("读取中");
  const configMessage = ref("");
  const configMessageState = ref("idle");
  const rules = ref([]);
  const scopeDraft = ref("");
  const selectedCategory = ref("");
  const selectedApp = ref("");
  const recordingInput = ref(null);
  const nextId = ref(1);

  const globalRules = computed(() => rules.value.filter((rule) => rule.scopeKind === "global"));
  const categoryRules = computed(() => rules.value.filter((rule) => rule.scopeKind === "category"));
  const appRules = computed(() => rules.value.filter((rule) => rule.scopeKind === "app"));
  const scopeItems = computed(() => {
    const source = activeTab.value === "category" ? categoryRules.value : appRules.value;
    const counts = new Map();

    for (const rule of source) {
      counts.set(rule.scopeName, (counts.get(rule.scopeName) ?? 0) + 1);
    }

    return [...counts.entries()]
      .map(([name, count]) => ({ name, count }))
      .sort((left, right) => left.name.localeCompare(right.name, "zh-Hans-CN"));
  });
  const selectedScopeName = computed(() => {
    if (activeTab.value === "category") {
      return selectedCategory.value;
    }

    if (activeTab.value === "app") {
      return selectedApp.value;
    }

    return "";
  });
  const visibleRules = computed(() => {
    if (activeTab.value === "global") {
      return globalRules.value;
    }

    const name = selectedScopeName.value;
    if (!name) {
      return [];
    }

    return rules.value.filter((rule) => rule.scopeKind === activeTab.value && rule.scopeName === name);
  });
  const workspaceClass = computed(() => (activeTab.value === "global" ? "workspace--single" : "workspace--split"));
  const routeLabel = computed(() => {
    if (activeTab.value === "global") {
      return "全局规则";
    }

    return `${activeTab.value === "category" ? "分类" : "App"}规则`;
  });

  function installWebViewBridge() {
    if (window.chrome?.webview) {
      window.chrome.webview.addEventListener("message", (event) => {
        handleMessage(event.data);
      });

      window.chrome.webview.postMessage("get-status");
      return;
    }

    statusText.value = "浏览器预览";
    statusState.value = "idle";
  }

  function installKeyboardRecorder() {
    document.addEventListener("keydown", (event) => {
      if (!recordingInput.value || event.target !== recordingInput.value) {
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
        recordingInput.value.value = getModifierKeys(event).join(" + ");
        return;
      }

      if (!shouldRecordKey(event)) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();

      const keys = [...getModifierKeys(event), normalizeKey(event.key)];
      recordingInput.value.value = [...new Set(keys)].join(" + ");
      stopRecording();
    }, true);

    document.addEventListener("keyup", (event) => {
      if (!recordingInput.value || event.target !== recordingInput.value || !isModifierOnly(event.key)) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();
    }, true);
  }

  function handleMessage(message) {
    if (message.type === "status") {
      statusText.value = message.status === "running" ? "运行中" : message.status;
      statusState.value = message.status === "running" ? "running" : "idle";
      return;
    }

    if (message.type === "rules") {
      configPath.value = message.configPath;
      rules.value = (message.rules ?? []).map((rule) => toViewRule(rule));
      ensureSelection();
      return;
    }

    if (message.type === "config-result") {
      configMessage.value = message.message;
      configMessageState.value = message.success ? "success" : "error";
    }
  }

  function toViewRule(rule) {
    const scope = parseScope(rule.scope);
    return {
      id: createRuleId(nextId.value++),
      scopeKind: scope.kind,
      scopeName: scope.name,
      patternText: toPatternText(rule.pattern),
      actionName: rule.actionName ?? "",
      keysText: Array.isArray(rule.keys) ? rule.keys.join(" + ") : "",
      actionType: rule.actionType ?? "hotkey"
    };
  }

  function ensureSelection() {
    if (activeTab.value === "category") {
      if (!categoryRules.value.some((rule) => rule.scopeName === selectedCategory.value)) {
        selectedCategory.value = scopeItems.value[0]?.name ?? "";
      }
      return;
    }

    if (activeTab.value === "app") {
      if (!appRules.value.some((rule) => rule.scopeName === selectedApp.value)) {
        selectedApp.value = scopeItems.value[0]?.name ?? "";
      }
    }
  }

  function selectTab(tab) {
    activeTab.value = tab;
    ensureSelection();
  }

  function createScopeTarget(name) {
    const trimmed = String(name ?? "").trim();
    if (!trimmed) {
      setMessage("请输入名称后再新增。", "error");
      return;
    }

    if (activeTab.value === "category") {
      selectedCategory.value = trimmed;
    } else if (activeTab.value === "app") {
      selectedApp.value = trimmed;
    }

    rules.value.push(createRule(activeTab.value, trimmed));
    scopeDraft.value = "";
    setMessage("已新增。", "success");
  }

  function renameSelectedScope(nextName) {
    const name = String(nextName ?? "").trim();
    if (!name) {
      return;
    }

    const currentName = selectedScopeName.value;
    if (!currentName || currentName === name) {
      return;
    }

    for (const rule of rules.value) {
      if (rule.scopeKind === activeTab.value && rule.scopeName === currentName) {
        rule.scopeName = name;
      }
    }

    if (activeTab.value === "category") {
      selectedCategory.value = name;
    } else if (activeTab.value === "app") {
      selectedApp.value = name;
    }
  }

  function deleteSelectedScope() {
    const name = selectedScopeName.value;
    if (!name) {
      return;
    }

    rules.value = rules.value.filter((rule) => !(rule.scopeKind === activeTab.value && rule.scopeName === name));
    ensureSelection();
    setMessage("已删除当前项。", "success");
  }

  function createRule(scopeKind, scopeName) {
    return {
      id: createRuleId(nextId.value++),
      scopeKind,
      scopeName,
      patternText: "Left",
      actionName: "Back",
      keysText: "Alt + Left",
      actionType: "hotkey"
    };
  }

  function removeRule(id) {
    rules.value = rules.value.filter((rule) => rule.id !== id);
    ensureSelection();
  }

  function saveRules() {
    const payloadRules = [];
    for (const rule of rules.value) {
      const parsed = toPayloadRule(rule);
      if (!parsed) {
        setMessage("存在无效规则，请检查方向和快捷键。", "error");
        return;
      }

      payloadRules.push(parsed);
    }

    postWebMessage({
      type: "save-rules",
      rules: payloadRules
    });
  }

  function toPayloadRule(rule) {
    const pattern = parsePattern(rule.patternText);
    const keys = parseKeys(rule.keysText);
    if (pattern.length === 0 || keys.length === 0) {
      return null;
    }

    return {
      scope: buildScope(rule.scopeKind, rule.scopeName),
      pattern,
      actionName: rule.actionName.trim(),
      action: {
        type: rule.actionType || "hotkey",
        keys
      }
    };
  }

  function reloadRules() {
    postWebMessage({ type: "reload-rules" });
  }

  function resetRules() {
    postWebMessage({ type: "reset-rules" });
  }

  function postWebMessage(message) {
    if (window.chrome?.webview) {
      window.chrome.webview.postMessage(message);
    }
  }

  function setMessage(message, state = "idle") {
    configMessage.value = message;
    configMessageState.value = state;
  }

  function startRecording(input) {
    stopRecording();
    recordingInput.value = input;
    recordingInput.value.dataset.previousValue = recordingInput.value.value;
    recordingInput.value.placeholder = "按组合键或手动输入";
    recordingInput.value.classList.add("is-recording");
    setMessage("快捷键输入框已监听组合键，也可手动输入。");
  }

  function stopRecording() {
    if (!recordingInput.value) {
      return;
    }

    recordingInput.value.placeholder = "";
    recordingInput.value.classList.remove("is-recording");
    delete recordingInput.value.dataset.previousValue;
    recordingInput.value = null;
  }

  onMounted(() => {
    installWebViewBridge();
    installKeyboardRecorder();
  });

  watch(activeTab, () => ensureSelection());

  return {
    activeTab,
    statusText,
    statusState,
    configPath,
    configMessage,
    configMessageState,
    scopeDraft,
    selectedScopeName,
    visibleRules,
    workspaceClass,
    routeLabel,
    scopeItems,
    selectTab,
    createScopeTarget,
    renameSelectedScope,
    deleteSelectedScope,
    removeRule,
    saveRules,
    reloadRules,
    resetRules,
    startRecording,
    stopRecording
  };
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
    return `${kind}:${name.trim()}`;
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
