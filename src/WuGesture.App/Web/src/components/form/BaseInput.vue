<script setup lang="ts">
import { computed, useAttrs } from 'vue'

type InputValue = string | number

const props = withDefaults(
  defineProps<{
    modelValue?: InputValue
    type?: string
    modelModifiers?: Record<string, boolean>
  }>(),
  {
    modelValue: '',
    type: 'text',
    modelModifiers: () => ({})
  }
)

const emit = defineEmits<{
  'update:modelValue': [value: InputValue]
  input: [event: Event]
  change: [event: Event]
  blur: [event: FocusEvent]
}>()

const attrs = useAttrs()
const value = computed(() => props.modelValue ?? '')

/** 已由全局原生控件样式接管外观的类型，不再套 Filled Text Field。 */
const NATIVE_TYPES = new Set(['color', 'range', 'checkbox', 'radio', 'file', 'date', 'time'])

const isField = computed(() => !NATIVE_TYPES.has(props.type))

function normalizeValue(event: Event) {
  const target = event.target as HTMLInputElement
  let nextValue: InputValue = target.value

  if (props.modelModifiers.trim) {
    nextValue = nextValue.trim()
  }

  if (props.modelModifiers.number) {
    const numericValue = target.valueAsNumber
    nextValue = Number.isNaN(numericValue) ? target.value : numericValue
  }

  return nextValue
}

function update(event: Event) {
  emit('update:modelValue', normalizeValue(event))
  emit('input', event)
}

function change(event: Event) {
  emit('change', event)
}
</script>

<template>
  <input
    v-bind="attrs"
    class="base-input"
    :class="{ 'base-input--field': isField }"
    :type="type"
    :value="value"
    @input="update"
    @change="change"
    @blur="emit('blur', $event)"
  />
</template>

<style scoped lang="scss">
.base-input {
  width: 100%;
}

/* Filled Text Field：容器高 48，顶部圆角 8，聚焦时底部 2px primary。 */
.base-input.base-input--field {
  min-height: 48px;
  padding: 0 16px;
  border: 0;
  border-bottom: 1px solid var(--md-sys-color-on-surface-variant);
  border-radius: var(--md-sys-shape-corner-extra-small) var(--md-sys-shape-corner-extra-small) 0 0;
  background: var(--md-sys-color-surface-container-highest);
  color: var(--md-sys-color-on-surface);
  font-size: var(--md-sys-typescale-body-large-size);
  line-height: var(--md-sys-typescale-body-large-line-height);
  transition:
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    box-shadow var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    border-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &::placeholder {
    color: var(--md-sys-color-on-surface-variant);
    opacity: 1;
  }

  &:hover {
    background: color-mix(in srgb, var(--md-sys-color-on-surface) 6%, var(--md-sys-color-surface-container-highest));
  }

  &:focus {
    border-bottom-color: var(--md-sys-color-primary);
    box-shadow: inset 0 -2px 0 0 var(--md-sys-color-primary);
    outline: none;
  }

  &:disabled {
    cursor: not-allowed;
    border-bottom-style: dotted;
    background: color-mix(in srgb, var(--md-sys-color-on-surface) 4%, transparent);
    color: var(--md-sys-color-on-surface);
    opacity: var(--md-sys-state-disabled-content-opacity);
  }
}
</style>
