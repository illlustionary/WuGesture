export function useGestureApplications({
  getSelectedName,
  notifications,
  scheduleSaveRules,
  state
}) {
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
      notifications.show("请先选择一个程序。", "error");
      return;
    }

    const application = ensureApplication(name);
    application.displayName = String(displayName ?? "").trim();
    scheduleSaveRules();
  }

  function updateApplicationCategory(appName = getSelectedName("app"), categoryName = "") {
    const name = String(appName ?? "").trim();
    if (!name) {
      notifications.show("请先选择一个程序。", "error");
      return;
    }

    const application = ensureApplication(name);
    application.category = String(categoryName ?? "").trim();
    notifications.show(application.category ? "已设置程序分类。" : "已清除程序分类。", "success");
    scheduleSaveRules();
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

  return {
    ensureApplication,
    getApplication,
    getApplicationsForCategory,
    updateApplicationCategory,
    updateApplicationDisplayName
  };
}
