<script setup>
import { onBeforeUnmount, reactive, watch } from 'vue'
import AppShell from '../components/AppShell.vue'
import { useGestureEditorStore } from '../composables/gestureEditorStore'

const editor = useGestureEditorStore()
const draftActions = reactive([])
const pendingActionKeys = new Set()

const groups = [
  {
    type: 'corner',
    title: '触发角',
    description: '鼠标进入角落时触发一次，离开后再次进入才会再次触发。'
  },
  {
    type: 'friction',
    title: '摩擦边',
    description: '进入角落后按相邻方向反复移动，达到次数后触发。'
  },
  {
    type: 'wheel',
    title: '鼠标滚动边',
    description: '鼠标停在屏幕边缘滚动时触发，并吞掉原始滚轮事件。'
  }
]

const triggerLabels = {
  corner: '触发角',
  friction: '摩擦边',
  wheel: '滚动边'
}

watch(
  () => editor.state.edgeActions,
  () => {
    if (pendingActionKeys.size > 0) {
      return
    }

    replaceDraftActions()
  },
  { deep: true, immediate: true }
)

function groupActions(type) {
  return draftActions.filter(action => action.triggerType === type)
}

function locationLabel(action) {
  const locations = action.triggerType === 'wheel'
    ? editor.edgeLocations.edge
    : editor.edgeLocations.corner
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

function updateOperation(action, value) {
  if (action.actionType === 'volume') {
    updateDraftAction(action, { volumeOperation: value })
    return
  }

  if (action.actionType === 'brightness') {
    updateDraftAction(action, { brightnessOperation: value })
    return
  }

  updateDraftAction(action, { windowOperation: value })
}

function updateDraftAction(action, patch) {
  Object.assign(action, patch)
  pendingActionKeys.add(actionKey(action))
}

function commitDraftPatch(action, patch) {
  updateDraftAction(action, patch)
  commitDraftAction(action)
}

function commitOperation(action, value) {
  updateOperation(action, value)
  commitDraftAction(action)
}

function commitDraftAction(action) {
  const key = actionKey(action)
  const target = findSourceAction(action)
  if (!target) {
    pendingActionKeys.delete(key)
    return
  }

  const patch = toCommitPatch(action)
  if (!pendingActionKeys.has(key) && isPatchEqual(target, patch)) {
    return
  }

  pendingActionKeys.delete(key)
  if (isPatchEqual(target, patch)) {
    return
  }

  editor.updateEdgeAction(target, patch, { notifyResult: false, notifyPreview: false })
}

function flushDraftActions() {
  for (const action of [...draftActions]) {
    commitDraftAction(action)
  }
}

function recordHotkey(action) {
  commitDraftAction(action)
  const target = findSourceAction(action)
  if (!target) {
    return
  }

  editor.startRecording(target)
}

function replaceDraftActions() {
  draftActions.splice(0, draftActions.length, ...editor.state.edgeActions.map(cloneAction))
}

function findSourceAction(action) {
  return editor.state.edgeActions.find(item => actionKey(item) === actionKey(action)) ?? null
}

function cloneAction(action) {
  return { ...action }
}

function actionKey(action) {
  return `${action.triggerType}:${action.location}:${action.wheelDirection || ''}`
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

function isPatchEqual(target, patch) {
  return Object.entries(patch).every(([key, value]) => String(target[key] ?? '') === String(value ?? ''))
}

onBeforeUnmount(() => {
  flushDraftActions()
})
</script>

<template>
  <AppShell
    title="边缘操作"
    description="配置屏幕角落、边缘摩擦和边缘滚轮触发的动作。"
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
            >
              <header class="edge-card__head">
                <label class="edge-toggle">
                  <input
                    type="checkbox"
                    :checked="action.enabled"
                    @change="commitDraftPatch(action, { enabled: $event.target.checked })"
                    @blur="commitDraftAction(action)"
                  />
                  <span />
                </label>
                <div>
                  <strong>{{ locationLabel(action) }}</strong>
                  <small>{{ wheelLabel(action) || triggerLabels[action.triggerType] }}</small>
                </div>
              </header>

              <label class="edge-field">
                <span>名称</span>
                <input
                  :value="action.actionName"
                  class="scope-input"
                  :placeholder="editor.getEdgeActionLabel(action)"
                  @input="updateDraftAction(action, { actionName: $event.target.value })"
                  @change="commitDraftAction(action)"
                  @blur="commitDraftAction(action)"
                />
              </label>

              <label
                v-if="action.triggerType === 'friction'"
                class="edge-field"
              >
                <span>摩擦次数</span>
                <input
                  :value="action.frictionCount"
                  class="scope-input"
                  type="number"
                  min="1"
                  max="20"
                  @input="updateDraftAction(action, { frictionCount: $event.target.value })"
                  @change="commitDraftAction(action)"
                  @blur="commitDraftAction(action)"
                />
              </label>

              <div class="edge-command">
                <label class="edge-field">
                  <span>命令类型</span>
                  <select
                    :value="action.actionType"
                    class="scope-input"
                    @change="commitDraftPatch(action, { actionType: $event.target.value })"
                    @blur="commitDraftAction(action)"
                  >
                    <option value="hotkey">快捷键</option>
                    <option value="window">窗口控制</option>
                    <option value="volume">音量控制</option>
                    <option value="brightness">亮度控制</option>
                  </select>
                </label>

                <label
                  v-if="action.actionType === 'hotkey'"
                  class="edge-field"
                >
                  <span>操作</span>
                  <button
                    type="button"
                    class="scope-input hotkey-record-button"
                    :class="{ 'is-recording': editor.isRecordingHotkey(action) }"
                    @click="recordHotkey(action)"
                  >
                    {{
                      editor.isRecordingHotkey(action)
                        ? '录制中...'
                        : action.keysText || '点击录制快捷键'
                    }}
                  </button>
                </label>

                <label
                  v-else
                  class="edge-field"
                >
                  <span>操作</span>
                  <select
                    :value="operationModel(action)"
                    class="scope-input"
                    @change="commitOperation(action, $event.target.value)"
                    @blur="commitDraftAction(action)"
                  >
                    <option
                      v-for="operation in operationOptions(action)"
                      :key="operation.value"
                      :value="operation.value"
                    >
                      {{ operation.label }}
                    </option>
                  </select>
                </label>

                <label
                  v-if="action.actionType === 'volume' && action.volumeOperation !== 'mute'"
                  class="edge-field"
                >
                  <span>数值</span>
                  <input
                    :value="action.amount"
                    class="scope-input"
                    type="number"
                    min="1"
                    max="100"
                    @input="updateDraftAction(action, { amount: $event.target.value })"
                    @change="commitDraftAction(action)"
                    @blur="commitDraftAction(action)"
                  />
                </label>

                <label
                  v-if="action.actionType === 'brightness'"
                  class="edge-field"
                >
                  <span>数值</span>
                  <input
                    :value="action.amount"
                    class="scope-input"
                    type="number"
                    min="1"
                    max="100"
                    @input="updateDraftAction(action, { amount: $event.target.value })"
                    @change="commitDraftAction(action)"
                    @blur="commitDraftAction(action)"
                  />
                </label>
              </div>
            </article>
          </div>
        </section>
      </div>
    </template>
  </AppShell>
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
  .edge-command {
    grid-template-columns: 1fr;
  }
}
</style>
