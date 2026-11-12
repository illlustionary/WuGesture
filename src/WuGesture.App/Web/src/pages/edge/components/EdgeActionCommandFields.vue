<script setup>
import AppIcon from '@/components/ui/AppIcon.vue'
import CustomSelect from '@/components/form/CustomSelect.vue'
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
        <AppIcon
          name="keyboard"
          class="hotkey-record-button__icon"
          aria-hidden="true"
        />
        <span>
          {{ isRecordingHotkey(draft) ? '录制中...' : draft.keysText || '点击录制快捷键' }}
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
@use '../../../styles/tokens/state' as *;

.edge-dialog__command {
  display: grid;
  gap: var(--md-sys-spacing-lg);
  margin-top: var(--md-sys-spacing-lg);
}

.edge-field {
  display: grid;
  gap: var(--md-sys-spacing-xs);
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-label-large-size);
  line-height: var(--md-sys-typescale-label-large-line-height);
}

.hotkey-record-button {
  @include state-layer(currentColor);

  display: inline-flex;
  align-items: center;
  gap: var(--md-sys-spacing-sm);
  overflow: hidden;
  color: var(--md-sys-color-on-surface);
  text-align: left;
  white-space: nowrap;
  text-overflow: ellipsis;
  cursor: pointer;
  background: var(--md-sys-color-surface-container-highest);

  &__icon {
    flex: 0 0 auto;
    width: 18px;
    height: 18px;
    color: var(--md-sys-color-primary);
  }

  &.is-recording {
    color: var(--md-sys-color-on-error-container);
    background: var(--md-sys-color-error-container);
  }

  &.is-recording &__icon {
    color: var(--md-sys-color-on-error-container);
  }
}
</style>
