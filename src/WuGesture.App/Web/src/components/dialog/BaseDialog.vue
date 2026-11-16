<script lang="ts">
// 弹窗可以嵌套（例如手势编辑弹窗里再打开程序选择器），只有最后打开的一层拦截 Tab。
const focusTrapStack: symbol[] = []
</script>

<script setup lang="ts">
import { nextTick, onBeforeUnmount, ref, watch } from 'vue'
import AppButton from '@/components/ui/AppButton.vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'

type ClassBinding = string | unknown[] | Record<string, unknown>

const FOCUSABLE_SELECTOR = [
  'a[href]',
  'button:not([disabled])',
  'input:not([disabled]):not([type="hidden"])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[contenteditable="true"]',
  '[tabindex]:not([tabindex="-1"])'
].join(',')

const props = withDefaults(
  defineProps<{
    open: boolean
    title: string
    description?: string
    titleId: string
    showClose?: boolean
    closeOnMask?: boolean
    showActions?: boolean
    cancelText?: string
    confirmText?: string
    confirmTone?: 'primary' | 'danger'
    confirmDisabled?: boolean
    keepMounted?: boolean
    backdropClass?: ClassBinding
    panelClass?: ClassBinding
  }>(),
  {
    description: '',
    showClose: false,
    closeOnMask: true,
    showActions: true,
    cancelText: '取消',
    confirmText: '确认',
    confirmTone: 'primary',
    confirmDisabled: false,
    keepMounted: false,
    backdropClass: '',
    panelClass: ''
  }
)

const emit = defineEmits<{
  close: []
  confirm: []
}>()

const panel = ref<HTMLElement | null>(null)
const trapToken = Symbol('base-dialog')
let restoreTarget: HTMLElement | null = null
let trapActive = false

function listFocusable(container: HTMLElement): HTMLElement[] {
  return Array.from(container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)).filter(element => {
    // 隐藏元素（含 v-show 关闭的面板内容）不进焦点循环。
    return element.getAttribute('aria-hidden') !== 'true' && element.getClientRects().length > 0
  })
}

function focusPanelContent() {
  const container = panel.value

  if (!container || !props.open) {
    return
  }

  const active = document.activeElement

  // 子弹窗可能已经先一步把焦点放进面板（例如快速查找的输入框），这里不再覆盖。
  if (active instanceof HTMLElement && container.contains(active)) {
    return
  }

  const [first] = listFocusable(container)
  ;(first ?? container).focus()
}

function attachFocusTrap() {
  if (trapActive || typeof document === 'undefined') {
    return
  }

  trapActive = true
  focusTrapStack.push(trapToken)
  document.addEventListener('keydown', handleFocusTrapKeydown, true)
}

function detachFocusTrap() {
  if (!trapActive) {
    return
  }

  const index = focusTrapStack.lastIndexOf(trapToken)

  if (index >= 0) {
    focusTrapStack.splice(index, 1)
  }

  document.removeEventListener('keydown', handleFocusTrapKeydown, true)
  trapActive = false
}

function handleFocusTrapKeydown(event: KeyboardEvent) {
  if (event.key !== 'Tab' || !props.open) {
    return
  }

  // 只有最上层的弹窗负责循环，避免嵌套弹窗互相抢焦点。
  if (focusTrapStack[focusTrapStack.length - 1] !== trapToken) {
    return
  }

  const container = panel.value

  if (!container) {
    return
  }

  const focusable = listFocusable(container)

  if (!focusable.length) {
    event.preventDefault()
    container.focus()
    return
  }

  const first = focusable[0]
  const last = focusable[focusable.length - 1]
  const active = document.activeElement

  if (!(active instanceof HTMLElement) || !container.contains(active)) {
    event.preventDefault()
    ;(event.shiftKey ? last : first).focus()
    return
  }

  if (event.shiftKey) {
    if (active === first || active === container) {
      event.preventDefault()
      last.focus()
    }

    return
  }

  if (active === last) {
    event.preventDefault()
    first.focus()
  }
}

function rememberFocus() {
  const active = document.activeElement
  restoreTarget = active instanceof HTMLElement ? active : null
  attachFocusTrap()
  nextTick(focusPanelContent)
}

function releaseFocus() {
  detachFocusTrap()

  const target = restoreTarget
  restoreTarget = null

  nextTick(() => {
    if (!target || !target.isConnected || target === document.body) {
      return
    }

    const active = document.activeElement
    const focusIsFree =
      !active ||
      active === document.body ||
      active === document.documentElement ||
      (active instanceof HTMLElement && Boolean(panel.value?.contains(active)))

    // 关闭过程中如果别的界面已经接管焦点，就不把焦点抢回来。
    if (!focusIsFree) {
      return
    }

    target.focus()
  })
}

watch(
  () => props.open,
  open => {
    if (open) {
      rememberFocus()
      return
    }

    releaseFocus()
  },
  { immediate: true }
)

onBeforeUnmount(() => {
  detachFocusTrap()
  restoreTarget = null
})

function closeFromMask(event: PointerEvent) {
  if (props.closeOnMask && event.target === event.currentTarget) {
    emit('close')
  }
}
</script>

<template>
  <Transition name="base-dialog">
    <div
      v-if="keepMounted || open"
      v-show="open"
      class="base-dialog__backdrop"
      :class="backdropClass"
      @pointerdown="closeFromMask"
    >
      <section
        ref="panel"
        class="base-dialog__panel"
        :class="panelClass"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="titleId"
        tabindex="-1"
        @keydown.esc.prevent="emit('close')"
      >
        <header class="base-dialog__head">
          <div class="base-dialog__heading">
            <h3 :id="titleId">{{ title }}</h3>
            <p v-if="description">{{ description }}</p>
          </div>
          <slot name="header-action">
            <IconActionButton
              v-if="showClose"
              icon="close"
              label="关闭"
              class="base-dialog__close ghost-button"
              tone="muted"
              @click="emit('close')"
            />
          </slot>
        </header>

        <div class="base-dialog__body">
          <slot />
        </div>

        <footer
          v-if="showActions || $slots.actions"
          class="base-dialog__actions"
        >
          <slot name="actions">
            <AppButton
              variant="text"
              @click="emit('close')"
            >
              {{ cancelText }}
            </AppButton>
            <AppButton
              :variant="confirmTone === 'danger' ? 'danger' : 'filled'"
              :disabled="confirmDisabled"
              @click="emit('confirm')"
            >
              {{ confirmText }}
            </AppButton>
          </slot>
        </footer>
      </section>
    </div>
  </Transition>
</template>

<style scoped lang="scss">
.base-dialog__backdrop {
  position: fixed;
  inset: 0;
  z-index: 20;
  display: grid;
  place-items: center;
  padding: var(--md-sys-spacing-xl);
  background: var(--scrim);
}

.base-dialog__panel {
  display: flex;
  flex-direction: column;
  width: min(480px, 100%);
  max-height: calc(100vh - 40px);
  /* 面板自身不滚，改由正文区滚动，保证底栏按钮在矮视口下始终可见。 */
  overflow: hidden;
  padding: var(--md-sys-spacing-xl);
  border: 1px solid var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-extra-large);
  background: var(--md-sys-color-surface-container-high);
  box-shadow: var(--md-sys-elevation-level3);
}

.base-dialog__head {
  display: flex;
  flex: 0 0 auto;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.base-dialog__heading {
  min-width: 0;

  h3 {
    color: var(--md-sys-color-on-surface);
    font-size: var(--md-sys-typescale-title-large-size);
    line-height: var(--md-sys-typescale-title-large-line-height);
    font-weight: var(--md-sys-typescale-title-large-weight);
  }

  p {
    margin-top: 6px;
    color: var(--md-sys-color-on-surface-variant);
    font-size: var(--md-sys-typescale-body-medium-size);
    line-height: var(--md-sys-typescale-body-medium-line-height);
  }
}

:deep(.base-dialog__close) {
  flex: 0 0 auto;
  color: var(--md-sys-color-on-surface-variant);
}

.base-dialog__body {
  min-width: 0;
  min-height: 0;
  margin-top: var(--md-sys-spacing-lg);
  overflow-y: auto;
  overscroll-behavior: contain;
}

.base-dialog__actions {
  display: flex;
  flex: 0 0 auto;
  justify-content: flex-end;
  gap: var(--md-sys-spacing-sm);
  margin-top: var(--md-sys-spacing-xl);
}

.base-dialog-enter-active,
.base-dialog-leave-active {
  transition: background-color var(--md-sys-motion-duration-medium) var(--md-sys-motion-easing-emphasized-decelerate);

  .base-dialog__panel {
    transition:
      opacity var(--md-sys-motion-duration-medium) var(--md-sys-motion-easing-emphasized-decelerate),
      transform var(--md-sys-motion-duration-medium) var(--md-sys-motion-easing-emphasized-decelerate);
  }
}

.base-dialog-enter-from,
.base-dialog-leave-to {
  background: transparent;

  .base-dialog__panel {
    opacity: 0;
    transform: translateY(-16px);
  }
}
</style>
