export function useGestureEditorWebViewBridge({ notifications }) {
  function isAvailable() {
    return Boolean(window.chrome?.webview);
  }

  function addMessageListener(handler) {
    if (isAvailable()) {
      window.chrome.webview.addEventListener("message", handler);
    }
  }

  function post(message, options = {}) {
    if (isAvailable()) {
      window.chrome.webview.postMessage(message);
      return;
    }

    notifications.show("浏览器预览中不会写入本机配置。", "idle", {
      notify: options.notifyPreview !== false
    });
  }

  function postSilent(message) {
    if (isAvailable()) {
      window.chrome.webview.postMessage(message);
    }
  }

  return {
    addMessageListener,
    isAvailable,
    post,
    postSilent
  };
}
