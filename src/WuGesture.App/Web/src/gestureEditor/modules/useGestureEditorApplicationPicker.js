import { SCOPE_KINDS, WEBVIEW_MESSAGE_TYPES } from '@/constants/gestureEditorOptions'

export function useGestureEditorApplicationPicker({
  state,
  addExcludedApplication,
  closeApplicationPickerState,
  createRequestId,
  createRule,
  ensureApplication,
  getScopeItems,
  notifications,
  webView,
  scheduleSaveRules,
  setSelectedName
}) {
  const pendingRequests = new Map()

  function openApplicationPicker(categoryName = '', scopeKind = SCOPE_KINDS.category) {
    state.applicationPickerCategory = String(categoryName ?? '').trim()
    state.applicationPickerScopeKind = scopeKind === SCOPE_KINDS.app ? SCOPE_KINDS.app : SCOPE_KINDS.category
    state.applicationPickerTarget = 'scope'
    state.applicationPickerOpen = true
  }

  function openExcludedApplicationPicker() {
    state.applicationPickerCategory = ''
    state.applicationPickerScopeKind = ''
    state.applicationPickerTarget = 'exclusion'
    state.applicationPickerOpen = true
  }

  function closeApplicationPicker() {
    closeApplicationPickerState()
  }

  function selectApplication(categoryName = '') {
    requestApplication(WEBVIEW_MESSAGE_TYPES.selectApplication, categoryName)
  }

  function pickApplicationWindow(categoryName = '') {
    requestApplication(WEBVIEW_MESSAGE_TYPES.pickApplicationWindow, categoryName)
  }

  function requestApplication(type, categoryName) {
    const category = String(categoryName || state.applicationPickerCategory || '').trim()
    const requestId = createRequestId()
    pendingRequests.set(requestId, {
      target: state.applicationPickerTarget,
      scopeKind: state.applicationPickerScopeKind,
      category
    })
    closeApplicationPicker()
    webView.post({
      type,
      requestId,
      category
    })
  }

  function addSelectedApplication(message) {
    const name = String(message.name ?? '').trim()
    if (!name) {
      return
    }

    const requestContext = pendingRequests.get(message.requestId) ?? null
    pendingRequests.delete(message.requestId)
    if (requestContext?.target === 'exclusion') {
      addExcludedApplication(message)
      return
    }

    const requestedCategory = String(requestContext?.category ?? message.category ?? '').trim()
    const application = ensureApplication(name)
    application.displayName = String(message.displayName ?? application.displayName ?? name).trim()
    application.path = String(message.path ?? '').trim()
    if (requestedCategory) {
      application.categories = application.categories.filter(category => category !== requestedCategory)
      application.categories.push(requestedCategory)
    }
    application.icon = String(message.icon ?? application.icon ?? '').trim()

    if (
      requestContext?.scopeKind === SCOPE_KINDS.app &&
      !getScopeItems(SCOPE_KINDS.app).some(item => item.name === application.name)
    ) {
      state.rules.push(createRule(SCOPE_KINDS.app, application.name))
    }

    setSelectedName(SCOPE_KINDS.app, application.name)
    if (application.categories.length > 0) {
      setSelectedName(SCOPE_KINDS.category, application.categories.at(-1))
    }

    notifications.show('已添加程序。', 'success')
    scheduleSaveRules()
  }

  return {
    openApplicationPicker,
    openExcludedApplicationPicker,
    closeApplicationPicker,
    selectApplication,
    pickApplicationWindow,
    addSelectedApplication
  }
}
