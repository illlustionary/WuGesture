import { onBeforeUnmount } from 'vue'
import { useToast } from 'vue-toastification'
import { GESTURE_EDITOR_EVENTS, onGestureEditorEvent } from '@/gestureEditor/events/gestureEditorEventBus'

export function useGestureEditorEventListeners(router) {
  const toast = useToast()
  const stopListeningToNotifications = onGestureEditorEvent(
    GESTURE_EDITOR_EVENTS.notify,
    ({ message, stateName = 'idle' } = {}) => showToast(toast, message, stateName)
  )
  const stopListeningToNavigation = onGestureEditorEvent(
    GESTURE_EDITOR_EVENTS.navigate,
    ({ replace = false, route } = {}) => (replace ? router.replace(route) : router.push(route))
  )

  onBeforeUnmount(() => {
    stopListeningToNotifications()
    stopListeningToNavigation()
  })
}

function showToast(toast, message, stateName) {
  const content = String(message ?? '').trim()
  if (!content) {
    return
  }

  if (stateName === 'success') {
    toast.success(content)
    return
  }

  if (stateName === 'error') {
    toast.error(content)
    return
  }

  toast.info(content)
}
