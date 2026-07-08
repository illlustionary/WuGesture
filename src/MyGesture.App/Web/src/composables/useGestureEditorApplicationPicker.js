export function useGestureEditorApplicationPicker({
  state,
  addExcludedApplication,
  closeApplicationPickerState,
  createRequestId,
  createRule,
  ensureApplication,
  getScopeItems,
  postWebMessage,
  scheduleSaveRules,
  setMessage,
  setSelectedName
}) {
  const pendingRequests = new Map();

  function openApplicationPicker(categoryName = "", scopeKind = "category") {
    state.applicationPickerCategory = String(categoryName ?? "").trim();
    state.applicationPickerScopeKind = scopeKind === "app" ? "app" : "category";
    state.applicationPickerTarget = "scope";
    state.applicationPickerOpen = true;
  }

  function openExcludedApplicationPicker() {
    state.applicationPickerCategory = "";
    state.applicationPickerScopeKind = "";
    state.applicationPickerTarget = "exclusion";
    state.applicationPickerOpen = true;
  }

  function closeApplicationPicker() {
    closeApplicationPickerState();
  }

  function selectApplication(categoryName = "") {
    requestApplication("select-application", categoryName);
  }

  function pickApplicationWindow(categoryName = "") {
    requestApplication("pick-application-window", categoryName);
  }

  function requestApplication(type, categoryName) {
    const category = String(categoryName || state.applicationPickerCategory || "").trim();
    const requestId = createRequestId();
    pendingRequests.set(requestId, {
      target: state.applicationPickerTarget,
      scopeKind: state.applicationPickerScopeKind,
      category
    });
    closeApplicationPicker();
    postWebMessage({
      type,
      requestId,
      category
    });
  }

  function addSelectedApplication(message) {
    const name = String(message.name ?? "").trim();
    if (!name) {
      return;
    }

    const requestContext = pendingRequests.get(message.requestId) ?? null;
    pendingRequests.delete(message.requestId);
    if (requestContext?.target === "exclusion") {
      addExcludedApplication(message);
      return;
    }

    const application = ensureApplication(name);
    application.displayName = String(message.displayName ?? application.displayName ?? name).trim();
    application.path = String(message.path ?? "").trim();
    const selectedCategory = String(message.category ?? "").trim();
    if (selectedCategory || requestContext?.scopeKind !== "app") {
      application.category = selectedCategory;
    }
    application.icon = String(message.icon ?? application.icon ?? "").trim();

    if (requestContext?.scopeKind === "app" && !getScopeItems("app").some((item) => item.name === application.name)) {
      state.rules.push(createRule("app", application.name));
    }

    setSelectedName("app", application.name);
    if (application.category) {
      setSelectedName("category", application.category);
    }

    setMessage("已添加程序。", "success");
    scheduleSaveRules();
  }

  return {
    openApplicationPicker,
    openExcludedApplicationPicker,
    closeApplicationPicker,
    selectApplication,
    pickApplicationWindow,
    addSelectedApplication
  };
}
