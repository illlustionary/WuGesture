import { computed, onBeforeUnmount, ref, watch } from 'vue'

export function useSidebarOverlay(editor) {
  const isCollapsed = ref(Boolean(editor.state.uiSettings.sidebar?.collapsed))
  const isInLayout = ref(true)
  const isCollapsing = ref(false)
  const isOverlayMounted = ref(false)
  const isOverlayVisible = ref(false)
  const isSidebarVisible = computed(() => isInLayout.value || isCollapsing.value || isOverlayMounted.value)
  const isSidebarHidden = computed(() => !isSidebarVisible.value)
  let showOverlayFrame = 0

  watch(
    () => editor.state.uiSettings.sidebar?.collapsed,
    collapsed => {
      isCollapsed.value = Boolean(collapsed)
    }
  )

  function clearShowOverlayFrame() {
    if (!showOverlayFrame) {
      return
    }

    window.cancelAnimationFrame(showOverlayFrame)
    showOverlayFrame = 0
  }

  function showOverlay() {
    if (!isCollapsed.value || isCollapsing.value) {
      return
    }

    if (isOverlayVisible.value) {
      return
    }

    clearShowOverlayFrame()
    isOverlayMounted.value = true
    showOverlayFrame = window.requestAnimationFrame(() => {
      isOverlayVisible.value = true
      showOverlayFrame = 0
    })
  }

  function cancelPendingOverlay() {
    if (isOverlayVisible.value) {
      return
    }

    clearShowOverlayFrame()
    isOverlayMounted.value = false
  }

  function hideOverlay() {
    if (!isCollapsed.value || !isOverlayMounted.value) {
      return
    }

    clearShowOverlayFrame()
    const wasVisible = isOverlayVisible.value
    isOverlayVisible.value = false
    if (!wasVisible) {
      isOverlayMounted.value = false
    }
  }

  function toggleSidebar() {
    clearShowOverlayFrame()

    if (isCollapsed.value) {
      setCollapsed(false)
      isCollapsing.value = false
      isOverlayVisible.value = false
      isOverlayMounted.value = false
      isInLayout.value = true
      return
    }

    setCollapsed(true)
    isCollapsing.value = true
  }

  function setCollapsed(collapsed) {
    isCollapsed.value = collapsed
    const uiSettings = editor.getUiSettingsSnapshot()
    uiSettings.sidebar.collapsed = collapsed
    editor.saveUiSettings(uiSettings)
  }

  function completeTransition() {
    if (isCollapsing.value) {
      isCollapsing.value = false
      isInLayout.value = false
      return
    }

    if (isCollapsed.value && isOverlayMounted.value && !isOverlayVisible.value) {
      isOverlayMounted.value = false
    }
  }

  onBeforeUnmount(() => {
    clearShowOverlayFrame()
  })

  return {
    isCollapsed,
    isInLayout,
    isCollapsing,
    isOverlayMounted,
    isOverlayVisible,
    isSidebarVisible,
    isSidebarHidden,
    showOverlay,
    cancelPendingOverlay,
    hideOverlay,
    toggleSidebar,
    completeTransition
  }
}
