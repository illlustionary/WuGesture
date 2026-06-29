const statusEl = document.querySelector("#status");
const lastGestureEl = document.querySelector("#lastGesture");
const gestureDockEl = document.querySelector("#gestureDock");

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

    if (message.type === "gesture-progress") {
      gestureDockEl.textContent = message.pattern.length === 0
        ? "正在识别手势"
        : `当前手势: ${message.pattern.join(" > ")}`;
    }
  });

  window.chrome.webview.postMessage("get-status");
} else {
  setStatus("浏览器预览");
}
