<script setup>
import { computed, reactive, ref } from 'vue'
import AppShell from '../components/AppShell.vue'
import IconActionButton from '../components/IconActionButton.vue'
import { useGestureEditorStore } from '../composables/gestureEditorStore'

const editor = useGestureEditorStore()
const editingKey = ref('')
const draft = reactive(createEmptyDraft())

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

function groupActions(type) {
  return editor.state.edgeActions.filter(action => action.triggerType === type)
}

const editingAction = computed(
  () => editor.state.edgeActions.find(action => actionKey(action) === editingKey.value) ?? null
)

const dialogTitle = computed(() => {
  const action = editingAction.value
  if (!action) {
    return '边缘操作'
  }

  return [locationLabel(action), wheelLabel(action) || triggerLabels[action.triggerType]].filter(Boolean).join(' · ')
})

function locationLabel(action) {
  const locations = action.triggerType === 'corner'
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
    const operation = editor.windowOperations.find(item => item.value === action.windowOperation)
    return operation?.label ?? '窗口控制'
  }

  if (action.actionType === 'volume') {
    const operation = editor.volumeOperations.find(item => item.value === action.volumeOperation)
    return action.volumeOperation === 'mute'
      ? operation?.label ?? '静音'
      : `${operation?.label ?? '音量 +'} ${action.amount}`
  }

  if (action.actionType === 'brightness') {
    const operation = editor.brightnessOperations.find(item => item.value === action.brightnessOperation)
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

function openEditor(action) {
  editingKey.value = actionKey(action)
  Object.assign(draft, cloneAction(action))
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

  editor.updateEdgeAction(action, toCommitPatch(draft), { notifyResult: false, notifyPreview: false })
}

function recordHotkey() {
  editor.startRecording(draft)
}

function actionKey(action) {
  return `${action.triggerType}:${action.location}:${action.wheelDirection || ''}`
}

function cloneAction(action) {
  return { ...action }
}

function createEmptyDraft() {
  return {
    enabled: false,
    triggerType: 'corner',
    location: 'top-left',
    wheelDirection: '',
    frictionCount: 4,
    actionName: '',
    keysText: '',
    actionType: 'hotkey',
    windowOperation: 'toggle-maximize',
    volumeOperation: 'increase',
    brightnessOperation: 'increase',
    amount: 5
  }
}

function toCommitPatch(action) {
  return {
    enabled: action.enabled,
    actionName: action.actionName,
    frictionCount: action.frictionCount,
    actionType: action.actionType,
    keysText: action.keysText,
    windowOperation: action.windowOperation,
    volumeOperation: action.volumeOperation,
    brightnessOperation: action.brightnessOperation,
    amount: action.amount
  }
}
</script>

<template>
  <AppShell
    title="边缘操作"
    description="配置屏幕角落、四边摩擦和边缘滚轮触发的动作。"
    layout-class="page-shell__grid--single"
  >
    <template #right>
      <div class="edge-page">
        <section
          v-for="group in groups"
          :key="group.type"
          class="edge-section"
        >
          <div class="edge-section__head">
            <div>
              <h3>{{ group.title }}</h3>
              <p>{{ group.description }}</p>
            </div>
          </div>

          <div class="edge-grid">
            <article
              v-for="action in groupActions(group.type)"
              :key="`${action.triggerType}:${action.location}:${action.wheelDirection}`"
              class="edge-card"
              :class="{ 'is-disabled': !action.enabled }"
              role="button"
              tabindex="0"
              @click="openEditor(action)"
              @keydown.enter.prevent="openEditor(action)"
            >
              <header class="edge-card__head">
                <span
                  class="edge-status"
                  :class="{ 'is-enabled': action.enabled }"
                />
                <div>
                  <strong>{{ locationLabel(action) }}</strong>
                  <small>{{ wheelLabel(action) || triggerLabels[action.triggerType] }}</small>
                </div>
              </header>

              <div class="edge-card__summary">
                <span>{{ action.actionName || editor.getEdgeActionLabel(action) }}</span>
                <strong>{{ actionSummary(action) }}</strong>
                <small v-if="action.triggerType === 'friction'">摩擦 {{ action.frictionCount }} 次</small>
              </div>
            </article>
          </div>
        </section>
      </div>
    </template>
  </AppShell>

  <div
    v-if="editingAction"
    class="modal-backdrop"
    @click.self="closeEditor"
  >
    <section
      class="modal-panel edge-dialog"
      role="dialog"
      aria-modal="true"
      aria-labelledby="edge-dialog-title"
      tabindex="-1"
      @keydown.esc.prevent="closeEditor"
    >
      <div class="modal-panel__head">
        <div>
          <h3 id="edge-dialog-title">{{ dialogTitle }}</h3>
          <p>关闭弹窗后自动保存。</p>
        </div>
        <IconActionButton
          icon="close"
          label="关闭"
          class="ghost-button"
          @click="closeEditor"
        />
      </div>

      <div class="edge-dialog__grid">
        <label class="edge-toggle-field">
          <span>启用</span>
          <span class="edge-toggle">
            <input
              v-model="draft.enabled"
              type="checkbox"
            />
            <span />
          </span>
        </label>

        <label class="edge-field">
          <span>名称</span>
          <input
            v-model="draft.actionName"
            class="scope-input"
            :placeholder="editor.getEdgeActionLabel(editingAction)"
          />
        </label>

        <label
          v-if="draft.triggerType === 'friction'"
          class="edge-field"
        >
          <span>摩擦次数</span>
          <input
            v-model.number="draft.frictionCount"
            class="scope-input"
            type="number"
            min="1"
            max="20"
          />
        </label>

        <label class="edge-field">
          <span>命令类型</span>
          <select
            v-model="draft.actionType"
            class="scope-input"
          >
            <option value="hotkey">快捷键</option>
            <option value="window">窗口控制</option>
            <option value="volume">音量控制</option>
            <option value="brightness">亮度控制</option>
          </select>
        </label>
      </div>

      <div class="edge-dialog__command">
        <label
          v-if="draft.actionType === 'hotkey'"
          class="edge-field"
        >
          <span>操作</span>
          <button
            type="button"
            class="scope-input hotkey-record-button"
            :class="{ 'is-recording': editor.isRecordingHotkey(draft) }"
            @click="recordHotkey"
          >
            {{
              editor.isRecordingHotkey(draft)
                ? '录制中...'
                : draft.keysText || '点击录制快捷键'
            }}
          </button>
        </label>

        <label
          v-else
          class="edge-field"
        >
          <span>操作</span>
          <select
            :value="operationModel(draft)"
            class="scope-input"
            @change="updateDraftOperation($event.target.value)"
          >
            <option
              v-for="operation in operationOptions(draft)"
              :key="operation.value"
              :value="operation.value"
            >
              {{ operation.label }}
            </option>
          </select>
        </label>

        <label
          v-if="draft.actionType === 'volume' && draft.volumeOperation !== 'mute'"
          class="edge-field"
        >
          <span>数值</span>
          <input
            v-model.number="draft.amount"
            class="scope-input"
            type="number"
            min="1"
            max="100"
          />
        </label>

        <label
          v-if="draft.actionType === 'brightness'"
          class="edge-field"
        >
          <span>数值</span>
          <input
            v-model.number="draft.amount"
            class="scope-input"
            type="number"
            min="1"
            max="100"
          />
        </label>
      </div>
    </section>
  </div>
</template>

<style scoped lang="scss">
.edge-page {
  display: grid;
  gap: 16px;
}

.edge-section {
  display: grid;
  gap: 14px;
  padding: 22px;
  border: 1px solid var(--border);
  border-radius: 26px;
  background: rgba(255, 255, 255, 0.9);
  box-shadow: var(--shadow-soft);
  backdrop-filter: blur(20px) saturate(1.12);

  &__head {
    display: flex;
    justify-content: space-between;
    gap: 12px;
  }

  h3 {
    font-size: 16px;
    font-weight: 700;
  }

  p {
    margin-top: 4px;
    color: var(--muted);
    font-size: 13px;
  }
}

.edge-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.edge-card {
  display: grid;
  gap: 12px;
  min-width: 0;
  padding: 14px;
  border: 1px solid rgba(18, 30, 42, 0.08);
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.78);
  text-align: left;
  cursor: pointer;

  &:hover,
  &:focus-visible {
    border-color: rgba(29, 81, 109, 0.26);
    background: #fff;
    outline: none;
  }

  &.is-disabled {
    opacity: 0.62;
  }

  &__head {
    display: flex;
    align-items: center;
    gap: 10px;

    strong,
    small {
      display: block;
    }

    strong {
      font-size: 14px;
    }

    small {
      color: var(--muted);
      font-size: 12px;
    }
  }

  &__summary {
    display: grid;
    gap: 4px;

    span {
      color: var(--text);
      font-size: 13px;
    }

    strong {
      font-size: 14px;
      font-weight: 700;
    }

    small {
      color: var(--muted);
      font-size: 12px;
    }
  }
}

.edge-status {
  width: 12px;
  height: 12px;
  border-radius: 999px;
  flex: 0 0 auto;
  background: rgba(101, 113, 128, 0.32);

  &.is-enabled {
    background: var(--accent);
    box-shadow: 0 0 0 4px rgba(29, 81, 109, 0.1);
  }
}

.edge-toggle {
  position: relative;
  display: inline-flex;
  width: 42px;
  height: 24px;
  flex: 0 0 auto;

  input {
    position: absolute;
    opacity: 0;
  }

  span {
    width: 100%;
    border-radius: 999px;
    background: rgba(101, 113, 128, 0.22);
    transition: background-color 120ms ease;

    &::after {
      content: '';
      position: absolute;
      top: 3px;
      left: 3px;
      width: 18px;
      height: 18px;
      border-radius: 999px;
      background: #fff;
      box-shadow: 0 3px 8px rgba(18, 30, 42, 0.18);
      transition: transform 120ms ease;
    }
  }

  input:checked + span {
    background: var(--accent);

    &::after {
      transform: translateX(18px);
    }
  }
}

.edge-field {
  display: grid;
  gap: 6px;
  color: var(--muted);
  font-size: 13px;
}

.edge-toggle-field {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 42px;
  color: var(--muted);
  font-size: 13px;
}

.edge-dialog {
  width: min(620px, calc(100vw - 36px));

  &__grid,
  &__command {
    display: grid;
    gap: 12px;
  }

  &__grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  &__command {
    margin-top: 12px;
  }
}

.edge-command {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.hotkey-record-button {
  overflow: hidden;
  text-align: left;
  white-space: nowrap;
  text-overflow: ellipsis;
  cursor: pointer;
  color: var(--text);
  background: #fff;

  &.is-recording {
    color: #8a441f;
    background: #fff3df;
  }
}

@media (max-width: 920px) {
  .edge-grid,
  .edge-command,
  .edge-dialog__grid {
    grid-template-columns: 1fr;
  }
}
</style>
