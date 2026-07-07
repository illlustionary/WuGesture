<script setup>
import KeyboardIcon from '../../../assets/keyboard.svg'

defineProps({
  draft: { type: Object, required: true },
  editor: { type: Object, required: true },
  operationOptions: { type: Function, required: true },
  operationModel: { type: Function, required: true }
})

const emit = defineEmits(['record-hotkey', 'update-operation'])
</script>

<template>
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
        @click="emit('record-hotkey')"
      >
        <KeyboardIcon
          class="hotkey-record-button__icon"
          aria-hidden="true"
        />
        <span>
          {{
            editor.isRecordingHotkey(draft)
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
      <select
        :value="operationModel(draft)"
        class="scope-input"
        @change="emit('update-operation', $event.target.value)"
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
