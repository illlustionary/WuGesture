<script setup>
import { computed } from 'vue'
import AppIcon from '@/components/ui/AppIcon.vue'
import CustomSelect from '@/components/form/CustomSelect.vue'
import GestureRuleField from './GestureRuleField.vue'
import {
  DIRECTION_SYMBOLS,
  MOUSE_BUTTON_OPTIONS,
  WHEEL_MODIFIER_OPTIONS,
  WHEEL_TRIGGER_OPTIONS
} from '@/constants/gestureEditorOptions'
import { normalizeWheelModifier, parsePattern } from '@/utils/gestureEditorNormalizers'

const props = defineProps({
  draft: { type: Object, required: true },
  isRecording: { type: Boolean, default: false },
  message: { type: String, default: '' }
})

defineEmits(['record'])

const directionLabel = computed(() =>
  parsePattern(props.draft.patternText)
    .map(direction => DIRECTION_SYMBOLS[direction] ?? '')
    .join('')
)

const hasWheelModifier = computed(() => normalizeWheelModifier(props.draft.wheelModifier) !== '')

const mouseButtonLabel = computed(
  () => MOUSE_BUTTON_OPTIONS.find(option => option.value === props.draft.mouseButton)?.label ?? '右键'
)

const wheelTrigger = computed({
  get: () => (props.draft.executeOnWheel ? 'repeat' : 'once'),
  set: value => {
    props.draft.executeOnWheel = value === 'repeat'
  }
})

const hint = computed(() => {
  if (!hasWheelModifier.value) {
    return `按住${mouseButtonLabel.value}绘制方向手势，松开时执行。`
  }

  const prefix = directionLabel.value
    ? `先按住${mouseButtonLabel.value}绘制手势，再继续按住滚动滚轮。`
    : `按住${mouseButtonLabel.value}直接滚动滚轮即可触发。`
  return props.draft.executeOnWheel
    ? `${prefix}每滚动一格执行一次。`
    : `${prefix}滚轮只用于选择动作，松开${mouseButtonLabel.value}时执行一次。`
})
</script>

<template>
  <section
    class="gesture-rule-gesture-field"
    aria-labelledby="gesture-dialog-gesture-label"
  >
    <div class="gesture-rule-gesture-field__header">
      <span
        id="gesture-dialog-gesture-label"
        class="gesture-rule-gesture-field__label"
        >手势</span
      >
    </div>

    <div class="gesture-rule-gesture-field__control">
      <strong :class="{ 'is-empty': !directionLabel }">
        {{ directionLabel || (hasWheelModifier ? '未录制方向手势' : '尚未录制') }}
      </strong>
      <button
        type="button"
        class="gesture-rule-gesture-field__record"
        :class="{ 'is-recording': isRecording }"
        @click="$emit('record')"
      >
        <AppIcon
          name="record"
          class="gesture-rule-gesture-field__record-icon"
          aria-hidden="true"
        />
        <span>{{ isRecording ? '停止录制' : directionLabel ? '重新录制' : '录制手势' }}</span>
      </button>
    </div>

    <div class="gesture-rule-gesture-field__options">
      <GestureRuleField label="按住按键">
        <CustomSelect
          v-model="draft.mouseButton"
          :options="MOUSE_BUTTON_OPTIONS"
          placeholder="选择鼠标按键"
        />
      </GestureRuleField>
      <GestureRuleField label="滚轮修饰">
        <CustomSelect
          v-model="draft.wheelModifier"
          :options="WHEEL_MODIFIER_OPTIONS"
          placeholder="选择滚轮修饰"
        />
      </GestureRuleField>
      <GestureRuleField
        v-if="hasWheelModifier"
        label="触发方式"
      >
        <CustomSelect
          v-model="wheelTrigger"
          :options="WHEEL_TRIGGER_OPTIONS"
          placeholder="选择触发方式"
        />
      </GestureRuleField>
    </div>

    <p class="gesture-rule-gesture-field__message">
      {{ hint }}
    </p>
    <p
      v-if="message"
      class="gesture-rule-gesture-field__message"
    >
      {{ message }}
    </p>
  </section>
</template>

<style scoped lang="scss">
.gesture-rule-gesture-field {
  display: grid;
  gap: 6px;
}

.gesture-rule-gesture-field__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.gesture-rule-gesture-field__label {
  color: var(--muted);
  font-size: 13px;
  font-weight: 600;
}

.gesture-rule-gesture-field__options {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 12px;
  margin-top: 6px;
}

.gesture-rule-gesture-field__control {
  display: flex;
  align-items: center;
  min-height: 52px;
  overflow: hidden;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  background: var(--panel-inset);

  strong {
    min-width: 0;
    flex: 1 1 auto;
    padding: 0 14px;
    overflow: hidden;
    color: var(--text);
    font-size: 14px;
    text-overflow: ellipsis;
    white-space: nowrap;

    &.is-empty {
      color: var(--text-subtle);
      font-weight: 500;
    }
  }
}

.gesture-rule-gesture-field__record {
  display: flex;
  align-self: stretch;
  align-items: center;
  justify-content: center;
  gap: 7px;
  min-width: 128px;
  padding: 0 16px;
  border: 0;
  border-left: 1px solid var(--border-muted);
  background: transparent;
  color: var(--accent-strong);
  cursor: pointer;
  transition:
    background-color 120ms ease,
    color 120ms ease;

  &:hover {
    background: var(--accent-soft);
  }

  &.is-recording {
    background: var(--recording-bg);
    color: var(--recording-text);
  }
}

.gesture-rule-gesture-field__record-icon {
  width: 16px;
  height: 16px;
}

.gesture-rule-gesture-field__message {
  color: var(--muted);
  font-size: 13px;
  line-height: 1.45;
}

@media (max-width: 720px) {
  .gesture-rule-gesture-field__control {
    flex-direction: column;

    .gesture-rule-gesture-field__record {
      width: 100%;
      min-height: 40px;
      border-top: 1px solid var(--border-muted);
      border-left: 0;
    }
  }
}
</style>
