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

    return state.applications.filter((application) => application.categories.includes(trimmed));
  }

  function getApplication(appName = getSelectedName("app")) {
    const name = String(appName ?? "").trim();
    if (!name) {
      return null;
    }

    return state.applications.find((application) => application.name === name) ?? null;
  }

  function getCategoriesForApplication(appName = getSelectedName("app")) {
    return [...(getApplication(appName)?.categories ?? [])];
  }

  function moveApplicationCategory(appName, categoryName, direction) {
    const application = getApplication(appName);
    const category = String(categoryName ?? "").trim();
    const offset = direction === "down" ? 1 : -1;
    const index = application?.categories.indexOf(category) ?? -1;
    const nextIndex = index + offset;
    if (!application || index < 0 || nextIndex < 0 || nextIndex >= application.categories.length) {
      return false;
    }

    const categories = [...application.categories];
    [categories[index], categories[nextIndex]] = [categories[nextIndex], categories[index]];
    return setApplicationCategories(appName, categories);
  }

  function setApplicationCategories(appName, categories) {
    const application = getApplication(appName);
    if (!application) {
      return false;
    }

    const normalized = [];
    for (const value of Array.isArray(categories) ? categories : []) {
      const category = String(value ?? "").trim();
      if (category && !normalized.includes(category)) {
        normalized.push(category);
      }
    }

    application.categories = normalized;
    scheduleSaveRules();
    return true;
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
    const category = String(categoryName ?? "").trim();
    application.categories = category ? [category] : [];
    notifications.show(category ? "已设置程序分类。" : "已清除程序分类。", "success");
    scheduleSaveRules();
  }

  function ensureApplication(name) {
    const trimmed = String(name ?? "").trim();
    let application = state.applications.find((item) => item.name === trimmed);
    if (!application) {
      application = { name: trimmed, displayName: trimmed, path: "", categories: [], icon: "" };
      state.applications.push(application);
    }

    return application;
  }

  return {
    ensureApplication,
    getApplication,
    getApplicationsForCategory,
    getCategoriesForApplication,
    moveApplicationCategory,
    setApplicationCategories,
    updateApplicationCategory,
    updateApplicationDisplayName
  };
}
