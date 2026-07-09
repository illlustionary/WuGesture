import { computed, reactive, ref } from 'vue'
import {
  ACTION_TYPES,
  EDGE_TRIGGER_TYPES,
  OPERATIONS
} from '../../../constants/gestureEditorOptions'

export function useEdgeActionDraft(edgeActionsStore) {
  const editingKey = ref('')
  const draft = reactive(createEmptyDraft())

  const editingAction = computed(
    () =>
      edgeActionsStore.edgeActions.find(action => actionKey(action) === editingKey.value) ??
      null
  )

  function openEditor(action) {
    editingKey.value = actionKey(action)
    Object.assign(draft, { ...action })
  }

  function closeEditor() {
    persistEditor()
    editingKey.value = ''
  }

  function persistEditor() {
    const action = editingAction.value
    if (!action) {
      return
    }

    edgeActionsStore.updateEdgeAction(action, toCommitPatch(draft), {
      notifyResult: false,
      notifyPreview: false
    })
  }

  return {
    draft,
    editingAction,
    openEditor,
    closeEditor
  }
}

export function actionKey(action) {
  return `${action.triggerType}:${action.location}:${action.wheelDirection || ''}`
}

function createEmptyDraft() {
  return {
    enabled: false,
    triggerType: EDGE_TRIGGER_TYPES.corner,
    location: 'top-left',
    wheelDirection: '',
    frictionCount: 4,
    keysText: '',
    actionType: ACTION_TYPES.hotkey,
    windowOperation: OPERATIONS.toggleMaximize,
    volumeOperation: OPERATIONS.increase,
    brightnessOperation: OPERATIONS.increase,
    amount: 5
  }
}

function toCommitPatch(action) {
  return {
    enabled: action.enabled,
    frictionCount: action.frictionCount,
    actionType: action.actionType,
    keysText: action.keysText,
    windowOperation: action.windowOperation,
    volumeOperation: action.volumeOperation,
    brightnessOperation: action.brightnessOperation,
    amount: action.amount
  }
}
