<script setup>
import { computed, useAttrs } from 'vue'

const props = defineProps({
  modelValue: { type: [String, Number], default: '' },
  type: { type: String, default: 'text' },
  modelModifiers: { type: Object, default: () => ({}) }
})

const emit = defineEmits(['update:modelValue', 'input', 'change', 'blur'])
const attrs = useAttrs()
const value = computed(() => props.modelValue ?? '')

function normalizeValue(event) {
  let nextValue = event.target.value

  if (props.modelModifiers.trim) {
    nextValue = nextValue.trim()
  }

  if (props.modelModifiers.number) {
    const numericValue = event.target.valueAsNumber
    nextValue = Number.isNaN(numericValue) ? event.target.value : numericValue
  }

  return nextValue
}

function update(event) {
  emit('update:modelValue', normalizeValue(event))
  emit('input', event)
}

function change(event) {
  emit('change', event)
}
</script>

<template>
  <input
    v-bind="attrs"
    class="base-input"
    :type="type"
    :value="value"
    @input="update"
    @change="change"
    @blur="emit('blur', $event)"
  >
</template>

<style scoped lang="scss">
.base-input {
  width: 100%;
}
</style>
