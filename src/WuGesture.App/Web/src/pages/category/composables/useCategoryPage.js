import { ref } from 'vue'
import BriefcaseIcon from '@/assets/briefcase.svg'
import BrowserIcon from '@/assets/browser.svg'
import CircleDashedIcon from '@/assets/circle-dashed.svg'
import CodeIcon from '@/assets/code.svg'
import FolderIcon from '@/assets/folder.svg'
import MediaIcon from '@/assets/media.svg'
import SparkleIcon from '@/assets/sparkle.svg'
import { SCOPE_KINDS } from '@/constants/gestureEditorOptions'

export function useCategoryPage(editor) {
  const scopeKind = SCOPE_KINDS.category
  editor.setActiveScope(scopeKind)

  const categoryDraft = ref('')
  const categoryDialogOpen = ref(false)
  const categoryRenameDraft = ref('')
  const categoryRenameDialogOpen = ref(false)
  const categoryRenameSource = ref('')

  function openCategoryDialog() {
    categoryDraft.value = ''
    categoryDialogOpen.value = true
  }

  function closeCategoryDialog() {
    categoryDialogOpen.value = false
  }

  function confirmCategoryDialog() {
    if (editor.createScopeTarget(scopeKind, categoryDraft.value)) {
      closeCategoryDialog()
    }
  }

  function openCategoryRenameDialog(item) {
    const name = String(item?.name ?? '').trim()
    if (!name) {
      return
    }

    editor.selectScope(scopeKind, name)
    categoryRenameSource.value = name
    categoryRenameDraft.value = name
    categoryRenameDialogOpen.value = true
  }

  function closeCategoryRenameDialog() {
    categoryRenameDialogOpen.value = false
    categoryRenameDraft.value = ''
    categoryRenameSource.value = ''
  }

  function confirmCategoryRenameDialog() {
    const nextName = String(categoryRenameDraft.value ?? '').trim()
    if (!nextName) {
      return
    }

    if (nextName === categoryRenameSource.value) {
      closeCategoryRenameDialog()
      return
    }

    if (editor.renameSelectedScope(scopeKind, nextName, categoryRenameSource.value)) {
      closeCategoryRenameDialog()
    }
  }

  function getCategoryIcon(name) {
    const value = String(name ?? '')
      .trim()
      .toLowerCase()
    if (!value) {
      return CircleDashedIcon
    }

    if (
      value.includes('浏览') ||
      value.includes('browser') ||
      value.includes('网页')
    ) {
      return BrowserIcon
    }
    if (value.includes('办公') || value.includes('office')) {
      return BriefcaseIcon
    }
    if (
      value.includes('开发') ||
      value.includes('dev') ||
      value.includes('编程')
    ) {
      return CodeIcon
    }
    if (value.includes('设计') || value.includes('创作')) {
      return SparkleIcon
    }
    if (
      value.includes('媒体') ||
      value.includes('音乐') ||
      value.includes('视频')
    ) {
      return MediaIcon
    }

    return FolderIcon
  }

  function deleteCategoryItem(name) {
    editor.selectScope(scopeKind, name)
    editor.deleteSelectedScope(scopeKind)
  }

  return {
    scopeKind,
    categoryDraft,
    categoryDialogOpen,
    categoryRenameDraft,
    categoryRenameDialogOpen,
    openCategoryDialog,
    closeCategoryDialog,
    confirmCategoryDialog,
    openCategoryRenameDialog,
    closeCategoryRenameDialog,
    confirmCategoryRenameDialog,
    getCategoryIcon,
    deleteCategoryItem
  }
}
