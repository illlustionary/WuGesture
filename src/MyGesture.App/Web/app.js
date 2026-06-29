const statusEl = document.querySelector("#status");
const lastGestureEl = document.querySelector("#lastGesture");
const gestureDockEl = document.querySelector("#gestureDock");
const configPathEl = document.querySelector("#configPath");
const rulesEl = document.querySelector("#rules");
const saveRulesButton = document.querySelector("#saveRules");
const reloadRulesButton = document.querySelector("#reloadRules");
const resetRulesButton = document.querySelector("#resetRules");
const configMessageEl = document.querySelector("#configMessage");

let currentRules = [];
let recordingInput = null;

function setStatus(value) {
  statusEl.textContent = value === "running" ? "运行中" : value;
}

if (window.chrome?.webview) {
  window.chrome.webview.addEventListener("message", (event) => {
    const message = event.data;
    if (message.type === "status") {
      setStatus(message.status);
    }

    if (message.type === "gesture") {
      lastGestureEl.textContent = `${message.pattern.join(", ")} -> ${message.action}`;
      gestureDockEl.textContent = `${message.pattern.join(" > ")} -> ${message.action}`;
    }

    if (message.type === "gesture-action-failed") {
      lastGestureEl.textContent = `${message.pattern.join(", ")} -> ${message.action} 执行失败: ${message.error}`;
      gestureDockEl.textContent = `${message.action} 执行失败`;
    }

    if (message.type === "gesture-progress") {
      gestureDockEl.textContent = message.pattern.length === 0
        ? "正在识别手势"
        : `当前手势: ${message.pattern.join(" > ")}`;
    }

    if (message.type === "rules") {
      currentRules = message.rules;
      configPathEl.textContent = message.configPath;
      rulesEl.replaceChildren(...currentRules.map(createRuleCard));
    }

    if (message.type === "config-result") {
      configMessageEl.textContent = message.message;
      configMessageEl.dataset.state = message.success ? "success" : "error";
    }
  });

  window.chrome.webview.postMessage("get-status");
} else {
  setStatus("浏览器预览");
}

saveRulesButton.addEventListener("click", () => {
  postWebMessage({
    type: "save-rules",
    rules: collectRules()
  });
});

reloadRulesButton.addEventListener("click", () => {
  postWebMessage({
    type: "reload-rules"
  });
});

resetRulesButton.addEventListener("click", () => {
  postWebMessage({
    type: "reset-rules"
  });
});

document.addEventListener("keydown", (event) => {
  if (!recordingInput || event.target !== recordingInput) {
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
    recordingInput.value = getModifierKeys(event).join(" + ");
    return;
  }

  if (!shouldRecordKey(event)) {
    return;
  }

  event.preventDefault();
  event.stopPropagation();

  const keys = [...getModifierKeys(event), normalizeKey(event.key)];
  recordingInput.value = [...new Set(keys)].join(" + ");
  stopRecording();
}, true);

document.addEventListener("keyup", (event) => {
  if (!recordingInput || event.target !== recordingInput || !isModifierOnly(event.key)) {
    return;
  }

  event.preventDefault();
  event.stopPropagation();
}, true);

function createRuleCard(rule, index) {
  const article = document.createElement("article");
  const title = document.createElement("strong");
  const scope = document.createElement("span");
  const actionNameLabel = document.createElement("label");
  const actionNameInput = document.createElement("input");
  const keysLabel = document.createElement("label");
  const keysInput = document.createElement("input");

  article.dataset.index = index.toString();
  title.textContent = rule.pattern.join(", ");
  scope.textContent = rule.scope;
  actionNameLabel.textContent = "动作名称";
  keysLabel.textContent = "快捷键";
  actionNameInput.name = "actionName";
  actionNameInput.value = rule.actionName;
  keysInput.name = "keys";
  keysInput.value = rule.keys.join(" + ");
  keysInput.addEventListener("focus", () => startRecording(keysInput));
  keysInput.addEventListener("blur", () => stopRecording());

  actionNameLabel.append(actionNameInput);
  keysLabel.append(keysInput);
  article.append(title, scope, actionNameLabel, keysLabel);
  return article;
}

function collectRules() {
  return [...rulesEl.querySelectorAll("article")].map((article) => {
    const index = Number(article.dataset.index);
    const source = currentRules[index];
    const actionName = article.querySelector('input[name="actionName"]').value.trim();
    const keys = article.querySelector('input[name="keys"]').value
      .split("+")
      .map((key) => key.trim())
      .filter(Boolean);

    return {
      scope: source.scope,
      pattern: source.pattern,
      actionName,
      action: {
        type: "hotkey",
        keys
      }
    };
  });
}

function postWebMessage(message) {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage(message);
  }
}

function startRecording(input) {
  stopRecording();
  recordingInput = input;
  recordingInput.dataset.previousValue = recordingInput.value;
  recordingInput.placeholder = "按组合键或手动输入";
  recordingInput.classList.add("is-recording");
  configMessageEl.textContent = "快捷键输入框已监听组合键，也可手动输入";
}

function stopRecording() {
  if (!recordingInput) {
    return;
  }

  recordingInput.placeholder = "";
  recordingInput.classList.remove("is-recording");
  delete recordingInput.dataset.previousValue;

  recordingInput = null;
  configMessageEl.textContent = "";
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
  return event.ctrlKey ||
    event.altKey ||
    event.shiftKey ||
    event.metaKey ||
    isSpecialKey(event.key);
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
