import { WEBVIEW_MESSAGE_TYPES } from '@/constants/gestureEditorOptions'
import {
  cloneUiSettings,
  createDefaultUiSettings,
  normalizeEdgeActionInPlace,
  normalizeUiSettings
} from '@/utils/gestureEditorNormalizers'
import { buildConfigPayload, getWebDavSignature as createWebDavSignature } from '@/utils/gestureEditorPayloads'

export function useGestureConfigPersistence({
  getAutoSaveTimer,
  setAutoSaveTimer,
  state,
  initialized,
  setConfigResultMessage,
  setGesturePaused,
  setMessage,
  webView
}) {
  let pendingAutoSaveOptions = {}
  let preserveLocalEdgeActions = false
  let pendingWebDavTestSignature = ''

  function saveRules(options = {}) {
    if (getAutoSaveTimer()) {
      clearTimeout(getAutoSaveTimer())
      setAutoSaveTimer(0)
      pendingAutoSaveOptions = {}
    }

    const payload = getConfigPayload()

    if (payload.rules.length === 0) {
      setMessage('至少保留一条规则。', 'error')
      return
    }

    webView.post(
      {
        type: WEBVIEW_MESSAGE_TYPES.saveRules,
        ...payload
      },
      { notifyPreview: options.notifyPreview !== false }
    )
  }

  function scheduleSaveRules(options = {}) {
    if (!initialized.value) {
      return
    }

    if (!webView.isAvailable()) {
      return
    }

    pendingAutoSaveOptions = {
      ...pendingAutoSaveOptions,
      ...options
    }

    if (getAutoSaveTimer()) {
      clearTimeout(getAutoSaveTimer())
    }

    setAutoSaveTimer(
      window.setTimeout(() => {
        const options = pendingAutoSaveOptions
        setAutoSaveTimer(0)
        pendingAutoSaveOptions = {}
        saveRules(options)
      }, 250)
    )
  }

  function reloadRules() {
    preserveLocalEdgeActions = false
    webView.post({ type: WEBVIEW_MESSAGE_TYPES.reloadRules })
  }

  function resetRules() {
    preserveLocalEdgeActions = false
    webView.post({ type: WEBVIEW_MESSAGE_TYPES.resetRules })
  }

  function exportConfigToLocal() {
    const payload = getConfigPayload()
    if (payload.rules.length === 0) {
      setMessage('至少保留一条规则。', 'error')
      return
    }

    if (!webView.isAvailable()) {
      setMessage('浏览器预览不支持导出本地配置，请在 WuGesture 桌面应用中操作。', 'error')
      return
    }

    webView.post({
      type: WEBVIEW_MESSAGE_TYPES.exportConfig,
      ...payload
    })
  }

  function importConfigFromLocal() {
    if (!webView.isAvailable()) {
      setMessage('浏览器预览不支持导入本地配置，请在 WuGesture 桌面应用中操作。', 'error')
      return
    }

    preserveLocalEdgeActions = false
    webView.post({ type: WEBVIEW_MESSAGE_TYPES.importConfig })
  }

  function getUiSettingsSnapshot() {
    return cloneUiSettings(state.uiSettings)
  }

  function saveUiSettings(nextSettings) {
    const previousPaused = Boolean(state.uiSettings.appBehavior.gesturePaused)
    state.uiSettings = normalizeUiSettings(nextSettings)
    const nextPaused = Boolean(state.uiSettings.appBehavior.gesturePaused)
    if (previousPaused !== nextPaused) {
      setGesturePaused(nextPaused)
    }
    saveRules({ notifyPreview: false })
  }

  function resetUiSettings() {
    state.uiSettings = createDefaultUiSettings()
    setGesturePaused(false)
    state.webDavTestState = 'idle'
    state.webDavTestedSignature = ''
    pendingWebDavTestSignature = ''
    saveRules({ notifyPreview: false })
    setMessage('已恢复默认设置，手势和边缘操作未改变。', 'success')
  }

  function previewLevelOsd(kind) {
    if (!webView.isAvailable()) {
      setMessage('浏览器预览中无法显示系统 OSD。', 'error')
      return
    }

    webView.post({
      type: WEBVIEW_MESSAGE_TYPES.previewLevelOsd,
      kind
    })
  }

  function testWebDavConnection() {
    const payload = getConfigPayload()
    const signature = getWebDavSignature(payload.uiSettings.webDav)
    if (!payload.uiSettings.webDav.address) {
      setMessage('请先填写 WebDAV 地址，再测试连接。', 'error')
      return
    }

    if (!webView.isAvailable()) {
      setMessage('浏览器预览不支持测试 WebDAV 连接，请在 WuGesture 桌面应用中操作。', 'error')
      return
    }

    state.webDavTesting = true
    state.webDavTestState = 'idle'
    state.webDavTestedSignature = ''
    pendingWebDavTestSignature = signature
    webView.post({
      type: WEBVIEW_MESSAGE_TYPES.webDavTest,
      ...payload
    })
  }

  function saveConfigToWebDav() {
    const payload = getConfigPayload()
    if (payload.rules.length === 0) {
      setMessage('至少保留一条规则。', 'error')
      return
    }

    if (!isWebDavTested(payload.uiSettings.webDav)) {
      setMessage('请先测试连接，确认当前 WebDAV 配置可用。', 'error')
      return
    }

    webView.post({
      type: WEBVIEW_MESSAGE_TYPES.webDavSave,
      ...payload
    })
  }

  function restoreConfigFromWebDav() {
    const payload = getConfigPayload()
    if (!isWebDavTested(payload.uiSettings.webDav)) {
      setMessage('请先测试连接，确认当前 WebDAV 配置可用。', 'error')
      return
    }

    webView.post({
      type: WEBVIEW_MESSAGE_TYPES.webDavRestore,
      ...payload
    })
  }

  function updateEdgeAction(action, patch = {}, options = {}) {
    if (!action) {
      return
    }

    preserveLocalEdgeActions = true
    Object.assign(action, patch)
    normalizeEdgeActionInPlace(action)
    scheduleSaveRules(options)
  }

  function handleConfigResult(message) {
    if (message.success) {
      window.setTimeout(() => {
        preserveLocalEdgeActions = false
      }, 500)
    } else {
      preserveLocalEdgeActions = false
    }
    setConfigResultMessage(message.message, message.success, message.operation !== 'save')
  }

  function handleWebDavResult(message) {
    if (message.operation === 'test') {
      state.webDavTesting = false
      if (message.success) {
        state.webDavTestedSignature = pendingWebDavTestSignature
        state.webDavTestState = 'success'
      } else {
        state.webDavTestedSignature = ''
        state.webDavTestState = 'error'
      }
      pendingWebDavTestSignature = ''
    }

    setMessage(message.message, message.success ? 'success' : 'error')
  }

  function shouldPreserveLocalEdgeActions() {
    return preserveLocalEdgeActions
  }

  function getConfigPayload() {
    return buildConfigPayload(state)
  }

  function isWebDavTested(settings = state.uiSettings.webDav) {
    const signature = getWebDavSignature(settings)
    return Boolean(signature && signature === state.webDavTestedSignature)
  }

  function getWebDavSignature(settings = state.uiSettings.webDav) {
    return createWebDavSignature(settings)
  }

  return {
    exportConfigToLocal,
    getUiSettingsSnapshot,
    getWebDavSignature,
    handleConfigResult,
    handleWebDavResult,
    importConfigFromLocal,
    isWebDavTested,
    reloadRules,
    resetRules,
    resetUiSettings,
    previewLevelOsd,
    restoreConfigFromWebDav,
    saveConfigToWebDav,
    saveRules,
    saveUiSettings,
    scheduleSaveRules,
    shouldPreserveLocalEdgeActions,
    testWebDavConnection,
    updateEdgeAction
  }
}
