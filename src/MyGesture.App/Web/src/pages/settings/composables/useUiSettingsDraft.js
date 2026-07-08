import { computed, onBeforeUnmount, reactive, watch } from 'vue'

export function useUiSettingsDraft(editor) {
  const draft = reactive(createDraft(editor.getUiSettingsSnapshot()))
  let persistTimer = 0
  let hasPendingPersist = false
  let lastLocalPersistAt = 0

  const trailPreviewStyle = computed(() => ({
    '--trail-inactive-color': draft.mouseTrail.inactiveColor,
    '--trail-inactive-stroke': hexToRgba(
      draft.mouseTrail.inactiveColor,
      draft.mouseTrail.inactiveOpacity / 100
    ),
    '--trail-inactive-width': `${draft.mouseTrail.inactiveThickness}px`,
    '--trail-active-color': draft.mouseTrail.activeColor,
    '--trail-active-stroke': hexToRgba(
      draft.mouseTrail.activeColor,
      draft.mouseTrail.activeOpacity / 100
    ),
    '--trail-active-width': `${draft.mouseTrail.activeThickness}px`
  }))

  const hintPreviewStyle = computed(() => ({
    width: draft.gestureHint.autoWidth
      ? 'fit-content'
      : `${draft.gestureHint.widthPercent}%`,
    maxWidth: '100%',
    height: `${Math.max(80, draft.gestureHint.heightPercent * 3)}px`,
    '--hint-color': draft.gestureHint.textColor,
    '--hint-background-rgba': hexToRgba(
      draft.gestureHint.backgroundColor,
      draft.gestureHint.backgroundOpacity / 100
    ),
    '--hint-muted-color': hexToRgba(draft.gestureHint.textColor, 0.72),
    '--hint-font-size': `${draft.gestureHint.fontSize}px`,
    '--hint-radius': `${draft.gestureHint.cornerRadius}px`,
    '--hint-bottom-offset': `${Math.min(80, draft.gestureHint.bottomOffsetPercent * 0.8)}px`
  }))

  watch(
    () => editor.state.uiSettings,
    () => {
      const nextDraft = createDraft(editor.getUiSettingsSnapshot())
      if (
        hasPendingPersist ||
        persistTimer ||
        Date.now() - lastLocalPersistAt < 600
      ) {
        if (
          getExclusionSignature(draft.appBehavior.excludedApplications) !==
          getExclusionSignature(nextDraft.appBehavior.excludedApplications)
        ) {
          draft.appBehavior.excludedApplications =
            nextDraft.appBehavior.excludedApplications
        }
        return
      }

      Object.assign(draft, nextDraft)
    },
    { deep: true, immediate: true }
  )

  function persistDraft() {
    lastLocalPersistAt = Date.now()
    editor.saveUiSettings(createDraft(draft))
  }

  function queuePersistDraft() {
    hasPendingPersist = true
    if (persistTimer) {
      clearTimeout(persistTimer)
    }

    persistTimer = window.setTimeout(() => {
      persistTimer = 0
      flushPersistDraft()
    }, 250)
  }

  function flushPersistDraft() {
    if (persistTimer) {
      clearTimeout(persistTimer)
      persistTimer = 0
    }

    if (!hasPendingPersist) {
      return
    }

    hasPendingPersist = false
    persistDraft()
  }

  function resetSettings() {
    if (persistTimer) {
      clearTimeout(persistTimer)
      persistTimer = 0
    }
    hasPendingPersist = false
    lastLocalPersistAt = 0
    editor.resetUiSettings()
    Object.assign(draft, createDraft(editor.getUiSettingsSnapshot()))
  }

  onBeforeUnmount(() => {
    if (hasPendingPersist) {
      flushPersistDraft()
    }
  })

  return {
    draft,
    trailPreviewStyle,
    hintPreviewStyle,
    queuePersistDraft,
    flushPersistDraft,
    resetSettings
  }
}

function createDraft(settings) {
  const mouseTrail = normalizeObjectKeys(
    settings?.mouseTrail ?? settings?.MouseTrail
  )
  const gestureHint = normalizeObjectKeys(
    settings?.gestureHint ?? settings?.GestureHint
  )
  const appBehavior = normalizeObjectKeys(
    settings?.appBehavior ?? settings?.AppBehavior
  )
  const webDav = normalizeObjectKeys(settings?.webDav ?? settings?.WebDav)
  const legacyThickness = mouseTrail.thickness ?? mouseTrail.Thickness
  return {
    mouseTrail: {
      inactiveColor: mouseTrail.inactiveColor ?? '#AAAAAA',
      activeColor: mouseTrail.activeColor ?? '#87CEEB',
      inactiveThickness: mouseTrail.inactiveThickness ?? legacyThickness ?? 3,
      activeThickness: mouseTrail.activeThickness ?? legacyThickness ?? 3,
      thickness: legacyThickness ?? mouseTrail.inactiveThickness ?? 3,
      inactiveOpacity: mouseTrail.inactiveOpacity ?? 74,
      activeOpacity: mouseTrail.activeOpacity ?? 100
    },
    gestureHint: {
      fontFamily: gestureHint.fontFamily ?? 'Segoe UI Semibold',
      fontSize: gestureHint.fontSize ?? 22,
      textColor: gestureHint.textColor ?? '#FFFFFF',
      backgroundColor: gestureHint.backgroundColor ?? '#12181F',
      backgroundOpacity: gestureHint.backgroundOpacity ?? 90,
      width: gestureHint.width ?? 540,
      widthPercent: gestureHint.widthPercent ?? 28,
      autoWidth: Boolean(gestureHint.autoWidth ?? true),
      height: gestureHint.height ?? 120,
      heightPercent: gestureHint.heightPercent ?? 11,
      cornerRadius: gestureHint.cornerRadius ?? 28,
      bottomOffset: gestureHint.bottomOffset ?? 140,
      bottomOffsetPercent: gestureHint.bottomOffsetPercent ?? 13
    },
    appBehavior: {
      launchAtStartup: Boolean(appBehavior.launchAtStartup ?? false),
      runAsAdministrator: Boolean(appBehavior.runAsAdministrator ?? false),
      closeButtonBehavior: normalizeCloseButtonBehavior(
        appBehavior.closeButtonBehavior
      ),
      gesturePaused: Boolean(appBehavior.gesturePaused ?? false),
      excludedApplications: normalizeExcludedApplications(
        appBehavior.excludedApplications
      )
    },
    webDav: {
      address: webDav.address ?? '',
      userName: webDav.userName ?? '',
      password: webDav.password ?? '',
      remotePath: webDav.remotePath ?? ''
    }
  }
}

function normalizeCloseButtonBehavior(value) {
  return ['minimize-to-tray', 'minimize-to-taskbar', 'exit'].includes(value)
    ? value
    : 'minimize-to-tray'
}

function normalizeExcludedApplications(applications) {
  const normalized = []
  for (const application of Array.isArray(applications) ? applications : []) {
    const exclusion = normalizeExcludedApplication(application)
    if (!exclusion.name && !exclusion.path) {
      continue
    }

    if (!normalized.some(item => isSameApplicationIdentity(item, exclusion))) {
      normalized.push(exclusion)
    }
  }

  return normalized
}

function normalizeExcludedApplication(application) {
  application = normalizeObjectKeys(application)
  const name = String(application.name ?? '').trim()
  const path = String(application.path ?? '').trim()
  return {
    name,
    displayName: String(application.displayName ?? name).trim() || name || path,
    path,
    disableEdgeActions: Boolean(application.disableEdgeActions ?? false)
  }
}

function isSameApplicationIdentity(left, right) {
  const leftPath = String(left.path ?? '').trim().toLowerCase()
  const rightPath = String(right.path ?? '').trim().toLowerCase()
  if (leftPath && rightPath) {
    return leftPath === rightPath
  }

  return (
    String(left.name ?? '').trim().toLowerCase() ===
    String(right.name ?? '').trim().toLowerCase()
  )
}

function getExclusionSignature(applications) {
  return JSON.stringify(
    (Array.isArray(applications) ? applications : []).map(application => ({
      name: String(application.name ?? '').trim().toLowerCase(),
      path: String(application.path ?? '').trim().toLowerCase(),
      disableEdgeActions: Boolean(application.disableEdgeActions)
    }))
  )
}

function normalizeObjectKeys(source) {
  if (!source || typeof source !== 'object') {
    return {}
  }

  const normalized = {}
  for (const [key, value] of Object.entries(source)) {
    normalized[key.charAt(0).toLowerCase() + key.slice(1)] = value
  }

  return normalized
}

function hexToRgba(hex, alpha = 1) {
  const normalized = String(hex ?? '')
    .trim()
    .replace('#', '')
  const expanded =
    normalized.length === 3
      ? normalized
          .split('')
          .map(char => `${char}${char}`)
          .join('')
      : normalized

  if (!/^[0-9a-fA-F]{6}$/.test(expanded)) {
    return `rgba(0, 0, 0, ${alpha})`
  }

  const red = Number.parseInt(expanded.slice(0, 2), 16)
  const green = Number.parseInt(expanded.slice(2, 4), 16)
  const blue = Number.parseInt(expanded.slice(4, 6), 16)
  return `rgba(${red}, ${green}, ${blue}, ${Math.max(0, Math.min(1, alpha))})`
}
