import { SCOPE_KINDS } from "../../constants/gestureEditorOptions";

export function useGestureScopes({
  activeScope,
  collectAppItems,
  collectCategoryItems,
  createRule,
  notifications,
  scheduleSaveRules,
  state
}) {
  function setActiveScope(scope) {
    activeScope.value = scope;
    ensureSelection(scope);
  }

  function selectScope(kind, name) {
    const trimmed = String(name ?? "").trim();
    if (!trimmed) {
      return;
    }

    setSelectedName(kind, trimmed);
  }

  function getRulesForScope(kind, name = "") {
    if (kind === SCOPE_KINDS.global) {
      return getRulesByKind(SCOPE_KINDS.global);
    }

    const scopeName = String(name || getSelectedName(kind)).trim();
    if (!scopeName) {
      return [];
    }

    return state.rules.filter((rule) => rule.scopeKind === kind && rule.scopeName === scopeName);
  }

  function getVisibleRules(scope) {
    return getRulesForScope(scope, getSelectedName(scope));
  }

  function getRulesByKind(kind) {
    return state.rules.filter((rule) => rule.scopeKind === kind);
  }

  function getScopeItems(kind) {
    return kind === SCOPE_KINDS.category ? collectCategoryItems() : collectAppItems();
  }

  function createScopeTarget(kind, name) {
    const trimmed = String(name ?? "").trim();
    if (!trimmed) {
      notifications.show("请输入名称后再新增。", "error");
      return false;
    }

    if (!getScopeItems(kind).some((item) => item.name === trimmed)) {
      state.rules.push(createRule(kind, trimmed));
    }

    setSelectedName(kind, trimmed);
    notifications.show("已新增。", "success");
    scheduleSaveRules();
    return true;
  }

  function renameSelectedScope(kind, nextName, sourceName = getSelectedName(kind)) {
    const name = String(nextName ?? "").trim();
    const currentName = String(sourceName ?? "").trim() || getSelectedName(kind);
    if (!name || !currentName || currentName === name) {
      return false;
    }

    const scopeItems = getScopeItems(kind);
    if (scopeItems.some((item) => item.name === name && item.name !== currentName)) {
      notifications.show("名称已存在，请换一个分类名称。", "error");
      return false;
    }

    for (const rule of state.rules) {
      if (rule.scopeKind === kind && rule.scopeName === currentName) {
        rule.scopeName = name;
      }
    }

    if (kind === "category") {
      for (const application of state.applications) {
        if (application.category === currentName) {
          application.category = name;
        }
      }
    } else if (kind === "app") {
      const application = state.applications.find((item) => item.name === currentName);
      if (application) {
        application.name = name;
      }
    }

    setSelectedName(kind, name);
    scheduleSaveRules();
    notifications.show(kind === "category" ? "已更新分类名称。" : "已更新程序名称。", "success");
    return true;
  }

  function deleteSelectedScope(kind = activeScope.value) {
    const name = getSelectedName(kind);
    if (!name) {
      return;
    }

    state.rules = state.rules.filter((rule) => !(rule.scopeKind === kind && rule.scopeName === name));
    if (kind === "category") {
      for (const application of state.applications) {
        if (application.category === name) {
          application.category = "";
        }
      }
    } else if (kind === "app") {
      state.applications = state.applications.filter((application) => application.name !== name);
    }

    ensureSelection(kind);
    notifications.show("已删除当前项。", "success");
    scheduleSaveRules();
  }

  function ensureSelection(kind) {
    const items = getScopeItems(kind);
    const current = getSelectedName(kind);
    if (current && items.some((item) => item.name === current)) {
      return;
    }

    setSelectedName(kind, items[0]?.name ?? "");
  }

  function getSelectedName(kind) {
    if (kind === "category") {
      return state.selectedCategory;
    }

    if (kind === "app") {
      return state.selectedApp;
    }

    return "";
  }

  function setSelectedName(kind, name) {
    if (kind === "category") {
      state.selectedCategory = name;
    } else if (kind === "app") {
      state.selectedApp = name;
    }
  }

  function getFirstScopeName(kind) {
    return getScopeItems(kind)[0]?.name ?? "";
  }

  return {
    createScopeTarget,
    deleteSelectedScope,
    ensureSelection,
    getFirstScopeName,
    getRulesByKind,
    getRulesForScope,
    getScopeItems,
    getSelectedName,
    getVisibleRules,
    renameSelectedScope,
    selectScope,
    setActiveScope,
    setSelectedName
  };
}
