import { computed, onBeforeUnmount, ref, watch } from 'vue'

const systemThemeQuery = window.matchMedia('(prefers-color-scheme: dark)')

export function useAppearanceTheme(editor) {
  const systemPrefersDark = ref(systemThemeQuery.matches)
  const isDarkTheme = computed(() => {
    const theme = editor.state.uiSettings.appearance?.theme ?? 'system'
    return theme === 'dark' || (theme === 'system' && systemPrefersDark.value)
  })

  function updateSystemTheme(event) {
    systemPrefersDark.value = event.matches
  }

  function toggleTheme() {
    const settings = editor.getUiSettingsSnapshot()
    settings.appearance.theme = isDarkTheme.value ? 'light' : 'dark'
    editor.saveUiSettings(settings)
  }

  systemThemeQuery.addEventListener('change', updateSystemTheme)

  watch(
    isDarkTheme,
    isDark => {
      document.documentElement.dataset.theme = isDark ? 'dark' : 'light'
    },
    { immediate: true }
  )

  onBeforeUnmount(() => {
    systemThemeQuery.removeEventListener('change', updateSystemTheme)
  })

  return { isDarkTheme, toggleTheme }
}
