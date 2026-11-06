import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { GESTURE_EDITOR_LIMITS } from '@/constants/gestureEditorLimits'
import { clampInteger } from '@/utils/gestureEditorNormalizers'

export function useSidebarOverlay(editor) {
  const isCollapsed = ref(Boolean(editor.state.uiSettings.sidebar?.collapsed))
  const isInLayout = ref(!isCollapsed.value)
  const isCollapsing = ref(false)
  const isOverlayMounted = ref(false)
  const isOverlayVisible = ref(false)
  const isSidebarVisible = computed(() => isInLayout.value || isCollapsing.value || isOverlayMounted.value)
  const isSidebarHidden = computed(() => !isSidebarVisible.value)
  const revealDistance = computed(() => {
    const limits = GESTURE_EDITOR_LIMITS.sidebarRevealDistance
    return clampInteger(editor.state.uiSettings.sidebar?.revealDistance, limits.min, limits.max, limits.fallback)
  })

  watch(
    () => editor.state.uiSettings.sidebar?.collapsed,
    collapsed => {
      const nextCollapsed = Boolean(collapsed)
      if (nextCollapsed === isCollapsed.value) {
        return
      }

      isCollapsed.value = nextCollapsed
      isCollapsing.value = false
      isOverlayVisible.value = false
      isOverlayMounted.value = false
      isInLayout.value = !nextCollapsed
    }
  )

  function showOverlay() {
    if (!isCollapsed.value || isCollapsing.value || isOverlayVisible.value) {
      return
    }

    // The overlay element is already laid out while detached (only visibility/opacity are
    // hidden), so both classes can be applied in the same tick: the CSS transition starts
    // from the detached styles without waiting for an extra animation frame.
    isOverlayMounted.value = true
    isOverlayVisible.value = true
  }

  // Track proximity with a window-level pointermove instead of a fixed hot zone: the pointer
  // never has to hit an exact strip, and the invisible strip no longer blocks clicks in the
  // page content once the sidebar is collapsed.
  function handlePointerMove(event) {
    if (!isCollapsed.value || isCollapsing.value || isOverlayVisible.value) {
      return
    }

    // Do not reveal the sidebar while the user is dragging or selecting inside the page.
    if (event.buttons !== 0) {
      return
    }

    if (event.clientX <= revealDistance.value) {
      showOverlay()
    }
  }

  function hideOverlay() {
    if (!isCollapsed.value || !isOverlayMounted.value) {
      return
    }

    isOverlayVisible.value = false
  }

  function toggleSidebar() {
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

  window.addEventListener('pointermove', handlePointerMove, { passive: true })

  onBeforeUnmount(() => {
    window.removeEventListener('pointermove', handlePointerMove)
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
    hideOverlay,
    toggleSidebar,
    completeTransition
  }
}
