const listeners = new Map();

export const GESTURE_EDITOR_EVENTS = Object.freeze({
  navigate: "gesture-editor:navigate",
  notify: "gesture-editor:notify"
});

export function emitGestureEditorEvent(type, payload) {
  const handlers = listeners.get(type);
  if (!handlers || handlers.size === 0) {
    return Promise.resolve();
  }

  return Promise.all([...handlers].map((handler) => handler(payload)));
}

export function onGestureEditorEvent(type, handler) {
  if (!listeners.has(type)) {
    listeners.set(type, new Set());
  }

  const handlers = listeners.get(type);
  handlers.add(handler);

  return () => {
    handlers.delete(handler);
    if (handlers.size === 0) {
      listeners.delete(type);
    }
  };
}
