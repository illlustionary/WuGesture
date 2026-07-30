import {
  GESTURE_EDITOR_EVENTS,
  emitGestureEditorEvent
} from "@/gestureEditor/events/gestureEditorEventBus";

export function useGestureEditorNotifications({
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
    emitGestureEditorEvent(GESTURE_EDITOR_EVENTS.notify, {
      message,
      stateName
    });
  }

  return {
    show,
    showConfigResult
  };
}
