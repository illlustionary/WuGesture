import { computed, onBeforeUnmount, reactive, watch } from 'vue'
import { CLOSE_BUTTON_BEHAVIORS } from '@/constants/gestureEditorOptions'

export function useUiSettingsDraft(editor) {
  const draft = reactive(createDraft(editor.getUiSettingsSnapshot()))
  let persistTimer = 0
  let hasPendingPersist = false
  let lastLocalPersistAt = 0

  const trailPreviewStyle = computed(() => ({
    opacity: draft.mouseTrail.enabled ? 1 : 0.42,
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
    opacity: draft.gestureHint.enabled ? 1 : 0.42,
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

  const levelOsdPreviewStyle = computed(() => {
    const width = Number(draft.levelOsd.width) || 210
    const height = Number(draft.levelOsd.height) || 190
    const scale = Math.min(1, 300 / width, 150 / height)

    return {
      opacity: draft.levelOsd.enabled ? 1 : 0.42,
      width: `${Math.max(96, Math.round(width * scale))}px`,
      height: `${Math.max(80, Math.round(height * scale))}px`,
      borderRadius: `${Math.round(
        Math.min(draft.levelOsd.cornerRadius * scale, width * scale / 2, height * scale / 2)
      )}px`,
      '--level-osd-background': hexToRgba(
        draft.levelOsd.backgroundColor,
        draft.levelOsd.backgroundOpacity / 100
      ),
      '--level-osd-text': draft.levelOsd.textColor,
      '--level-osd-track': draft.levelOsd.trackColor,
      '--level-osd-accent': draft.levelOsd.volumeColor,
      '--level-osd-offset-x': `${Math.max(-72, Math.min(72, draft.levelOsd.offsetX / 8))}px`,
      '--level-osd-offset-y': `${Math.max(-48, Math.min(48, draft.levelOsd.offsetY / 8))}px`
    }
  })

  watch(
    () => editor.uiSettings,
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
    levelOsdPreviewStyle,
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
  const levelOsd = normalizeObjectKeys(
    settings?.levelOsd ?? settings?.LevelOsd
  )
  const gestureSensitivity = normalizeObjectKeys(
    settings?.gestureSensitivity ?? settings?.GestureSensitivity
  )
  const appBehavior = normalizeObjectKeys(
    settings?.appBehavior ?? settings?.AppBehavior
  )
  const webDav = normalizeObjectKeys(settings?.webDav ?? settings?.WebDav)
  const legacyThickness = mouseTrail.thickness ?? mouseTrail.Thickness
  return {
    mouseTrail: {
      enabled: Boolean(mouseTrail.enabled ?? true),
      inactiveColor: mouseTrail.inactiveColor ?? '#AAAAAA',
      activeColor: mouseTrail.activeColor ?? '#87CEEB',
      inactiveThickness: mouseTrail.inactiveThickness ?? legacyThickness ?? 3,
      activeThickness: mouseTrail.activeThickness ?? legacyThickness ?? 3,
      thickness: legacyThickness ?? mouseTrail.inactiveThickness ?? 3,
      inactiveOpacity: mouseTrail.inactiveOpacity ?? 74,
      activeOpacity: mouseTrail.activeOpacity ?? 100
    },
    gestureHint: {
      enabled: Boolean(gestureHint.enabled ?? true),
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
    levelOsd: {
      enabled: Boolean(levelOsd.enabled ?? true),
      displayDurationMs: levelOsd.displayDurationMs ?? 1800,
      fadeDurationMs: levelOsd.fadeDurationMs ?? 240,
      backgroundColor: levelOsd.backgroundColor ?? '#28282C',
      backgroundOpacity: levelOsd.backgroundOpacity ?? 88,
      textColor: levelOsd.textColor ?? '#DCDCDC',
      trackColor: levelOsd.trackColor ?? '#464646',
      volumeColor: levelOsd.volumeColor ?? '#64C8FF',
      brightnessColor: levelOsd.brightnessColor ?? '#FFC828',
      width: levelOsd.width ?? 210,
      height: levelOsd.height ?? 190,
      cornerRadius: levelOsd.cornerRadius ?? 22,
      position: levelOsd.position ?? 'center',
      offsetX: levelOsd.offsetX ?? 0,
      offsetY: levelOsd.offsetY ?? 0
    },
    gestureSensitivity: {
      level: gestureSensitivity.level ?? 'standard'
    },
    appBehavior: {
      launchAtStartup: Boolean(appBehavior.launchAtStartup ?? false),
      runAsAdministrator: Boolean(appBehavior.runAsAdministrator ?? false),
      closeButtonBehavior: normalizeCloseButtonBehavior(
        appBehavior.closeButtonBehavior
      ),
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
  return Object.values(CLOSE_BUTTON_BEHAVIORS).includes(value)
    ? value
    : CLOSE_BUTTON_BEHAVIORS.minimizeToTray
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
    icon: String(application.icon ?? '').trim(),
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
