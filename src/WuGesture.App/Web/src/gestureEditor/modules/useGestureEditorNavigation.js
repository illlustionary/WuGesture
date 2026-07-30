import { SCOPE_KINDS } from '@/constants/gestureEditorOptions'

export function useGestureEditorNavigation({ router }) {
  function getRouteGroup(path = '') {
    return String(path).replace(/^\//, '').split('/')[0] || SCOPE_KINDS.global
  }

  function navigateToScope(scopeKind, scopeName = '') {
    return router.push(getScopeRoute(scopeKind, scopeName))
  }

  function replaceScope(scopeKind, scopeName = '') {
    return router.replace(getScopeRoute(scopeKind, scopeName))
  }

  function navigateToExclusion(selected = '') {
    return router.push({
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

function getScopeRoute(scopeKind, scopeName) {
  if (scopeKind === SCOPE_KINDS.category) {
    return scopeName
      ? { name: 'category-scope', params: { name: scopeName } }
      : { name: 'category-overview' }
  }

  if (scopeKind === SCOPE_KINDS.app) {
    return scopeName
      ? { name: 'app-scope', params: { name: scopeName } }
      : { name: 'app-overview' }
  }

  return { name: 'global' }
}
