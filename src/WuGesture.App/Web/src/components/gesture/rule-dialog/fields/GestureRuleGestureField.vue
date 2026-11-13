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
  gap: var(--md-sys-spacing-sm);
}

.gesture-rule-gesture-field__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--md-sys-spacing-md);
}

.gesture-rule-gesture-field__label {
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-label-large-size);
  line-height: var(--md-sys-typescale-label-large-line-height);
  font-weight: var(--md-sys-typescale-label-large-weight);
}

.gesture-rule-gesture-field__options {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: var(--md-sys-spacing-md);
  margin-top: var(--md-sys-spacing-sm);
}

.gesture-rule-gesture-field__control {
  display: flex;
  align-items: center;
  min-height: var(--md-sys-size-field-height);
  overflow: hidden;
  border: 1px solid var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-small);
  background: var(--md-sys-color-surface-container);

  strong {
    min-width: 0;
    flex: 1 1 auto;
    padding: 0 var(--md-sys-spacing-lg);
    overflow: hidden;
    color: var(--md-sys-color-on-surface);
    font-size: var(--md-sys-typescale-body-medium-size);
    line-height: var(--md-sys-typescale-body-medium-line-height);
    text-overflow: ellipsis;
    white-space: nowrap;

    &.is-empty {
      color: var(--md-sys-color-outline);
      font-weight: 500;
    }
  }
}

.gesture-rule-gesture-field__record {
  display: flex;
  align-self: stretch;
  align-items: center;
  justify-content: center;
  gap: var(--md-sys-spacing-sm);
  min-width: 128px;
  padding: 0 var(--md-sys-spacing-lg);
  border: 0;
  border-left: 1px solid color-mix(in srgb, var(--md-sys-color-outline-variant) 60%, transparent);
  background: transparent;
  color: var(--md-sys-color-primary);
  cursor: pointer;
  transition:
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover {
    background: color-mix(in srgb, var(--md-sys-color-primary) 12%, transparent);
  }

  &.is-recording {
    background: var(--md-sys-color-tertiary-container);
    color: var(--md-sys-color-on-tertiary-container);
  }
}

.gesture-rule-gesture-field__record-icon {
  width: 16px;
  height: 16px;
}

.gesture-rule-gesture-field__message {
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
}

@media (max-width: 720px) {
  .gesture-rule-gesture-field__control {
    flex-direction: column;

    .gesture-rule-gesture-field__record {
      width: 100%;
      min-height: 40px;
      border-top: 1px solid color-mix(in srgb, var(--md-sys-color-outline-variant) 60%, transparent);
      border-left: 0;
    }
  }
}
</style>
