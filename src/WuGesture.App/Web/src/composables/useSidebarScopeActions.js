import { ref, watch } from 'vue'
import { useAppPage } from '@/pages/app/composables/useAppPage'
import { useCategoryPage } from '@/pages/category/composables/useCategoryPage'
import { useGestureEditorNavigation } from '@/gestureEditor/modules/useGestureEditorNavigation'

export function useSidebarScopeActions({ editor, route }) {
  const pendingAppRoute = ref(false)
  const navigation = useGestureEditorNavigation()
  const category = useCategoryPage(editor, { activate: false })
  const application = useAppPage(editor, { activate: false })

  watch(
    () => [editor.getSelectedName('app'), editor.appItems.map(item => item.name).join('\u0000')],
    ([selectedApp]) => {
      if (!pendingAppRoute.value || !selectedApp) {
        return
      }

      pendingAppRoute.value = false
      navigation.navigateToScope('app', selectedApp)
    }
  )

  function confirmSidebarCategory() {
    if (category.confirmCategoryDialog()) {
      navigation.navigateToScope('category', editor.getSelectedName('category'))
    }
  }

  function confirmSidebarCategoryRename() {
    if (category.confirmCategoryRenameDialog()) {
      navigation.navigateToScope('category', editor.getSelectedName('category'))
    }
  }

  function confirmSidebarAppRename() {
    if (application.confirmAppRenameDialog()) {
      navigation.navigateToScope('app', editor.getSelectedName('app'))
    }
  }

  function removeSidebarCategory(name) {
    const currentName = String(route.params.name ?? '')
    const isCurrent = route.name === 'category-scope' && currentName === name
    category.deleteCategoryItem(name)

    if (isCurrent) {
      navigation.navigateToScope('category', editor.getSelectedName('category'))
    } else if (currentName) {
      editor.selectScope('category', currentName)
    }
  }

  function removeSidebarApp(name) {
    const currentName = String(route.params.name ?? '')
    const isCurrent = route.name === 'app-scope' && currentName === name
    application.deleteAppItem(name)

    if (isCurrent) {
      navigation.navigateToScope('app', editor.getSelectedName('app'))
    } else if (currentName) {
      editor.selectScope('app', currentName)
    }
  }

  function openSidebarAppPicker() {
    pendingAppRoute.value = true
    editor.openApplicationPicker('', 'app')
  }

  return {
    ...category,
    ...application,
    confirmSidebarAppRename,
    confirmSidebarCategory,
    confirmSidebarCategoryRename,
    openSidebarAppPicker,
    removeSidebarApp,
    removeSidebarCategory
  }
}
