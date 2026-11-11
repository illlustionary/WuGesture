<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'

const props = withDefaults(
  defineProps<{
    text: string
    delay?: number
    offset?: number
    maxWidth?: number
    disabled?: boolean
  }>(),
  {
    delay: 500,
    offset: 12,
    maxWidth: 320,
    disabled: false
  }
)

const triggerRef = ref<HTMLElement | null>(null)
const bubbleRef = ref<HTMLElement | null>(null)
const isHovered = ref(false)
const isOpen = ref(false)
const bubbleVisible = ref(false)
const bubbleStyle = ref<Record<string, string>>({
  top: '0px',
  left: '0px'
})

let openTimer = 0
let updateFrame = 0
let listenersAttached = false

const tooltipId = `hover-bubble-${Math.random().toString(36).slice(2, 10)}`
const bubblePlacement = ref<'top' | 'bottom'>('top')
const bubbleMaxWidth = computed(() => `${props.maxWidth}px`)

function clearOpenTimer() {
  if (openTimer) {
    window.clearTimeout(openTimer)
    openTimer = 0
  }
}

function detachListeners() {
  if (!listenersAttached) {
    return
  }

  window.removeEventListener('resize', scheduleUpdate)
  window.removeEventListener('scroll', scheduleUpdate, true)
  listenersAttached = false
}

function hideBubble() {
  isHovered.value = false
  isOpen.value = false
  bubbleVisible.value = false
  clearOpenTimer()
  detachListeners()
}

function scheduleUpdate() {
  if (updateFrame) {
    window.cancelAnimationFrame(updateFrame)
  }

  updateFrame = window.requestAnimationFrame(() => {
    updateFrame = 0
    updateBubblePosition()
  })
}

function updateBubblePosition() {
  const triggerEl = triggerRef.value
  const bubbleEl = bubbleRef.value

  if (!triggerEl || !bubbleEl) {
    return
  }

  const triggerRect = triggerEl.getBoundingClientRect()
  const bubbleRect = bubbleEl.getBoundingClientRect()
  const viewportWidth = window.innerWidth
  const viewportHeight = window.innerHeight
  const gap = props.offset
  const margin = 10

  let top = triggerRect.top - bubbleRect.height - gap
  let placement: 'top' | 'bottom' = 'top'

  if (top < margin) {
    top = triggerRect.bottom + gap
    placement = 'bottom'
  }

  if (top + bubbleRect.height > viewportHeight - margin) {
    const fallbackTop = triggerRect.top - bubbleRect.height - gap
    if (fallbackTop >= margin) {
      top = fallbackTop
      placement = 'top'
    } else {
      top = Math.max(margin, viewportHeight - bubbleRect.height - margin)
    }
  }

  const triggerCenter = triggerRect.left + triggerRect.width / 2
  let left = triggerCenter - bubbleRect.width / 2
  left = Math.max(margin, Math.min(left, viewportWidth - bubbleRect.width - margin))
  bubblePlacement.value = placement

  bubbleStyle.value = {
    top: `${Math.round(top)}px`,
    left: `${Math.round(left)}px`,
    maxWidth: `min(${bubbleMaxWidth.value}, calc(100vw - 20px))`
  }
  bubbleVisible.value = true
}

function openBubble() {
  if (props.disabled || !isHovered.value) {
    return
  }

  isOpen.value = true
  nextTick().then(() => {
    updateBubblePosition()
    window.addEventListener('resize', scheduleUpdate)
    window.addEventListener('scroll', scheduleUpdate, true)
    listenersAttached = true
  })
}

function queueOpen() {
  if (props.disabled || !props.text?.trim()) {
    return
  }

  isHovered.value = true
  clearOpenTimer()
  openTimer = window.setTimeout(openBubble, props.delay)
}

/** 键盘聚焦时立即显示，保证 Tab 浏览也能读到提示。 */
function openOnFocus() {
  if (props.disabled || !props.text?.trim() || !isFocusVisible(triggerRef.value)) {
    return
  }

  isHovered.value = true
  clearOpenTimer()
  openBubble()
}

function isFocusVisible(element: HTMLElement | null) {
  if (!element) {
    return false
  }

  try {
    return element.matches(':focus-visible')
  } catch {
    return false
  }
}

watch(
  () => props.text,
  () => {
    if (!props.text?.trim()) {
      hideBubble()
    }
  }
)

watch(
  () => props.disabled,
  disabled => {
    if (disabled) {
      hideBubble()
    }
  }
)

onBeforeUnmount(() => {
  clearOpenTimer()
  detachListeners()
  if (updateFrame) {
    window.cancelAnimationFrame(updateFrame)
  }
})
</script>

<template>
  <span
    ref="triggerRef"
    class="hover-bubble-trigger"
    :aria-describedby="isOpen ? tooltipId : undefined"
    @pointerenter="queueOpen"
    @pointerleave="hideBubble"
    @focusin="openOnFocus"
    @focusout="hideBubble"
  >
    <slot>{{ text }}</slot>

    <Teleport to="body">
      <span
        v-if="isOpen"
        :id="tooltipId"
        ref="bubbleRef"
        class="hover-bubble"
        :class="{ 'is-visible': bubbleVisible }"
        :data-placement="bubblePlacement"
        :style="bubbleStyle"
        role="tooltip"
      >
        {{ text }}
      </span>
    </Teleport>
  </span>
</template>

<style scoped lang="scss">
.hover-bubble-trigger {
  display: inline-flex;
  min-width: 0;
  align-items: center;
}

.hover-bubble {
  position: fixed;
  z-index: 40;
  display: inline-flex;
  padding: 5px 8px;
  border-radius: var(--md-sys-shape-corner-extra-small);
  background-color: var(--md-sys-color-inverse-surface);
  box-shadow: var(--md-sys-elevation-level2);
  color: var(--md-sys-color-inverse-on-surface);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
  font-weight: var(--md-sys-typescale-body-small-weight);
  white-space: normal;
  word-break: break-word;
  pointer-events: none;
  transform: translateY(-3px);
  transition:
    opacity var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    transform var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);
  opacity: 0;
  visibility: hidden;

  &[data-placement='bottom'] {
    transform: translateY(3px);
  }
}

.hover-bubble.is-visible {
  visibility: visible;
  opacity: 1;
  transform: translateY(0);
}
</style>
