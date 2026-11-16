<script setup>
import AppIcon from '@/components/ui/AppIcon.vue'
import GestureRuleField from './GestureRuleField.vue'

defineProps({
  draft: { type: Object, required: true },
  isRecording: { type: Boolean, default: false }
})

defineEmits(['record'])
</script>

<template>
  <GestureRuleField label="快捷键">
    <button
      type="button"
      class="scope-input gesture-rule-hotkey-fields__record"
      :class="{ 'is-recording': isRecording }"
      @click="$emit('record')"
    >
      <AppIcon
        name="keyboard"
        class="gesture-rule-hotkey-fields__icon"
        aria-hidden="true"
      />
      <span>{{ isRecording ? '录制中...' : draft.keysText || '点击录制快捷键' }}</span>
    </button>
  </GestureRuleField>
</template>

<style scoped lang="scss">
.gesture-rule-hotkey-fields__record {
  display: inline-flex;
  align-items: center;
  height: var(--md-sys-size-button-height);
  gap: var(--md-sys-spacing-sm);
  overflow: hidden;
  text-align: left;
  white-space: nowrap;
  text-overflow: ellipsis;
  cursor: pointer;
  color: var(--md-sys-color-on-surface);
  background: var(--md-sys-color-surface-container-highest);

  &:hover {
    background: var(--md-sys-color-surface-container-high);
  }

  &.is-recording {
    color: var(--md-sys-color-on-tertiary-container);
    background: var(--md-sys-color-tertiary-container);
  }
}

.gesture-rule-hotkey-fields__icon {
  width: 18px;
  height: 18px;
  flex: 0 0 auto;
  color: var(--md-sys-color-primary);
}
</style>
