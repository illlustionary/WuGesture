import { computed, nextTick, onBeforeUnmount, onMounted, proxyRefs, ref } from 'vue'
import { SCOPE_KINDS } from '@/constants/gestureEditorOptions'
import { useGestureQuickSearchStore } from '@/gestureEditor/stores/useGestureQuickSearchStore'

export function useQuickSearch({ navigation }) {
  const quickSearchStore = useGestureQuickSearchStore()
  const isOpen = ref(false)
  const searchItems = computed(() => quickSearchStore.searchItems)

  function open() {
    isOpen.value = true
  }

  function close() {
    isOpen.value = false
  }

  async function select(item) {
    close()

    if (item.type === 'rule') {
      if (item.scopeKind !== SCOPE_KINDS.global) {
        quickSearchStore.setActiveScope(item.scopeKind)
        quickSearchStore.selectScope(item.scopeKind, item.scopeName)
      }
      await navigation.navigateToScope(item.scopeKind, item.scopeName)
      await nextTick()
      quickSearchStore.openEditRule(item.target)
      return
    }

    if (item.type === 'category') {
      quickSearchStore.setActiveScope(SCOPE_KINDS.category)
      quickSearchStore.selectScope(SCOPE_KINDS.category, item.target)
      await navigation.navigateToScope(SCOPE_KINDS.category, item.target)
      return
    }

    if (item.type === 'app') {
      quickSearchStore.setActiveScope(SCOPE_KINDS.app)
      quickSearchStore.selectScope(SCOPE_KINDS.app, item.target)
      await navigation.navigateToScope(SCOPE_KINDS.app, item.target)
      return
    }

    await navigation.navigateToExclusion(item.target)
  }

  function handleGlobalKeydown(event) {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault()
      open()
    }
  }

  onMounted(() => window.addEventListener('keydown', handleGlobalKeydown))
  onBeforeUnmount(() => window.removeEventListener('keydown', handleGlobalKeydown))

  return proxyRefs({
    isOpen,
    searchItems,
    open,
    close,
    select
  })
}
