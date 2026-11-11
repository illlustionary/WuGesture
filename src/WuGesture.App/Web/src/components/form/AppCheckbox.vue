<script setup lang="ts">
withDefaults(
  defineProps<{
    modelValue: boolean
    label?: string
    disabled?: boolean
    indeterminate?: boolean
  }>(),
  {
    label: '',
    disabled: false,
    indeterminate: false
  }
)

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  change: [value: boolean]
}>()

function updateValue(event: Event) {
  const checked = (event.target as HTMLInputElement).checked
  emit('update:modelValue', checked)
  emit('change', checked)
}
</script>

<template>
  <label
    class="app-checkbox"
    :class="{ 'is-disabled': disabled, 'is-indeterminate': indeterminate }"
  >
    <input
      class="app-checkbox__native"
      type="checkbox"
      :checked="modelValue"
      :disabled="disabled"
      :aria-checked="indeterminate ? 'mixed' : modelValue"
      @change="updateValue"
    />
    <span
      class="app-checkbox__box"
      aria-hidden="true"
    >
      <svg viewBox="0 0 24 24">
        <path
          v-if="indeterminate"
          d="M5 11h14v2H5z"
          fill="currentColor"
        />
        <path
          v-else
          d="M9 16.17 4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"
          fill="currentColor"
        />
      </svg>
    </span>
    <span
      v-if="label || $slots.default"
      class="app-checkbox__label"
    >
      <slot>{{ label }}</slot>
    </span>
  </label>
</template>

<style scoped lang="scss">
.app-checkbox {
  display: inline-flex;
  align-items: center;
  gap: 12px;
  min-height: 32px;
  color: var(--md-sys-color-on-surface);
  font-size: var(--md-sys-typescale-body-medium-size);
  line-height: var(--md-sys-typescale-body-medium-line-height);
  cursor: pointer;

  &.is-disabled {
    cursor: not-allowed;
    opacity: var(--md-sys-state-disabled-content-opacity);
  }
}

.app-checkbox__native {
  position: absolute;
  width: 1px;
  height: 1px;
  opacity: 0;
  pointer-events: none;
}

.app-checkbox__box {
  display: inline-grid;
  place-items: center;
  width: 18px;
  height: 18px;
  flex: 0 0 auto;
  border: 2px solid var(--md-sys-color-on-surface-variant);
  border-radius: 2px;
  color: var(--md-sys-color-on-primary);
  transition:
    border-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  svg {
    width: 16px;
    height: 16px;
    opacity: 0;
    transform: scale(0.6);
    transition:
      opacity var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
      transform var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);
  }
}

.app-checkbox__native:checked + .app-checkbox__box,
.app-checkbox__native:indeterminate + .app-checkbox__box,
.app-checkbox.is-indeterminate .app-checkbox__box {
  border-color: var(--md-sys-color-primary);
  background: var(--md-sys-color-primary);

  svg {
    opacity: 1;
    transform: scale(1);
  }
}

.app-checkbox__native:focus-visible + .app-checkbox__box {
  outline: 2px solid var(--md-sys-color-primary);
  outline-offset: 2px;
}

.app-checkbox__label {
  min-width: 0;
}
</style>
