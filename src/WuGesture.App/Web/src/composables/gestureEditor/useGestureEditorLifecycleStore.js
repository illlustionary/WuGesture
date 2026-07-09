import { computed, proxyRefs } from "vue";
import { useGestureEditorContext } from "./gestureEditorContext";

export function useGestureEditorLifecycleStore() {
  const editor = useGestureEditorContext();

  return proxyRefs({
    statusText: computed(() => editor.state.statusText),
    statusState: computed(() => editor.state.statusState),
    initialize: editor.initialize
  });
}
