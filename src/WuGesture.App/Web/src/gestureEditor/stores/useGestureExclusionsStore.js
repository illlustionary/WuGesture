import { computed, proxyRefs } from "vue";
import { useGestureEditorContext } from "@/gestureEditor/context/gestureEditorContext";

export function useGestureExclusionsStore() {
  const editor = useGestureEditorContext();
  const applications = computed(() => editor.state.applications);
  const excludedApplications = computed(
    () => editor.state.uiSettings.appBehavior.excludedApplications
  );
  const exclusionsWithIcons = computed(() =>
    excludedApplications.value.map((application) => {
      const matchedApplication = applications.value.find((item) => {
        const applicationPath = String(application.path ?? "")
          .trim()
          .toLowerCase();
        const itemPath = String(item.path ?? "")
          .trim()
          .toLowerCase();
        if (applicationPath && itemPath) {
          return applicationPath === itemPath;
        }

        return (
          String(item.name ?? "")
            .trim()
            .toLowerCase() ===
          String(application.name ?? "")
            .trim()
            .toLowerCase()
        );
      });

      return {
        application,
        icon: application.icon || matchedApplication?.icon || "",
        fallbackGlyph: (
          application.displayName ||
          application.name ||
          application.path ||
          "?"
        )
          .slice(0, 1)
          .toUpperCase()
      };
    })
  );

  return proxyRefs({
    applications,
    excludedApplications,
    exclusionsWithIcons,
    hasExclusions: computed(() => excludedApplications.value.length > 0),
    openExcludedApplicationPicker: editor.openExcludedApplicationPicker,
    removeExcludedApplication: editor.removeExcludedApplication,
    updateExcludedApplication: editor.updateExcludedApplication
  });
}
