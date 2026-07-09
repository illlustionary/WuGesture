<script setup>
import KeyboardIcon from '@/assets/keyboard.svg'
import CustomSelect from '@/components/CustomSelect.vue'
import { ACTION_TYPES, OPERATIONS } from '@/constants/gestureEditorOptions'
import { GESTURE_EDITOR_LIMITS } from '@/constants/gestureEditorLimits'

defineProps({
  draft: { type: Object, required: true },
  isRecordingHotkey: { type: Function, required: true },
  operationOptions: { type: Function, required: true },
  operationModel: { type: Function, required: true }
})

const emit = defineEmits(['record-hotkey', 'update-operation'])
</script>

<template>
  <div class="edge-dialog__command">
    <label
      v-if="draft.actionType === ACTION_TYPES.hotkey"
      class="edge-field"
    >
      <span>操作</span>
      <button
        type="button"
        class="scope-input hotkey-record-button"
        :class="{ 'is-recording': isRecordingHotkey(draft) }"
        @click="emit('record-hotkey')"
      >
        <KeyboardIcon
          class="hotkey-record-button__icon"
          aria-hidden="true"
        />
        <span>
          {{
            isRecordingHotkey(draft)
              ? '录制中...'
              : draft.keysText || '点击录制快捷键'
          }}
        </span>
      </button>
    </label>

    <label
      v-else
      class="edge-field"
    >
      <span>操作</span>
      <CustomSelect
        :value="operationModel(draft)"
        :options="operationOptions(draft)"
        placeholder="选择操作"
        @change="emit('update-operation', $event)"
      />
    </label>

    <label
      v-if="draft.actionType === ACTION_TYPES.volume && draft.volumeOperation !== OPERATIONS.mute"
      class="edge-field"
    >
      <span>数值</span>
      <input
        v-model.number="draft.amount"
        class="scope-input"
        type="number"
        :min="GESTURE_EDITOR_LIMITS.amount.min"
        :max="GESTURE_EDITOR_LIMITS.amount.max"
      />
    </label>

    <label
      v-if="draft.actionType === ACTION_TYPES.brightness"
      class="edge-field"
    >
      <span>数值</span>
      <input
        v-model.number="draft.amount"
        class="scope-input"
        type="number"
        :min="GESTURE_EDITOR_LIMITS.amount.min"
        :max="GESTURE_EDITOR_LIMITS.amount.max"
      />
    </label>
  </div>
</template>

<style scoped lang="scss">
.edge-dialog__command {
  display: grid;
  gap: 12px;
  margin-top: 12px;
}

.edge-field {
  display: grid;
  gap: 6px;
  color: var(--muted);
  font-size: 13px;
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
</style>
