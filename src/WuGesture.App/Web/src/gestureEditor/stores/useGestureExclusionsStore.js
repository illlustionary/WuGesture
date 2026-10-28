import { computed, proxyRefs } from 'vue'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import { isSameApplicationIdentity } from '@/utils/gestureEditorNormalizers'

export function useGestureExclusionsStore() {
  const editor = useGestureEditorContext()
  const applications = computed(() => editor.state.applications)
  const excludedApplications = computed(() => editor.state.uiSettings.appBehavior.excludedApplications)
  const exclusionsWithIcons = computed(() =>
    excludedApplications.value.map(application => {
      const matchedApplication = applications.value.find(item => isSameApplicationIdentity(application, item))

      return {
        application,
        icon: application.icon || matchedApplication?.icon || '',
        fallbackGlyph: (application.displayName || application.name || application.path || '?')
          .slice(0, 1)
          .toUpperCase()
      }
    })
  )

  return proxyRefs({
    applications,
    excludedApplications,
    exclusionsWithIcons,
    hasExclusions: computed(() => excludedApplications.value.length > 0),
    openExcludedApplicationPicker: editor.openExcludedApplicationPicker,
    removeExcludedApplication: editor.removeExcludedApplication,
    updateExcludedApplication: editor.updateExcludedApplication
  })
}
