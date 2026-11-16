<script setup lang="ts">
import { computed, useAttrs } from 'vue'
import { getRangeProgress } from '@/utils/rangeProgress'

type RangeValue = string | number

const props = withDefaults(
  defineProps<{
    modelValue: RangeValue
    min: RangeValue
    max: RangeValue
    step?: RangeValue
    modelModifiers?: Record<string, boolean>
  }>(),
  {
    step: 1,
    modelModifiers: () => ({})
  }
)

const emit = defineEmits<{
  'update:modelValue': [value: RangeValue]
  input: [event: Event]
  change: [event: Event]
  blur: [event: FocusEvent]
}>()

const attrs = useAttrs()
const rangeStyle = computed(() => ({
  '--range-progress': getRangeProgress(props.modelValue, props.min, props.max)
}))

function update(event: Event) {
  const target = event.target as HTMLInputElement
  const numericValue = target.valueAsNumber
  emit('update:modelValue', props.modelModifiers.number ? numericValue : target.value)
  emit('input', event)
}
</script>

<template>
  <input
    v-bind="attrs"
    class="base-range"
    type="range"
    :value="modelValue"
    :min="min"
    :max="max"
    :step="step"
    :style="rangeStyle"
    @input="update"
    @change="emit('change', $event)"
    @blur="emit('blur', $event)"
  />
</template>

<style scoped lang="scss">
.base-range {
  width: 100%;
  cursor: pointer;

  &:disabled {
    cursor: not-allowed;
  }
}
</style>
