import { ref } from 'vue'
import { SCOPE_KINDS } from '@/constants/gestureEditorOptions'

export function useAppPage(editor, options = {}) {
  const scopeKind = SCOPE_KINDS.app
  if (options.activate !== false) {
    editor.setActiveScope(scopeKind)
  }

  const appRenameDialogOpen = ref(false)
  const appRenameDraft = ref('')
  const appRenameSource = ref('')

  function getAppFallbackGlyph(item) {
    const text = String(item?.displayName ?? item?.name ?? '').trim()
    if (!text) {
      return '?'
    }

    return text.slice(0, 1).toUpperCase()
  }

  function deleteAppItem(name) {
    editor.selectScope(scopeKind, name)
    editor.deleteSelectedScope(scopeKind)
  }

  function openAppRenameDialog(item) {
    const name = String(item?.name ?? '').trim()
    if (!name) {
      return
    }

    editor.selectScope(scopeKind, name)
    appRenameSource.value = name
    appRenameDraft.value = name
    appRenameDialogOpen.value = true
  }

  function closeAppRenameDialog() {
    appRenameDialogOpen.value = false
    appRenameDraft.value = ''
    appRenameSource.value = ''
  }

  function confirmAppRenameDialog() {
    const nextName = String(appRenameDraft.value ?? '').trim()
    if (!nextName) {
      return false
    }

    if (nextName === appRenameSource.value) {
      closeAppRenameDialog()
      return true
    }

    if (editor.renameSelectedScope(scopeKind, nextName, appRenameSource.value)) {
      editor.updateApplicationDisplayName(nextName, nextName)
      closeAppRenameDialog()
      return true
    }

    return false
  }

  return {
    scopeKind,
    appRenameDialogOpen,
    appRenameDraft,
    getAppFallbackGlyph,
    deleteAppItem,
    openAppRenameDialog,
    closeAppRenameDialog,
    confirmAppRenameDialog
  }
}
