import { computed, proxyRefs } from 'vue'
import { BRIGHTNESS_OPERATIONS, VOLUME_OPERATIONS, WINDOW_OPERATIONS } from '@/constants/gestureEditorOptions'
import { getGestureMnemonic } from '@/utils/gestureEditorFormatters'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'

export function useGestureEditorOverlayStore() {
  const editor = useGestureEditorContext()

  return proxyRefs({
    applicationPickerOpen: computed(() => editor.state.applicationPickerOpen),
    applicationPickerTarget: computed(() => editor.state.applicationPickerTarget),
    applicationPickerScopeKind: computed(() => editor.state.applicationPickerScopeKind),
    gestureDraft: computed(() => editor.state.gestureDraft),
    gestureEditorOpen: computed(() => editor.state.gestureEditorOpen),
    gestureRecognitionMessage: computed(() => editor.state.gestureRecognitionMessage),
    gestureRecordingActive: computed(() => editor.state.gestureRecordingActive),
    brightnessOperations: BRIGHTNESS_OPERATIONS,
    volumeOperations: VOLUME_OPERATIONS,
    windowOperations: WINDOW_OPERATIONS,
    closeApplicationPicker: editor.closeApplicationPicker,
    closeGestureEditor: editor.closeGestureEditor,
    getGestureMnemonic,
    isRecordingHotkey: editor.isRecordingHotkey,
    openApplicationFolder: editor.openApplicationFolder,
    openProgramPicker: editor.openProgramPicker,
    persistGestureEditor: editor.persistGestureEditor,
    pickApplicationWindow: editor.pickApplicationWindow,
    selectApplication: editor.selectApplication,
    startGestureRecording: editor.startGestureRecording,
    startRecording: editor.startRecording
  })
}
