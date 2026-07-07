<script setup>
import { computed } from 'vue'
import AppShell from '../../components/AppShell.vue'
import { useGestureEditorStore } from '../../composables/gestureEditorStore'
import EdgeActionDialog from './components/EdgeActionDialog.vue'
import EdgeActionSection from './components/EdgeActionSection.vue'
import { useEdgeActionDraft } from './composables/useEdgeActionDraft'

const editor = useGestureEditorStore()
const { draft, editingAction, openEditor, closeEditor } = useEdgeActionDraft(editor)

const groups = [
  {
    type: 'corner',
    title: '触发角',
    description: '鼠标进入角落时触发一次，离开后再次进入才会再次触发。'
  },
  {
    type: 'friction',
    title: '摩擦边',
    description: '贴近屏幕边缘后沿边反复移动，达到次数后触发。'
  },
  {
    type: 'wheel',
    title: '边缘滚动',
    description: '鼠标停在屏幕边缘滚动时触发，并吞掉原始滚轮事件。'
  }
]

const triggerLabels = {
  corner: '触发角',
  friction: '摩擦边',
  wheel: '边缘滚动'
}

const dialogTitle = computed(() => {
  const action = editingAction.value
  if (!action) {
    return '边缘操作'
  }

  return [locationLabel(action), wheelLabel(action) || triggerLabels[action.triggerType]]
    .filter(Boolean)
    .join(' · ')
})

function groupActions(type) {
  return editor.state.edgeActions.filter(action => action.triggerType === type)
}

function locationLabel(action) {
  const locations =
    action.triggerType === 'corner'
      ? editor.edgeLocations.corner
      : editor.edgeLocations.edge
  return locations.find(item => item.value === action.location)?.label ?? action.location
}

function wheelLabel(action) {
  if (action.triggerType !== 'wheel') {
    return ''
  }

  return action.wheelDirection === 'down' ? '滚轮下' : '滚轮上'
}

function operationOptions(action) {
  if (action.actionType === 'volume') {
    return editor.volumeOperations
  }

  if (action.actionType === 'brightness') {
    return editor.brightnessOperations
  }

  return editor.windowOperations
}

function operationModel(action) {
  if (action.actionType === 'volume') {
    return action.volumeOperation
  }

  if (action.actionType === 'brightness') {
    return action.brightnessOperation
  }

  return action.windowOperation
}

function actionSummary(action) {
  if (action.actionType === 'window') {
    const operation = editor.windowOperations.find(
      item => item.value === action.windowOperation
    )
    return operation?.label ?? '窗口控制'
  }

  if (action.actionType === 'volume') {
    const operation = editor.volumeOperations.find(
      item => item.value === action.volumeOperation
    )
    return action.volumeOperation === 'mute'
      ? operation?.label ?? '静音'
      : `${operation?.label ?? '音量 +'} ${action.amount}`
  }

  if (action.actionType === 'brightness') {
    const operation = editor.brightnessOperations.find(
      item => item.value === action.brightnessOperation
    )
    return `${operation?.label ?? '亮度 +'} ${action.amount}`
  }

  return action.keysText || '未设置快捷键'
}

function updateDraftOperation(value) {
  if (draft.actionType === 'volume') {
    draft.volumeOperation = value
    return
  }

  if (draft.actionType === 'brightness') {
    draft.brightnessOperation = value
    return
  }

  draft.windowOperation = value
}

function recordHotkey() {
  editor.startRecording(draft)
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
          :get-edge-action-label="editor.getEdgeActionLabel"
          @open="openEditor"
        />
      </div>
    </template>
  </AppShell>

  <EdgeActionDialog
    v-if="editingAction"
    :action="editingAction"
    :draft="draft"
    :editor="editor"
    :title="dialogTitle"
    :operation-options="operationOptions"
    :operation-model="operationModel"
    @close="closeEditor"
    @record-hotkey="recordHotkey"
    @update-operation="updateDraftOperation"
  />
</template>
