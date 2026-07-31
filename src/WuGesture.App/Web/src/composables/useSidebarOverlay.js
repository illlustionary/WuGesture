import { computed, onBeforeUnmount, ref } from 'vue'

const HIDE_DELAY_MS = 180

export function useSidebarOverlay() {
  const isCollapsed = ref(false)
  const isOverlayVisible = ref(false)
  const isSidebarVisible = computed(() => !isCollapsed.value || isOverlayVisible.value)
  const isSidebarHidden = computed(() => !isSidebarVisible.value)
  let hideOverlayTimer = 0

  function clearHideOverlayTimer() {
    if (!hideOverlayTimer) {
      return
    }

    window.clearTimeout(hideOverlayTimer)
    hideOverlayTimer = 0
  }

  function showOverlay() {
    if (!isCollapsed.value) {
      return
    }

    clearHideOverlayTimer()
    isOverlayVisible.value = true
  }

  function queueHideOverlay() {
    if (!isCollapsed.value) {
      return
    }

    clearHideOverlayTimer()
    hideOverlayTimer = window.setTimeout(() => {
      isOverlayVisible.value = false
      hideOverlayTimer = 0
    }, HIDE_DELAY_MS)
  }

  function toggleSidebar() {
    isCollapsed.value = !isCollapsed.value
    isOverlayVisible.value = false
    clearHideOverlayTimer()
  }

  onBeforeUnmount(clearHideOverlayTimer)

  return {
    isCollapsed,
    isOverlayVisible,
    isSidebarVisible,
    isSidebarHidden,
    showOverlay,
    queueHideOverlay,
    toggleSidebar
  }
}
