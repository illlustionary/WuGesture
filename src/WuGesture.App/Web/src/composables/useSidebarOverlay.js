import { computed, onBeforeUnmount, ref, watch } from 'vue'

const HIDE_DELAY_MS = 180

export function useSidebarOverlay(editor) {
  const isCollapsed = ref(Boolean(editor.state.uiSettings.sidebar?.collapsed))
  const isInLayout = ref(true)
  const isCollapsing = ref(false)
  const isOverlayMounted = ref(false)
  const isOverlayVisible = ref(false)
  const isSidebarVisible = computed(() => isInLayout.value || isCollapsing.value || isOverlayMounted.value)
  const isSidebarHidden = computed(() => !isSidebarVisible.value)
  let hideOverlayTimer = 0
  let showOverlayFrame = 0

  watch(
    () => editor.state.uiSettings.sidebar?.collapsed,
    collapsed => {
      isCollapsed.value = Boolean(collapsed)
    }
  )

  function clearHideOverlayTimer() {
    if (!hideOverlayTimer) {
      return
    }

    window.clearTimeout(hideOverlayTimer)
    hideOverlayTimer = 0
  }

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

    clearHideOverlayTimer()
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

  function queueHideOverlay() {
    if (!isCollapsed.value || !isOverlayMounted.value) {
      return
    }

    clearHideOverlayTimer()
    hideOverlayTimer = window.setTimeout(() => {
      isOverlayVisible.value = false
      hideOverlayTimer = 0
    }, HIDE_DELAY_MS)
  }

  function toggleSidebar() {
    clearHideOverlayTimer()
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
    clearHideOverlayTimer()
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
    queueHideOverlay,
    toggleSidebar,
    completeTransition
  }
}
