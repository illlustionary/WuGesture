import { computed, proxyRefs } from "vue";
import { useGestureEditorStore } from "../gestureEditorStore";

export function useGestureSettingsStore() {
  const editor = useGestureEditorStore();

  return proxyRefs({
    uiSettings: computed(() => editor.state.uiSettings),
    webDavTesting: computed(() => editor.state.webDavTesting),
    webDavTestState: computed(() => editor.state.webDavTestState),
    webDavTestedSignature: computed(() => editor.state.webDavTestedSignature),
    exportConfigToLocal: editor.exportConfigToLocal,
    getUiSettingsSnapshot: editor.getUiSettingsSnapshot,
    getWebDavSignature: editor.getWebDavSignature,
    importConfigFromLocal: editor.importConfigFromLocal,
    resetUiSettings: editor.resetUiSettings,
    restoreConfigFromWebDav: editor.restoreConfigFromWebDav,
    saveConfigToWebDav: editor.saveConfigToWebDav,
    saveUiSettings: editor.saveUiSettings,
    testWebDavConnection: editor.testWebDavConnection
  });
}
