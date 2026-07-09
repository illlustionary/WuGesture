import { computed, proxyRefs } from "vue";
import { getActionLabel, getGestureMnemonic } from "@/utils/gestureEditorFormatters";
import { useGestureEditorContext } from "@/gestureEditor/context/gestureEditorContext";

export function useGestureRulesStore() {
  const editor = useGestureEditorContext();

  return proxyRefs({
    appItems: computed(() => editor.appItems),
    categoryItems: computed(() => editor.categoryItems),
    globalRules: computed(() => editor.globalRules),
    createScopeTarget: editor.createScopeTarget,
    deleteSelectedScope: editor.deleteSelectedScope,
    getActionLabel,
    getApplicationsForCategory: editor.getApplicationsForCategory,
    getGestureMnemonic,
    getRulesForScope: editor.getRulesForScope,
    getSelectedName: editor.getSelectedName,
    openAddRule: editor.openAddRule,
    openApplicationPicker: editor.openApplicationPicker,
    openEditRule: editor.openEditRule,
    removeAppFromCategory: editor.removeAppFromCategory,
    removeRule: editor.removeRule,
    renameSelectedScope: editor.renameSelectedScope,
    selectScope: editor.selectScope,
    setActiveScope: editor.setActiveScope,
    updateApplicationDisplayName: editor.updateApplicationDisplayName,
    updateRuleActionName: editor.updateRuleActionName
  });
}
