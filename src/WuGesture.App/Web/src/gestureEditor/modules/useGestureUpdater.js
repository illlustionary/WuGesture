import { WEBVIEW_MESSAGE_TYPES } from '@/constants/gestureEditorOptions'
import { GESTURE_EDITOR_EVENTS, emitGestureEditorEvent } from '@/gestureEditor/events/gestureEditorEventBus'

export function useGestureUpdater({ state, setMessage, webView }) {
  let manualCheck = false

  function checkForUpdate() {
    if (!webView.isAvailable()) {
      setMessage('浏览器预览中无法检查更新，请在 WuGesture 桌面应用中操作。', 'error')
      return
    }

    if (state.updateChecking) {
      return
    }

    manualCheck = true
    state.updateChecking = true
    state.updateStatus = 'checking'
    state.updateInfo = null
    state.updateError = ''
    state.updateDialogOpen = false
    webView.post({ type: WEBVIEW_MESSAGE_TYPES.checkUpdate })
  }

  function startUpdate() {
    if (!webView.isAvailable() || !state.updateInfo) {
      return
    }

    state.updateStatus = 'downloading'
    state.updatePhase = 'downloading'
    state.updateProgress = 0
    state.updateError = ''
    state.updateDialogOpen = true
    webView.post({ type: WEBVIEW_MESSAGE_TYPES.startUpdate })
  }

  function cancelUpdate() {
    state.updateStatus = state.updateInfo ? 'available' : 'idle'
    state.updatePhase = ''
    state.updateProgress = 0
    webView.post({ type: WEBVIEW_MESSAGE_TYPES.cancelUpdate })
  }

  function dismissUpdate() {
    state.updateDialogOpen = false
  }

  function retryUpdate() {
    if (state.updateInfo) {
      startUpdate()
      return
    }

    checkForUpdate()
  }

  function openUpdateDialog() {
    if (state.updateInfo && state.updateStatus !== 'restarting') {
      state.updateDialogOpen = true
    }
  }

  function openReleasePage() {
    webView.postSilent({ type: WEBVIEW_MESSAGE_TYPES.openUpdatePage })
  }

  function handleUpdateMessage(message) {
    if (message.type === WEBVIEW_MESSAGE_TYPES.updateAvailable) {
      state.updateChecking = false
      state.updateStatus = 'available'
      state.updateInfo = {
        currentVersion: message.currentVersion ?? '',
        latestVersion: message.latestVersion ?? '',
        source: message.source ?? '',
        preferredSource: message.preferredSource ?? '',
        notes: message.notes ?? '',
        publishedAt: message.publishedAt ?? '',
        htmlUrl: message.htmlUrl ?? '',
        assetName: message.assetName ?? '',
        assetSize: Number(message.assetSize) || 0
      }
      state.updateError = ''
      state.updateDialogOpen = true
      manualCheck = false
      return
    }

    if (message.type === WEBVIEW_MESSAGE_TYPES.updateNone) {
      state.updateChecking = false
      state.updateStatus = 'none'
      state.updateInfo = null
      state.updateProgress = 0
      state.updatePhase = ''
      if (manualCheck) {
        setMessage('当前已是最新版本。', 'success')
      }
      manualCheck = false
      return
    }

    if (message.type === WEBVIEW_MESSAGE_TYPES.updateError) {
      state.updateChecking = false
      state.updateStatus = 'error'
      state.updateError = message.message ?? '检查更新失败。'
      if (manualCheck) {
        setMessage(state.updateError, 'error')
      }
      manualCheck = false
      return
    }

    if (message.type === WEBVIEW_MESSAGE_TYPES.updateProgress) {
      state.updateStatus = 'downloading'
      state.updatePhase = message.phase ?? ''
      state.updateProgress = Math.max(0, Math.min(100, Number(message.percent) || 0))
      return
    }

    if (message.type === WEBVIEW_MESSAGE_TYPES.updateFailed) {
      state.updateChecking = false
      state.updateStatus = 'error'
      state.updateError = message.message ?? '更新失败。'
      state.updateDialogOpen = true
      manualCheck = false
      return
    }

    if (message.type === WEBVIEW_MESSAGE_TYPES.updateRestarting) {
      state.updateStatus = 'restarting'
      state.updateDialogOpen = true
      setMessage(message.message ?? '更新已准备就绪，WuGesture 正在重启。', 'success')
      return
    }

    if (message.type === WEBVIEW_MESSAGE_TYPES.updateCompleted) {
      // The host restarted into the new version: reset the dialog state and land on the About page so the
      // user immediately sees the new version number without opening the settings window manually.
      state.updateChecking = false
      state.updateStatus = 'updated'
      state.updateInfo = null
      state.updateError = ''
      state.updatePhase = ''
      state.updateProgress = 0
      state.updateDialogOpen = false
      manualCheck = false
      setMessage(message.message ?? '更新完成。', 'success')
      emitGestureEditorEvent(GESTURE_EDITOR_EVENTS.navigate, { route: '/about', replace: true })
    }
  }

  return {
    checkForUpdate,
    startUpdate,
    cancelUpdate,
    dismissUpdate,
    retryUpdate,
    openUpdateDialog,
    openReleasePage,
    handleUpdateMessage
  }
}
