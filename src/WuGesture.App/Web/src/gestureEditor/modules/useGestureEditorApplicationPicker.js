import { WEBVIEW_MESSAGE_TYPES } from '@/constants/gestureEditorOptions'

export function useGestureEditorApplicationPicker({
  state,
  closeApplicationPickerState,
  createRequestId,
  notifications,
  webView
}) {
  const pendingRequestIds = new Set()

  function open() {
    state.applicationPickerOpen = true
  }

  function close() {
    closeApplicationPickerState()
  }

  function selectApplication() {
    requestApplication(WEBVIEW_MESSAGE_TYPES.selectApplication)
  }

  function pickApplicationWindow() {
    requestApplication(WEBVIEW_MESSAGE_TYPES.pickApplicationWindow)
  }

  function requestApplication(type) {
    if (!webView.isAvailable()) {
      notifications.show('浏览器预览无法选择程序，请在桌面应用中操作。', 'error')
      return
    }

    const requestId = createRequestId()
    pendingRequestIds.add(requestId)
    closeApplicationPickerState()
    webView.post({ type, requestId, category: '' })
  }

  function takeSelectedApplication(message) {
    if (!pendingRequestIds.delete(message?.requestId)) {
      return null
    }

    const name = String(message?.name ?? '').trim()
    const path = String(message?.path ?? '').trim()
    if (!name || !path) {
      return null
    }

    return {
      name,
      displayName: String(message?.displayName ?? name).trim() || name,
      path,
      icon: String(message?.icon ?? '').trim()
    }
  }

  return {
    open,
    close,
    selectApplication,
    pickApplicationWindow,
    takeSelectedApplication
  }
}
