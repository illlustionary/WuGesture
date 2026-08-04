<script setup>
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'

const props = defineProps({
  text: {
    type: String,
    required: true
  },
  delay: {
    type: Number,
    default: 500
  },
  offset: {
    type: Number,
    default: 12
  },
  maxWidth: {
    type: Number,
    default: 320
  },
  disabled: {
    type: Boolean,
    default: false
  }
})

const triggerRef = ref(null)
const bubbleRef = ref(null)
const isHovered = ref(false)
const isOpen = ref(false)
const bubbleVisible = ref(false)
const bubbleStyle = ref({
  top: '0px',
  left: '0px'
})

let openTimer = 0
let updateFrame = 0
let listenersAttached = false

const tooltipId = `hover-bubble-${Math.random().toString(36).slice(2, 10)}`
const bubblePlacement = ref('top')
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
  let placement = 'top'

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
    maxWidth: `min(${bubbleMaxWidth.value}, calc(100vw - 20px))`,
    '--hover-bubble-arrow-left': `${Math.round(Math.max(18, Math.min(triggerCenter - left, bubbleRect.width - 18)))}px`
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
  padding: 10px 12px;
  border: 1px solid var(--border);
  border-radius: 10px;
  background-color: var(--panel-solid);
  box-shadow: var(--shadow-popover);
  color: var(--text);
  font-size: 13px;
  font-weight: 600;
  line-height: 1.45;
  white-space: normal;
  word-break: break-word;
  pointer-events: none;
  transform: translateY(-3px);
  transition:
    opacity 120ms ease,
    transform 120ms ease;
  opacity: 0;
  visibility: hidden;

  &::after {
    content: '';
    position: absolute;
    left: var(--hover-bubble-arrow-left, 24px);
    width: 10px;
    height: 10px;
    background: inherit;
    border-left: 1px solid var(--border);
    border-top: 1px solid var(--border);
    transform: translateX(-50%) rotate(45deg);
  }

  &[data-placement='bottom'] {
    transform: translateY(3px);
  }

  &[data-placement='bottom']::after {
    top: -5px;
  }

  &[data-placement='top']::after {
    bottom: -5px;
    transform: translateX(-50%) rotate(225deg);
  }
}

.hover-bubble.is-visible {
  visibility: visible;
  opacity: 1;
}
</style>
