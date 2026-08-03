<script setup>
import { computed } from 'vue'
import AppIcon from '@/components/AppIcon.vue'
import BaseDialog from './BaseDialog.vue'
import CustomSelect from './CustomSelect.vue'
import { ACTION_TYPE_OPTIONS, ACTION_TYPES, OPERATIONS } from '@/constants/gestureEditorOptions'
import { GESTURE_EDITOR_LIMITS } from '@/constants/gestureEditorLimits'

const props = defineProps({
  open: { type: Boolean, required: true },
  draft: { type: Object, required: true },
  message: { type: String, default: '' },
  isRecordingHotkey: { type: Function, default: null },
  isRecordingGesture: { type: Boolean, default: false },
  getGestureMnemonic: { type: Function, default: null },
  windowOperations: { type: Array, default: () => [] },
  volumeOperations: { type: Array, default: () => [] },
  brightnessOperations: { type: Array, default: () => [] }
})

defineEmits(['close', 'persist', 'record', 'record-hotkey'])

const patternLabel = computed(() => props.getGestureMnemonic?.(props.draft) || '尚未录制')

const actionTypeOptions = ACTION_TYPE_OPTIONS
</script>

<template>
  <BaseDialog
    :open="open"
    title="手势"
    description="点击开始录制后，按住右键或中键绘制，松开后完成识别。"
    title-id="gesture-dialog-title"
    :show-close="true"
    :show-actions="false"
    backdrop-class="gesture-dialog-backdrop"
    panel-class="gesture-dialog"
    @close="$emit('close')"
  >
    <div class="gesture-dialog__grid">
      <label>
        <span>名称</span>
        <input
          v-model.trim="draft.actionName"
          class="scope-input"
          placeholder="例如：关闭标签"
          @blur="$emit('persist')"
        />
      </label>

      <label>
        <span>命令类型</span>
        <CustomSelect
          v-model="draft.actionType"
          :options="actionTypeOptions"
          placeholder="选择命令类型"
          @change="$emit('persist')"
        />
      </label>
    </div>

    <div class="gesture-dialog__command">
      <label v-if="draft.actionType === ACTION_TYPES.window">
        <span>操作</span>
        <CustomSelect
          v-model="draft.windowOperation"
          :options="windowOperations"
          placeholder="选择窗口操作"
          @change="$emit('persist')"
        />
      </label>

      <template v-else-if="draft.actionType === ACTION_TYPES.volume">
        <label>
          <span>操作</span>
          <CustomSelect
            v-model="draft.volumeOperation"
            :options="volumeOperations"
            placeholder="选择音量操作"
            @change="$emit('persist')"
          />
        </label>
        <label v-if="draft.volumeOperation !== OPERATIONS.mute">
          <span>数值</span>
          <input
            v-model.number="draft.amount"
            class="scope-input"
            type="number"
            :min="GESTURE_EDITOR_LIMITS.amount.min"
            :max="GESTURE_EDITOR_LIMITS.amount.max"
            @blur="$emit('persist')"
          />
        </label>
      </template>

      <template v-else-if="draft.actionType === ACTION_TYPES.brightness">
        <label>
          <span>操作</span>
          <CustomSelect
            v-model="draft.brightnessOperation"
            :options="brightnessOperations"
            placeholder="选择亮度操作"
            @change="$emit('persist')"
          />
        </label>
        <label>
          <span>数值</span>
          <input
            v-model.number="draft.amount"
            class="scope-input"
            type="number"
            :min="GESTURE_EDITOR_LIMITS.amount.min"
            :max="GESTURE_EDITOR_LIMITS.amount.max"
            @blur="$emit('persist')"
          />
        </label>
      </template>

      <label v-else>
        <span>操作</span>
        <button
          type="button"
          class="scope-input hotkey-record-button"
          :class="{ 'is-recording': isRecordingHotkey?.(draft) }"
          @click="$emit('record-hotkey', draft)"
          @blur="$emit('persist')"
        >
          <AppIcon
            name="keyboard"
            class="hotkey-record-button__icon"
            aria-hidden="true"
          />
          <span>
            {{ isRecordingHotkey?.(draft) ? '录制中...' : draft.keysText || '点击录制快捷键' }}
          </span>
        </button>
      </label>
    </div>

    <button
      type="button"
      class="gesture-recorder__trigger"
      :class="{ 'is-recording': isRecordingGesture }"
      @click="$emit('record')"
    >
      <AppIcon
        name="record"
        class="gesture-recorder__icon"
        aria-hidden="true"
      />
      <span class="gesture-recorder__text">
        <strong>{{ isRecordingGesture ? '停止录制' : '开始录制' }}</strong>
        <span>{{ isRecordingGesture ? '再次点击停止录制。' : '点击后立即开始录制，按住右键或中键绘制。' }}</span>
      </span>
    </button>

    <div class="gesture-dialog__result">
      <span>识别结果</span>
      <strong>{{ patternLabel }}</strong>
    </div>

    <p
      v-if="message"
      class="gesture-dialog__message"
    >
      {{ message }}
    </p>
  </BaseDialog>
</template>

<style scoped lang="scss">
:deep(.gesture-dialog) {
  width: min(780px, calc(100vw - 36px));
}

.gesture-dialog__grid {
  display: grid;
  gap: 12px;
  grid-template-columns: 1fr 1fr;
  margin-bottom: 14px;

  label {
    display: grid;
    gap: 6px;
    color: var(--muted);
    font-size: 13px;
  }
}

.gesture-dialog__command {
  display: grid;
  gap: 10px;
  margin-bottom: 14px;

  label {
    display: grid;
    gap: 6px;
    color: var(--muted);
    font-size: 13px;
  }
}

.gesture-dialog__result {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 36px;
  padding: 4px 0 0;

  span {
    color: var(--muted);
    font-size: 13px;
  }

  strong {
    font-size: 14px;
  }
}

.gesture-dialog__message {
  margin-top: 4px;
  color: var(--muted);
  font-size: 13px;
}

:deep(.gesture-dialog-backdrop) {
  padding: 0;
  background: var(--scrim-subtle);
}

.hotkey-record-button {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  overflow: hidden;
  text-align: left;
  white-space: nowrap;
  text-overflow: ellipsis;
  cursor: pointer;
  color: var(--text);
  background: var(--panel-solid);

  &__icon {
    width: 18px;
    height: 18px;
    flex: 0 0 auto;
    color: var(--accent-strong);
  }

  &:hover {
    background: var(--interactive-hover-bg);
  }

  &.is-recording {
    color: var(--recording-text);
    background: var(--recording-bg);
  }
}

.gesture-recorder__trigger {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  min-height: 74px;
  margin-bottom: 14px;
  padding: 16px 18px;
  text-align: left;
  border-radius: 20px;
  border: 1px solid var(--border-strong);
  background: var(--interactive-bg);
  box-shadow: none;
  transition:
    background-color 140ms ease,
    border-color 140ms ease;

  &:hover {
    border-color: var(--accent-border);
    background: var(--interactive-hover-bg);
  }

  .gesture-recorder__icon {
    width: 28px;
    height: 28px;
    flex: 0 0 auto;
    color: var(--accent-strong);
  }

  .gesture-recorder__text {
    display: grid;
    gap: 4px;
    min-width: 0;
  }

  strong {
    color: var(--accent-strong);
    font-size: 15px;
  }

  span {
    color: var(--muted);
    font-size: 13px;
  }

  &.is-recording {
    border-color: var(--danger-border);
    background: linear-gradient(180deg, var(--recording-bg-start), var(--recording-bg-end));

    strong {
      color: var(--recording-text);
    }

    .gesture-recorder__icon {
      color: var(--recording-text);
    }
  }
}

@media (max-width: 720px) {
  .gesture-dialog__grid {
    grid-template-columns: 1fr;
  }
}
</style>
