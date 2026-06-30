<script setup>
import { computed, onMounted, ref, watch } from "vue";

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
const scopePanelTitle = computed(() => (activeTab.value === "category" ? "分类" : "App"));
const scopeDraftPlaceholder = computed(() => (activeTab.value === "category" ? "输入分类名" : "输入 App 名"));
const rulesPanelTitle = computed(() => (activeTab.value === "global" ? "全局规则" : `${scopePanelTitle.value}规则`));
const rulesPanelSubtitle = computed(() => {
  if (activeTab.value === "global") {
    return "当前只显示作用域为 global 的规则。";
  }

  if (selectedScopeName.value) {
    return `正在编辑 ${selectedScopeName.value} 的规则。`;
  }

  return "先在左侧选择一个项，再编辑对应规则。";
});
const emptyRulesText = computed(() => {
  if (activeTab.value === "global") {
    return "当前没有全局规则。";
  }

  if (selectedScopeName.value) {
    return `当前 ${selectedScopeName.value} 还没有规则。`;
  }

  return `左侧还没有任何 ${scopePanelTitle.value} 项。`;
});
const workspaceClass = computed(() => (activeTab.value === "global" ? "workspace--single" : "workspace--split"));

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
  document.addEventListener(
    "keydown",
    (event) => {
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
    },
    true
  );

  document.addEventListener(
    "keyup",
    (event) => {
      if (!recordingInput.value || event.target !== recordingInput.value || !isModifierOnly(event.key)) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();
    },
    true
  );
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

function selectScope(name) {
  if (activeTab.value === "category") {
    selectedCategory.value = name;
  } else if (activeTab.value === "app") {
    selectedApp.value = name;
  }
}

function createScopeTarget() {
  const name = scopeDraft.value.trim();
  if (!name) {
    setMessage("请输入名称后再新增。", "error");
    return;
  }

  if (activeTab.value === "category") {
    selectedCategory.value = name;
  } else if (activeTab.value === "app") {
    selectedApp.value = name;
  }

  rules.value.push(createRule(activeTab.value, name));
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

function addRule() {
  const scopeName = resolveCurrentScopeName();
  if (activeTab.value !== "global" && !scopeName) {
    setMessage("请先在左侧选择或新增一个项。", "error");
    return;
  }

  rules.value.push(createRule(activeTab.value, scopeName));
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

function resolveCurrentScopeName() {
  if (activeTab.value === "category") {
    return selectedCategory.value;
  }

  if (activeTab.value === "app") {
    return selectedApp.value;
  }

  return "";
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
  postWebMessage({
    type: "reload-rules"
  });
}

function resetRules() {
  postWebMessage({
    type: "reset-rules"
  });
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

function describeRuleScope(rule) {
  if (rule.scopeKind === "global") {
    return "全局";
  }

  return `${scopeKindLabel(rule.scopeKind)} / ${rule.scopeName}`;
}

function scopeKindLabel(kind) {
  return kind === "category" ? "分类" : "App";
}

function formatPattern(patternText) {
  const pattern = parsePattern(patternText);
  if (pattern.length === 0) {
    return "未设置方向";
  }

  return pattern.map(toDirectionGlyph).join("  ");
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

function toDirectionGlyph(direction) {
  const glyphs = {
    Up: "↑",
    UpRight: "↗",
    Right: "→",
    DownRight: "↘",
    Down: "↓",
    DownLeft: "↙",
    Left: "←",
    UpLeft: "↖"
  };

  return glyphs[direction] ?? direction;
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
</script>

<template>
  <div class="app-shell">
    <header class="hero">
      <div>
        <p class="eyebrow">Mouse gesture editor</p>
        <h1>My Gesture</h1>
        <p class="subtitle">全局、分类、App 三层规则编辑器</p>
      </div>
      <div class="status-badge" :data-state="statusState">{{ statusText }}</div>
    </header>

    <section class="panel panel--surface">
      <div class="panel-head">
        <div>
          <h2>规则编辑器</h2>
          <p>按作用域拆分编辑，右侧直接修改手势、动作和快捷键。</p>
        </div>
        <div class="config-path">{{ configPath }}</div>
      </div>

      <nav class="tabs" aria-label="规则作用域">
        <button type="button" class="tab" :class="{ active: activeTab === 'global' }" @click="selectTab('global')">全局</button>
        <button type="button" class="tab" :class="{ active: activeTab === 'category' }" @click="selectTab('category')">分类</button>
        <button type="button" class="tab" :class="{ active: activeTab === 'app' }" @click="selectTab('app')">App</button>
      </nav>

      <div class="workspace" :class="workspaceClass">
        <aside v-if="activeTab !== 'global'" class="scope-panel">
          <div class="scope-panel__head">
            <div>
              <h3>{{ scopePanelTitle }}</h3>
              <p>左侧选择具体项，右侧编辑对应手势。</p>
            </div>
          </div>

          <div class="scope-panel__actions">
            <input
              v-model.trim="scopeDraft"
              class="scope-input"
              :placeholder="scopeDraftPlaceholder"
              @keydown.enter.prevent="createScopeTarget"
            >
            <button type="button" class="secondary-button" @click="createScopeTarget">新增</button>
          </div>

          <div v-if="scopeItems.length > 0" class="scope-list">
            <button
              v-for="item in scopeItems"
              :key="item.name"
              type="button"
              class="scope-item"
              :class="{ active: item.name === selectedScopeName }"
              @click="selectScope(item.name)"
            >
              <span>{{ item.name }}</span>
              <small>{{ item.count }} 条</small>
            </button>
          </div>

          <div v-else class="empty-state">还没有{{ scopePanelTitle }}规则，先新增一个。</div>

          <div v-if="selectedScopeName" class="scope-editor">
            <label>
              当前{{ scopePanelTitle }}
              <input :value="selectedScopeName" @input="renameSelectedScope($event.target.value)">
            </label>
            <div class="scope-editor__actions">
              <button type="button" class="danger-button" @click="deleteSelectedScope">删除当前项</button>
            </div>
          </div>
        </aside>

        <section class="rules-panel">
          <div class="rules-panel__head">
            <div>
              <h3>{{ rulesPanelTitle }}</h3>
              <p>{{ rulesPanelSubtitle }}</p>
            </div>
            <button type="button" class="primary-button" @click="addRule">新增规则</button>
          </div>

          <div v-if="visibleRules.length === 0" class="empty-state empty-state--large">
            {{ emptyRulesText }}
          </div>

          <div v-else class="rules-grid">
            <article v-for="rule in visibleRules" :key="rule.id" class="rule-card">
              <div class="rule-card__head">
                <strong>{{ formatPattern(rule.patternText) }}</strong>
                <button type="button" class="ghost-button" @click="removeRule(rule.id)">删除</button>
              </div>

              <div class="rule-card__scope">{{ describeRuleScope(rule) }}</div>

              <label>
                方向
                <input v-model.trim="rule.patternText" placeholder="Left, Right">
              </label>

              <label>
                动作名称
                <input v-model.trim="rule.actionName" placeholder="Close Tab">
              </label>

              <label>
                快捷键
                <input
                  v-model.trim="rule.keysText"
                  placeholder="Control + W"
                  @focus="startRecording($event.target)"
                  @blur="stopRecording"
                >
              </label>
            </article>
          </div>
        </section>
      </div>
    </section>

    <footer class="toolbar">
      <div class="toolbar__message" :data-state="configMessageState">{{ configMessage }}</div>
      <div class="toolbar__actions">
        <button type="button" class="secondary-button" @click="reloadRules">重新加载</button>
        <button type="button" class="secondary-button" @click="resetRules">恢复默认</button>
        <button type="button" class="primary-button" @click="saveRules">保存</button>
      </div>
    </footer>
  </div>
</template>
