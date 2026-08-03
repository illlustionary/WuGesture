import { SCOPE_KINDS } from '@/constants/gestureEditorOptions'
import { GESTURE_EDITOR_EVENTS, emitGestureEditorEvent } from '@/gestureEditor/events/gestureEditorEventBus'

export function useGestureEditorNavigation() {
  function getRouteGroup(path = '') {
    return String(path).replace(/^\//, '').split('/')[0] || SCOPE_KINDS.global
  }

  function navigateToScope(scopeKind, scopeName = '') {
    return navigate(getScopeRoute(scopeKind, scopeName))
  }

  function replaceScope(scopeKind, scopeName = '') {
    return navigate(getScopeRoute(scopeKind, scopeName), { replace: true })
  }

  function navigateToExclusion(selected = '') {
    return navigate({
      path: '/exclusions',
      query: selected ? { selected } : undefined
    })
  }

  return {
    getRouteGroup,
    navigateToExclusion,
    navigateToScope,
    replaceScope
  }
}

function navigate(route, { replace = false } = {}) {
  return emitGestureEditorEvent(GESTURE_EDITOR_EVENTS.navigate, {
    replace,
    route
  })
}

function getScopeRoute(scopeKind, scopeName) {
  if (scopeKind === SCOPE_KINDS.category) {
    return scopeName ? { name: 'category-scope', params: { name: scopeName } } : { name: 'category-overview' }
  }

  if (scopeKind === SCOPE_KINDS.app) {
    return scopeName ? { name: 'app-scope', params: { name: scopeName } } : { name: 'app-overview' }
  }

  return { name: 'global' }
}
