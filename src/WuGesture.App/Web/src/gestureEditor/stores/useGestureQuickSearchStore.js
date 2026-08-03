import { computed, proxyRefs } from 'vue'
import { SCOPE_KINDS } from '@/constants/gestureEditorOptions'
import { getActionLabel, getGestureMnemonic } from '@/utils/gestureEditorFormatters'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'

export function useGestureQuickSearchStore() {
  const editor = useGestureEditorContext()

  const searchItems = computed(() => {
    const rules = editor.state.rules.map(rule => ({
      id: `rule:${rule.id}`,
      type: 'rule',
      group: '手势规则',
      title: rule.actionName || getActionLabel(rule),
      detail: [getScopeLabel(rule.scopeKind, rule.scopeName), getGestureMnemonic(rule), getActionLabel(rule)]
        .filter(Boolean)
        .join(' · '),
      searchText: [
        rule.actionName,
        rule.patternText,
        getGestureMnemonic(rule),
        getActionLabel(rule),
        rule.scopeName,
        getScopeLabel(rule.scopeKind, rule.scopeName)
      ].join(' '),
      target: rule.id,
      scopeKind: rule.scopeKind,
      scopeName: rule.scopeName
    }))
    const categories = editor.categoryItems.map(item => ({
      id: `category:${item.name}`,
      type: 'category',
      group: '分类',
      title: item.name,
      detail: '分类规则',
      searchText: item.name,
      target: item.name
    }))
    const applications = editor.state.applications.map(application => ({
      id: `app:${application.name}`,
      type: 'app',
      group: '程序',
      title: application.displayName || application.name || application.path,
      detail: application.path || application.name,
      searchText: [application.displayName, application.name, application.path].join(' '),
      target: application.name
    }))
    const exclusions = editor.state.uiSettings.appBehavior.excludedApplications.map((application, index) => ({
      id: `exclusion:${getApplicationKey(application, index)}`,
      type: 'exclusion',
      group: '排除项',
      title: application.displayName || application.name || application.path,
      detail: application.path || application.name,
      searchText: [application.displayName, application.name, application.path].join(' '),
      target: getApplicationKey(application, index)
    }))

    return [...rules, ...categories, ...applications, ...exclusions]
  })

  return proxyRefs({
    searchItems,
    openEditRule: editor.openEditRule,
    selectScope: editor.selectScope,
    setActiveScope: editor.setActiveScope
  })
}

export function getApplicationKey(application, index = 0) {
  return String(application.path || application.name || index)
}

function getScopeLabel(scopeKind, scopeName) {
  if (scopeKind === SCOPE_KINDS.category) {
    return `分类：${scopeName}`
  }

  if (scopeKind === SCOPE_KINDS.app) {
    return `程序：${scopeName}`
  }

  return '全局'
}
