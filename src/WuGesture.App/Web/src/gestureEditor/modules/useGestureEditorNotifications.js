export function useGestureEditorNotifications({
  getToast,
  state
}) {
  function show(message, stateName = "idle", options = {}) {
    state.configMessage = message;
    state.configMessageState = stateName;
    if (options.notify !== false) {
      showToast(message, stateName);
    }
  }

  function showConfigResult(message, success, notifySuccess) {
    const stateName = success ? "success" : "error";
    state.configMessage = message;
    state.configMessageState = stateName;
    if (!success || notifySuccess) {
      showToast(message, stateName);
      return;
    }
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
    show,
    showConfigResult
  };
}
