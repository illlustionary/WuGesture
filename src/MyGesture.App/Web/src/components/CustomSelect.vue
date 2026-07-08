<script setup>
import { computed } from 'vue'

const props = defineProps({
  modelValue: { type: [String, Number], default: '' },
  value: { type: [String, Number], default: undefined },
  options: { type: Array, required: true },
  placeholder: { type: String, default: '请选择' },
  disabled: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'change'])

const currentValue = computed(() =>
  props.value === undefined ? props.modelValue : props.value
)

const selectedOption = computed(() =>
  props.options.find(option => option.value === currentValue.value)
)

function updateValue(event) {
  const nextValue = event.target.value
  emit('update:modelValue', nextValue)
  emit('change', nextValue)
}
</script>

<template>
  <label
    class="custom-select"
    :class="{ 'is-disabled': disabled }"
  >
    <select
      class="custom-select__native"
      :value="currentValue"
      :disabled="disabled"
      @change="updateValue"
    >
      <option
        v-if="placeholder"
        value=""
        disabled
      >
        {{ placeholder }}
      </option>
      <option
        v-for="option in options"
        :key="option.value"
        :value="option.value"
      >
        {{ option.label }}
      </option>
    </select>
    <span class="custom-select__value">
      {{ selectedOption?.label || placeholder }}
    </span>
    <span
      class="custom-select__chevron"
      aria-hidden="true"
    />
  </label>
</template>

<style scoped lang="scss">
.custom-select {
  position: relative;
  display: grid;
  align-items: center;
  min-height: 42px;
  border: 1px solid var(--border);
  border-radius: 14px;
  background: var(--panel-control);
  color: var(--text);
  box-shadow: var(--shadow-control);
  transition:
    border-color 120ms ease,
    background-color 120ms ease,
    box-shadow 120ms ease;

  &:hover {
    border-color: var(--border-strong);
    background: var(--interactive-hover-bg);
  }

  &:focus-within {
    border-color: var(--accent-border-strong);
    box-shadow: 0 0 0 4px var(--focus-ring);
  }

  &.is-disabled {
    cursor: not-allowed;
    opacity: 0.58;
  }
}

.custom-select__native {
  position: absolute;
  inset: 0;
  z-index: 2;
  width: 100%;
  height: 100%;
  opacity: 0;
  cursor: pointer;

  &:disabled {
    cursor: not-allowed;
  }
}

.custom-select__value {
  min-width: 0;
  padding: 0 42px 0 12px;
  overflow: hidden;
  color: var(--text);
  font-size: 13px;
  font-weight: 650;
  line-height: 42px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.custom-select__chevron {
  position: absolute;
  top: 50%;
  right: 14px;
  width: 8px;
  height: 8px;
  border-right: 2px solid var(--muted);
  border-bottom: 2px solid var(--muted);
  pointer-events: none;
  transform: translateY(-65%) rotate(45deg);
}
</style>
