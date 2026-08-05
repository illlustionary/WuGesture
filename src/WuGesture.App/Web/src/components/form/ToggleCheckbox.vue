<script setup>
defineProps({
  modelValue: { type: Boolean, required: true },
  label: { type: String, required: true },
  note: { type: String, default: '' },
  disabled: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'change'])

function updateValue(event) {
  const checked = event.target.checked
  emit('update:modelValue', checked)
  emit('change', checked)
}
</script>

<template>
  <label
    class="toggle-checkbox"
    :class="{ 'is-disabled': disabled }"
  >
    <input
      class="toggle-checkbox__native"
      type="checkbox"
      :checked="modelValue"
      :disabled="disabled"
      @change="updateValue"
    />
    <span
      class="toggle-checkbox__box"
      aria-hidden="true"
    >
      <svg
        viewBox="0 0 16 16"
        focusable="false"
      >
        <path
          d="M3.4 8.1 6.5 11 12.7 4.9"
          fill="none"
          stroke="currentColor"
          stroke-width="2.2"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      </svg>
    </span>
    <span class="toggle-checkbox__text">
      <span class="toggle-checkbox__label">{{ label }}</span>
      <small v-if="note">{{ note }}</small>
    </span>
  </label>
</template>

<style scoped lang="scss">
.toggle-checkbox {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 44px;
  padding: 10px 12px;
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  background: var(--panel-inset);
  cursor: pointer;
  transition:
    border-color 120ms ease,
    background-color 120ms ease,
    box-shadow 120ms ease;

  &:hover {
    border-color: var(--border-strong);
    background: var(--interactive-hover-bg);
  }

  &:has(.toggle-checkbox__native:focus-visible) {
    border-color: var(--accent-border-strong);
    box-shadow: 0 0 0 4px var(--focus-ring);
  }

  &:has(.toggle-checkbox__native:checked) {
    border-color: var(--accent-border);
    background: var(--accent-soft);
  }

  &.is-disabled {
    cursor: not-allowed;
    opacity: 0.58;
  }
}

.toggle-checkbox__native {
  position: absolute;
  width: 1px;
  height: 1px;
  opacity: 0;
  pointer-events: none;
}

.toggle-checkbox__box {
  position: relative;
  display: inline-grid;
  place-items: center;
  width: 22px;
  height: 22px;
  flex: 0 0 auto;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-sm);
  background: var(--panel-solid);
  color: var(--text-inverse);
  box-shadow: var(--shadow-control);
  transition:
    border-color 120ms ease,
    background-color 120ms ease,
    transform 120ms ease;

  svg {
    width: 15px;
    height: 15px;
    opacity: 0;
    transform: scale(0.72);
    transition:
      opacity 120ms ease,
      transform 120ms ease;
  }
}

.toggle-checkbox__native:checked + .toggle-checkbox__box {
  border-color: var(--accent-strong);
  background: var(--accent-strong);

  svg {
    opacity: 1;
    transform: scale(1);
  }
}

.toggle-checkbox__text {
  display: grid;
  gap: 2px;
  min-width: 0;
}

.toggle-checkbox__label {
  color: var(--text);
  font-size: 13px;
  font-weight: 650;
}

small {
  color: var(--muted);
  font-size: 12px;
  line-height: 1.35;
}
</style>
