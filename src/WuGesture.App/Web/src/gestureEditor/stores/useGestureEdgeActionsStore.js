import { computed, proxyRefs } from 'vue'
import {
  BRIGHTNESS_OPERATIONS,
  EDGE_LOCATIONS,
  VOLUME_OPERATIONS,
  WINDOW_OPERATIONS
} from '@/constants/gestureEditorOptions'
import { getEdgeActionLabel } from '@/utils/gestureEditorFormatters'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'

export function useGestureEdgeActionsStore() {
  const editor = useGestureEditorContext()

  return proxyRefs({
    edgeActions: computed(() => editor.state.edgeActions),
    edgeLocations: EDGE_LOCATIONS,
    windowOperations: WINDOW_OPERATIONS,
    volumeOperations: VOLUME_OPERATIONS,
    brightnessOperations: BRIGHTNESS_OPERATIONS,
    getEdgeActionLabel,
    isRecordingHotkey: editor.isRecordingHotkey,
    startRecording: editor.startRecording,
    updateEdgeAction: editor.updateEdgeAction
  })
}
