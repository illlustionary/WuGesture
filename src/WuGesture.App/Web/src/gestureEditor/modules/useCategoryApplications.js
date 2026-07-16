export function useCategoryApplications({
  ensureApplication,
  getSelectedName,
  notifications,
  scheduleSaveRules,
  setSelectedName,
  state
}) {
  function assignSelectedAppToCategory(categoryName = getSelectedName("category"), appName = getSelectedName("app")) {
    const category = String(categoryName ?? "").trim();
    const name = String(appName ?? "").trim();
    if (!category || !name) {
      notifications.show("请先选择分类和程序。", "error");
      return;
    }

    const application = ensureApplication(name);
    application.categories = application.categories.filter((item) => item !== category);
    application.categories.push(category);
    setSelectedName("category", category);
    notifications.show("已关联程序到分类。", "success");
    scheduleSaveRules();
  }

  function removeAppFromCategory(appName, categoryName = getSelectedName("category")) {
    const category = String(categoryName ?? "").trim();
    const name = String(appName ?? "").trim();
    if (!category || !name) {
      return;
    }

    const application = state.applications.find((item) => item.name === name && item.categories.includes(category));
    if (application) {
      application.categories = application.categories.filter((item) => item !== category);
      notifications.show("已移除分类关联。", "success");
      scheduleSaveRules();
    }
  }

  return {
    assignSelectedAppToCategory,
    removeAppFromCategory
  };
}
