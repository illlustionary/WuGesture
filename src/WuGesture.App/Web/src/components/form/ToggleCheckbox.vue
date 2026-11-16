<script setup lang="ts">
withDefaults(
  defineProps<{
    modelValue: boolean
    label: string
    note?: string
    disabled?: boolean
  }>(),
  {
    note: '',
    disabled: false
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
    class="toggle-checkbox"
    :class="{ 'is-disabled': disabled }"
  >
    <span class="toggle-checkbox__text">
      <span class="toggle-checkbox__label">{{ label }}</span>
      <small
        v-if="note"
        class="toggle-checkbox__note"
        >{{ note }}</small
      >
    </span>
    <span class="toggle-checkbox__control">
      <input
        class="toggle-checkbox__native"
        type="checkbox"
        role="switch"
        :checked="modelValue"
        :disabled="disabled"
        :aria-checked="modelValue"
        @change="updateValue"
      />
      <span
        class="toggle-checkbox__track"
        aria-hidden="true"
      >
        <span class="toggle-checkbox__thumb" />
      </span>
    </span>
  </label>
</template>

<style scoped lang="scss">
.toggle-checkbox {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  min-height: 56px;
  padding: 8px 12px;
  border-radius: var(--md-sys-shape-corner-small);
  background: transparent;
  cursor: pointer;
  transition: background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover {
    background: color-mix(in srgb, var(--md-sys-color-on-surface) 6%, transparent);
  }

  &:has(.toggle-checkbox__native:focus-visible) {
    outline: 2px solid var(--md-sys-color-primary);
    outline-offset: -2px;
  }

  &.is-disabled {
    cursor: not-allowed;
    opacity: var(--md-sys-state-disabled-content-opacity);
  }
}

.toggle-checkbox__text {
  display: grid;
  min-width: 0;
  gap: 2px;
}

.toggle-checkbox__label {
  color: var(--md-sys-color-on-surface);
  font-size: var(--md-sys-typescale-body-medium-size);
  line-height: var(--md-sys-typescale-body-medium-line-height);
  font-weight: var(--md-sys-typescale-label-large-weight);
}

.toggle-checkbox__note {
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
}

.toggle-checkbox__control {
  position: relative;
  display: inline-flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
}

.toggle-checkbox__native {
  position: absolute;
  width: 1px;
  height: 1px;
  opacity: 0;
  pointer-events: none;
}

.toggle-checkbox__track {
  position: relative;
  display: block;
  width: 52px;
  height: 32px;
  border: 2px solid var(--md-sys-color-outline);
  border-radius: var(--md-sys-shape-corner-full);
  background: var(--md-sys-color-surface-container-highest);
  transition:
    background-color var(--md-sys-motion-duration-medium) var(--md-sys-motion-easing-emphasized),
    border-color var(--md-sys-motion-duration-medium) var(--md-sys-motion-easing-emphasized);
}

.toggle-checkbox__thumb {
  position: absolute;
  top: 50%;
  left: 8px;
  width: 16px;
  height: 16px;
  border-radius: var(--md-sys-shape-corner-full);
  background: var(--md-sys-color-outline);
  transform: translateY(-50%);
  transition:
    left var(--md-sys-motion-duration-medium) var(--md-sys-motion-easing-emphasized),
    width var(--md-sys-motion-duration-medium) var(--md-sys-motion-easing-emphasized),
    height var(--md-sys-motion-duration-medium) var(--md-sys-motion-easing-emphasized),
    background-color var(--md-sys-motion-duration-medium) var(--md-sys-motion-easing-emphasized);
}

.toggle-checkbox__native:checked + .toggle-checkbox__track {
  border-color: var(--md-sys-color-primary);
  background: var(--md-sys-color-primary);

  .toggle-checkbox__thumb {
    left: 20px;
    width: 24px;
    height: 24px;
    background: var(--md-sys-color-on-primary);
  }
}

.toggle-checkbox__control:active .toggle-checkbox__native:checked + .toggle-checkbox__track .toggle-checkbox__thumb {
  left: 16px;
  width: 28px;
  height: 28px;
}
</style>
