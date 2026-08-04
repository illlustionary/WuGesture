<script setup>
import { computed, useAttrs } from 'vue'
import { getRangeProgress } from '@/utils/rangeProgress'

const props = defineProps({
  modelValue: { type: [String, Number], required: true },
  min: { type: [String, Number], required: true },
  max: { type: [String, Number], required: true },
  step: { type: [String, Number], default: 1 },
  modelModifiers: { type: Object, default: () => ({}) }
})

const emit = defineEmits(['update:modelValue', 'input', 'change', 'blur'])
const attrs = useAttrs()
const rangeStyle = computed(() => ({
  '--range-progress': getRangeProgress(props.modelValue, props.min, props.max)
}))

function update(event) {
  const numericValue = event.target.valueAsNumber
  emit('update:modelValue', props.modelModifiers.number ? numericValue : event.target.value)
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
}
</style>
