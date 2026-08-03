<script setup>
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'

const props = defineProps({
  modelValue: { type: [String, Number], default: '' },
  value: { type: [String, Number], default: undefined },
  options: { type: Array, required: true },
  placeholder: { type: String, default: '请选择' },
  disabled: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'change'])

const rootRef = ref(null)
const listRef = ref(null)
const open = ref(false)
const activeIndex = ref(-1)
const menuStyle = ref({})
const placement = ref('below')

const currentValue = computed(() => (props.value === undefined ? props.modelValue : props.value))

const selectedIndex = computed(() => props.options.findIndex(option => option.value === currentValue.value))

const selectedOption = computed(() => props.options[selectedIndex.value] ?? null)

const listboxId = `custom-select-${Math.random().toString(36).slice(2)}`

watch(open, isOpen => {
  if (isOpen) {
    activeIndex.value = selectedIndex.value >= 0 ? selectedIndex.value : 0
    document.addEventListener('pointerdown', handleOutsidePointerDown, true)
    window.addEventListener('resize', updateMenuPosition)
    window.addEventListener('scroll', updateMenuPosition, true)
    nextTick(() => {
      updateMenuPosition()
      scrollActiveOptionIntoView()
    })
    return
  }

  document.removeEventListener('pointerdown', handleOutsidePointerDown, true)
  window.removeEventListener('resize', updateMenuPosition)
  window.removeEventListener('scroll', updateMenuPosition, true)
})

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', handleOutsidePointerDown, true)
})

function toggleOpen() {
  if (props.disabled) {
    return
  }

  open.value = !open.value
}

function close() {
  open.value = false
}

function handleOutsidePointerDown(event) {
  if (!rootRef.value?.contains(event.target) && !listRef.value?.contains(event.target)) {
    close()
  }
}

function selectOption(option) {
  if (!option || option.disabled) {
    return
  }

  emit('update:modelValue', option.value)
  emit('change', option.value)
  close()
}

function moveActive(delta) {
  if (!props.options.length) {
    return
  }

  if (!open.value) {
    open.value = true
    return
  }

  const total = props.options.length
  let nextIndex = activeIndex.value

  for (let step = 0; step < total; step += 1) {
    nextIndex = (nextIndex + delta + total) % total
    if (!props.options[nextIndex]?.disabled) {
      activeIndex.value = nextIndex
      nextTick(scrollActiveOptionIntoView)
      return
    }
  }
}

function handleKeydown(event) {
  if (props.disabled) {
    return
  }

  if (event.key === 'ArrowDown') {
    event.preventDefault()
    moveActive(1)
  } else if (event.key === 'ArrowUp') {
    event.preventDefault()
    moveActive(-1)
  } else if (event.key === 'Home') {
    event.preventDefault()
    activeIndex.value = 0
    open.value = true
    nextTick(scrollActiveOptionIntoView)
  } else if (event.key === 'End') {
    event.preventDefault()
    activeIndex.value = props.options.length - 1
    open.value = true
    nextTick(scrollActiveOptionIntoView)
  } else if (event.key === 'Enter' || event.key === ' ') {
    event.preventDefault()
    if (!open.value) {
      open.value = true
      return
    }

    selectOption(props.options[activeIndex.value])
  } else if (event.key === 'Escape') {
    event.preventDefault()
    close()
  }
}

function scrollActiveOptionIntoView() {
  const list = listRef.value
  if (!list || activeIndex.value < 0) {
    return
  }

  list.querySelector(`[data-index="${activeIndex.value}"]`)?.scrollIntoView({
    block: 'nearest'
  })
}

function updateMenuPosition() {
  const button = rootRef.value?.querySelector('.custom-select__button')
  const menu = listRef.value
  if (!button || !menu) {
    return
  }

  const buttonRect = button.getBoundingClientRect()
  const viewportPadding = 8
  const gap = 6
  const availableBelow = window.innerHeight - buttonRect.bottom - gap - viewportPadding
  const availableAbove = buttonRect.top - gap - viewportPadding
  const shouldOpenAbove = availableBelow < Math.min(menu.scrollHeight, 240) && availableAbove > availableBelow
  const availableHeight = Math.max(80, Math.min(240, shouldOpenAbove ? availableAbove : availableBelow))
  const menuHeight = Math.min(menu.scrollHeight, availableHeight)
  const left = Math.max(
    viewportPadding,
    Math.min(buttonRect.left, window.innerWidth - buttonRect.width - viewportPadding)
  )
  const top = shouldOpenAbove ? buttonRect.top - gap - menuHeight : buttonRect.bottom + gap

  placement.value = shouldOpenAbove ? 'above' : 'below'
  menuStyle.value = {
    left: `${left}px`,
    top: `${Math.max(viewportPadding, top)}px`,
    width: `${buttonRect.width}px`,
    maxHeight: `${availableHeight}px`
  }
}
</script>

<template>
  <div
    ref="rootRef"
    class="custom-select"
    :class="{ 'is-open': open, 'is-disabled': disabled }"
  >
    <button
      type="button"
      class="custom-select__button"
      :disabled="disabled"
      :aria-expanded="open"
      :aria-controls="listboxId"
      aria-haspopup="listbox"
      @click="toggleOpen"
      @keydown="handleKeydown"
    >
      <span
        class="custom-select__value"
        :class="{ 'is-placeholder': !selectedOption }"
      >
        {{ selectedOption?.label || placeholder }}
      </span>
      <span
        class="custom-select__chevron"
        aria-hidden="true"
      />
    </button>

    <Teleport to="body">
      <Transition name="custom-select-pop">
        <div
          v-if="open"
          :id="listboxId"
          ref="listRef"
          class="custom-select__menu"
          :class="`is-${placement}`"
          :style="menuStyle"
          role="listbox"
          :aria-activedescendant="`${listboxId}-option-${activeIndex}`"
        >
          <button
            v-for="(option, index) in options"
            :id="`${listboxId}-option-${index}`"
            :key="option.value"
            type="button"
            class="custom-select__option"
            :class="{
              'is-active': index === activeIndex,
              'is-selected': option.value === currentValue
            }"
            :disabled="option.disabled"
            :data-index="index"
            role="option"
            :aria-selected="option.value === currentValue"
            @mouseenter="activeIndex = index"
            @click="selectOption(option)"
          >
            <span>{{ option.label }}</span>
          </button>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped lang="scss">
.custom-select {
  position: relative;
  min-width: 0;

  &.is-disabled {
    cursor: not-allowed;
    opacity: 0.58;
  }
}

.custom-select__button {
  position: relative;
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: center;
  width: 100%;
  min-height: 42px;
  padding: 0 12px;
  border: 1px solid var(--border);
  border-radius: 14px;
  background: var(--panel-control);
  color: var(--text);
  text-align: left;
  box-shadow: var(--shadow-control);
  transition:
    border-color 120ms ease,
    background-color 120ms ease,
    box-shadow 120ms ease;

  &:hover:not(:disabled) {
    border-color: var(--border-strong);
    background: var(--interactive-hover-bg);
  }

  &:focus-visible {
    border-color: var(--accent-border-strong);
    outline: none;
    box-shadow: 0 0 0 4px var(--focus-ring);
  }

  &:disabled {
    cursor: not-allowed;
  }
}

.custom-select.is-open .custom-select__button {
  border-color: var(--accent-border-strong);
  box-shadow: 0 0 0 4px var(--focus-ring);
}

.custom-select__value {
  min-width: 0;
  overflow: hidden;
  color: var(--text);
  font-size: 13px;
  font-weight: 650;
  line-height: 1.25;
  text-overflow: ellipsis;
  white-space: nowrap;

  &.is-placeholder {
    color: var(--muted);
  }
}

.custom-select__chevron {
  width: 8px;
  height: 8px;
  margin: 0 2px 3px 12px;
  border-right: 2px solid var(--muted);
  border-bottom: 2px solid var(--muted);
  pointer-events: none;
  transform: rotate(45deg);
  transition: transform 120ms ease;
}

.custom-select.is-open .custom-select__chevron {
  margin-bottom: -3px;
  transform: rotate(225deg);
}

.custom-select__menu {
  position: fixed;
  z-index: 30;
  display: grid;
  gap: 4px;
  max-height: min(240px, 45vh);
  padding: 6px;
  overflow-y: auto;
  border: 1px solid var(--border-strong);
  border-radius: 14px;
  background: var(--panel-solid);
  box-shadow: var(--shadow-popover);
}

.custom-select__option {
  display: flex;
  align-items: center;
  min-height: 34px;
  padding: 7px 10px;
  border: 0;
  border-radius: 10px;
  background: transparent;
  color: var(--text);
  text-align: left;
  box-shadow: none;

  span {
    min-width: 0;
    overflow: hidden;
    font-size: 13px;
    font-weight: 600;
    line-height: 1.25;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &:hover:not(:disabled),
  &.is-active {
    background: var(--interactive-hover-bg);
  }

  &.is-selected {
    color: var(--accent-strong);
    background: var(--interactive-active-bg);
  }

  &:disabled {
    cursor: not-allowed;
    opacity: 0.55;
  }
}

.custom-select-pop-enter-active,
.custom-select-pop-leave-active {
  transition:
    opacity 100ms ease,
    transform 100ms ease;
}

.custom-select-pop-enter-from,
.custom-select-pop-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
