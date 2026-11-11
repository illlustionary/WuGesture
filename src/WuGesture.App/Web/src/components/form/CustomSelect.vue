<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import AppIcon from '@/components/ui/AppIcon.vue'
import type { SelectOption, SelectOptionValue } from '@/types/ui'

const props = withDefaults(
  defineProps<{
    modelValue?: SelectOptionValue
    value?: SelectOptionValue
    options: SelectOption[]
    placeholder?: string
    disabled?: boolean
  }>(),
  {
    modelValue: '',
    placeholder: '请选择',
    disabled: false
  }
)

const emit = defineEmits<{
  'update:modelValue': [value: SelectOptionValue]
  change: [value: SelectOptionValue]
}>()

const rootRef = ref<HTMLElement | null>(null)
const listRef = ref<HTMLElement | null>(null)
const open = ref(false)
const activeIndex = ref(-1)
const menuStyle = ref<Record<string, string>>({})
const placement = ref<'above' | 'below'>('below')

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
  window.removeEventListener('resize', updateMenuPosition)
  window.removeEventListener('scroll', updateMenuPosition, true)
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

function handleOutsidePointerDown(event: Event) {
  const target = event.target as Node
  if (!rootRef.value?.contains(target) && !listRef.value?.contains(target)) {
    close()
  }
}

function selectOption(option: SelectOption | undefined) {
  if (!option || option.disabled) {
    return
  }

  emit('update:modelValue', option.value)
  emit('change', option.value)
  close()
}

function moveActive(delta: number) {
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

function handleKeydown(event: KeyboardEvent) {
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
  } else if (event.key === 'Tab') {
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
  const button = rootRef.value?.querySelector<HTMLElement>('.custom-select__button')
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
            <span class="custom-select__option-label">{{ option.label }}</span>
            <AppIcon
              v-if="option.value === currentValue"
              name="check"
              class="custom-select__check"
              aria-hidden="true"
            />
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
    opacity: var(--md-sys-state-disabled-content-opacity);
  }
}

/* Exposed Dropdown 触发器沿用 Filled Text Field 造型。 */
.custom-select__button {
  position: relative;
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: center;
  width: 100%;
  min-height: 48px;
  padding: 0 12px 0 16px;
  border: 0;
  border-bottom: 1px solid var(--md-sys-color-on-surface-variant);
  border-radius: var(--md-sys-shape-corner-extra-small) var(--md-sys-shape-corner-extra-small) 0 0;
  background: var(--md-sys-color-surface-container-highest);
  color: var(--md-sys-color-on-surface);
  text-align: left;
  cursor: pointer;
  transition:
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    box-shadow var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover:not(:disabled) {
    background: color-mix(in srgb, var(--md-sys-color-on-surface) 6%, var(--md-sys-color-surface-container-highest));
  }

  &:focus-visible {
    outline: none;
    border-bottom-color: var(--md-sys-color-primary);
    box-shadow: inset 0 -2px 0 0 var(--md-sys-color-primary);
  }

  &:disabled {
    cursor: not-allowed;
  }
}

.custom-select.is-open .custom-select__button {
  border-bottom-color: var(--md-sys-color-primary);
  box-shadow: inset 0 -2px 0 0 var(--md-sys-color-primary);
}

.custom-select__value {
  min-width: 0;
  overflow: hidden;
  color: var(--md-sys-color-on-surface);
  font-size: var(--md-sys-typescale-body-large-size);
  line-height: var(--md-sys-typescale-body-large-line-height);
  text-overflow: ellipsis;
  white-space: nowrap;

  &.is-placeholder {
    color: var(--md-sys-color-on-surface-variant);
  }
}

.custom-select__chevron {
  width: 8px;
  height: 8px;
  margin: 0 6px 3px 12px;
  border-right: 2px solid var(--md-sys-color-on-surface-variant);
  border-bottom: 2px solid var(--md-sys-color-on-surface-variant);
  pointer-events: none;
  transform: rotate(45deg);
  transition: transform var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);
}

.custom-select.is-open .custom-select__chevron {
  margin-bottom: -3px;
  transform: rotate(225deg);
}

.custom-select__menu {
  position: fixed;
  z-index: 30;
  display: grid;
  gap: 0;
  max-height: min(240px, 45vh);
  padding: 8px 0;
  overflow-y: auto;
  border-radius: var(--md-sys-shape-corner-extra-small);
  background: var(--md-sys-color-surface-container);
  box-shadow: var(--md-sys-elevation-level2);
  transform-origin: top center;
}

.custom-select__option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  min-height: 48px;
  padding: 0 16px;
  border: 0;
  border-radius: 0;
  background: transparent;
  color: var(--md-sys-color-on-surface);
  text-align: left;
  cursor: pointer;

  &.is-active {
    background: color-mix(in srgb, var(--md-sys-color-on-surface) 8%, transparent);
  }

  &.is-selected {
    background: var(--md-sys-color-secondary-container);
    color: var(--md-sys-color-on-secondary-container);
  }

  &:disabled {
    cursor: not-allowed;
    opacity: var(--md-sys-state-disabled-content-opacity);
  }
}

.custom-select__option-label {
  min-width: 0;
  overflow: hidden;
  font-size: var(--md-sys-typescale-body-medium-size);
  line-height: var(--md-sys-typescale-body-medium-line-height);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.custom-select__check {
  width: 18px;
  height: 18px;
  flex: 0 0 auto;
}

.custom-select-pop-enter-active,
.custom-select-pop-leave-active {
  transition:
    opacity var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    transform var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);
}

.custom-select-pop-enter-from,
.custom-select-pop-leave-to {
  opacity: 0;
  transform: translateY(-4px) scaleY(0.96);
}
</style>
