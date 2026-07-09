export function useGestureEditorNotifications({
  getAutoSaveTimer,
  getToast,
  state
}) {
  let pendingConfigResultToastTimer = 0;
  let saveActivityVersion = 0;

  function show(message, stateName = "idle", options = {}) {
    state.configMessage = message;
    state.configMessageState = stateName;
    if (options.notify !== false) {
      showToast(message, stateName);
    }
  }

  function showConfigResult(message, success, notify) {
    const stateName = success ? "success" : "error";
    if (!success || !notify) {
      show(message, stateName, { notify });
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
      if (version === saveActivityVersion && !getAutoSaveTimer()) {
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
    const toast = getToast();
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

  return {
    markSaveActivity,
    show,
    showConfigResult
  };
}
