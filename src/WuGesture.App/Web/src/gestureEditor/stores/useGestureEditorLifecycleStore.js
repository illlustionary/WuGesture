import { computed, proxyRefs } from "vue";
import { useGestureEditorContext } from "@/gestureEditor/context/gestureEditorContext";

export function useGestureEditorLifecycleStore() {
  const editor = useGestureEditorContext();

  return proxyRefs({
    statusText: computed(() => editor.state.statusText),
    statusState: computed(() => editor.state.statusState),
    appInfo: computed(() => editor.state.appInfo),
    windowMaximized: computed(() => editor.state.windowMaximized),
    windowResizing: computed(() => editor.state.windowResizing),
    initialize: editor.initialize,
    toggleGesturePaused: editor.toggleUserPaused,
    minimizeWindow: editor.minimizeWindow,
    toggleWindowMaximize: editor.toggleWindowMaximize,
    closeWindow: editor.closeWindow,
    startWindowDrag: editor.startWindowDrag,
    startWindowResize: editor.startWindowResize
  });
}
