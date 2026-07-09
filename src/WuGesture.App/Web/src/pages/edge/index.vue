<script setup>
import { computed } from 'vue'
import AppShell from '../../components/AppShell.vue'
import { useGestureEdgeActionsStore } from '../../composables/gestureEditor/useGestureEdgeActionsStore'
import EdgeActionDialog from './components/EdgeActionDialog.vue'
import EdgeActionSection from './components/EdgeActionSection.vue'
import { useEdgeActionDraft } from './composables/useEdgeActionDraft'
import {
  ACTION_TYPES,
  EDGE_TRIGGER_TYPES,
  OPERATIONS,
  WHEEL_DIRECTIONS
} from '../../constants/gestureEditorOptions'

const edgeActionsStore = useGestureEdgeActionsStore()
const { draft, editingAction, openEditor, closeEditor } =
  useEdgeActionDraft(edgeActionsStore)

const groups = [
  {
    type: EDGE_TRIGGER_TYPES.corner,
    title: '触发角',
    description: '鼠标进入角落时触发一次，离开后再次进入才会再次触发。'
  },
  {
    type: EDGE_TRIGGER_TYPES.friction,
    title: '摩擦边',
    description: '贴近屏幕边缘后沿边反复移动，达到次数后触发。'
  },
  {
    type: EDGE_TRIGGER_TYPES.wheel,
    title: '边缘滚动',
    description: '鼠标停在屏幕边缘滚动时触发，并吞掉原始滚轮事件。'
  }
]

const triggerLabels = {
  [EDGE_TRIGGER_TYPES.corner]: '触发角',
  [EDGE_TRIGGER_TYPES.friction]: '摩擦边',
  [EDGE_TRIGGER_TYPES.wheel]: '边缘滚动'
}

const dialogTitle = computed(() => {
  const action = editingAction.value
  if (!action) {
    return '边缘操作'
  }

  return [
    locationLabel(action),
    wheelLabel(action) || triggerLabels[action.triggerType]
  ]
    .filter(Boolean)
    .join(' · ')
})

function groupActions(type) {
  return edgeActionsStore.edgeActions.filter(action => action.triggerType === type)
}

function locationLabel(action) {
  const locations =
    action.triggerType === EDGE_TRIGGER_TYPES.corner
      ? edgeActionsStore.edgeLocations.corner
      : edgeActionsStore.edgeLocations.edge
  return (
    locations.find(item => item.value === action.location)?.label ??
    action.location
  )
}

function wheelLabel(action) {
  if (action.triggerType !== EDGE_TRIGGER_TYPES.wheel) {
    return ''
  }

  return action.wheelDirection === WHEEL_DIRECTIONS.down ? '滚轮下' : '滚轮上'
}

function operationOptions(action) {
  if (action.actionType === ACTION_TYPES.volume) {
    return edgeActionsStore.volumeOperations
  }

  if (action.actionType === ACTION_TYPES.brightness) {
    return edgeActionsStore.brightnessOperations
  }

  return edgeActionsStore.windowOperations
}

function operationModel(action) {
  if (action.actionType === ACTION_TYPES.volume) {
    return action.volumeOperation
  }

  if (action.actionType === ACTION_TYPES.brightness) {
    return action.brightnessOperation
  }

  return action.windowOperation
}

function actionSummary(action) {
  if (action.actionType === ACTION_TYPES.window) {
    const operation = edgeActionsStore.windowOperations.find(
      item => item.value === action.windowOperation
    )
    return operation?.label ?? '窗口控制'
  }

  if (action.actionType === ACTION_TYPES.volume) {
    const operation = edgeActionsStore.volumeOperations.find(
      item => item.value === action.volumeOperation
    )
    return action.volumeOperation === OPERATIONS.mute
      ? (operation?.label ?? '静音')
      : `${operation?.label ?? '音量 +'} ${action.amount}`
  }

  if (action.actionType === ACTION_TYPES.brightness) {
    const operation = edgeActionsStore.brightnessOperations.find(
      item => item.value === action.brightnessOperation
    )
    return `${operation?.label ?? '亮度 +'} ${action.amount}`
  }

  return action.keysText || '未设置快捷键'
}

function updateDraftOperation(value) {
  if (draft.actionType === ACTION_TYPES.volume) {
    draft.volumeOperation = value
    return
  }

  if (draft.actionType === ACTION_TYPES.brightness) {
    draft.brightnessOperation = value
    return
  }

  draft.windowOperation = value
}

function recordHotkey() {
  edgeActionsStore.startRecording(draft)
}
</script>

<template>
  <AppShell
    title="边缘操作"
    description="配置屏幕角落、四边摩擦和边缘滚轮触发的动作。"
    layout-class="page-shell__grid--single"
  >
    <template #right>
      <div class="page-stack">
        <EdgeActionSection
          v-for="group in groups"
          :key="group.type"
          :group="group"
          :actions="groupActions(group.type)"
          :trigger-labels="triggerLabels"
          :location-label="locationLabel"
          :wheel-label="wheelLabel"
          :action-summary="actionSummary"
          :get-edge-action-label="edgeActionsStore.getEdgeActionLabel"
          @open="openEditor"
        />
      </div>
    </template>
  </AppShell>

  <EdgeActionDialog
    v-if="editingAction"
    :action="editingAction"
    :draft="draft"
    :is-recording-hotkey="edgeActionsStore.isRecordingHotkey"
    :title="dialogTitle"
    :operation-options="operationOptions"
    :operation-model="operationModel"
    @close="closeEditor"
    @record-hotkey="recordHotkey"
    @update-operation="updateDraftOperation"
  />
</template>
