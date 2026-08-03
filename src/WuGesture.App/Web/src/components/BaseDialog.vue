<script setup>
import IconActionButton from './IconActionButton.vue'

const props = defineProps({
  open: { type: Boolean, required: true },
  title: { type: String, required: true },
  description: { type: String, default: '' },
  titleId: { type: String, required: true },
  showClose: { type: Boolean, default: false },
  closeOnMask: { type: Boolean, default: true },
  showActions: { type: Boolean, default: true },
  cancelText: { type: String, default: '取消' },
  confirmText: { type: String, default: '确认' },
  confirmTone: {
    type: String,
    default: 'primary',
    validator: value => ['primary', 'danger'].includes(value)
  },
  confirmDisabled: { type: Boolean, default: false },
  keepMounted: { type: Boolean, default: false },
  backdropClass: { type: [String, Array, Object], default: '' },
  panelClass: { type: [String, Array, Object], default: '' }
})

const emit = defineEmits(['close', 'confirm'])

function closeFromMask(event) {
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
            <button
              type="button"
              class="base-dialog__button base-dialog__button--cancel"
              @click="emit('close')"
            >
              {{ cancelText }}
            </button>
            <button
              type="button"
              class="base-dialog__button base-dialog__button--confirm"
              :class="`base-dialog__button--${confirmTone}`"
              :disabled="confirmDisabled"
              @click="emit('confirm')"
            >
              {{ confirmText }}
            </button>
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
  padding: 20px;
  background: var(--scrim);
}

.base-dialog__panel {
  width: min(480px, 100%);
  max-height: calc(100vh - 40px);
  overflow: auto;
  padding: 16px;
  border: 1px solid var(--border-medium);
  border-radius: 16px;
  background: var(--panel);
  box-shadow: var(--shadow-float);
  backdrop-filter: blur(28px) saturate(1.2);
}

.base-dialog__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.base-dialog__heading {
  min-width: 0;

  h3 {
    font-size: 16px;
    font-weight: 700;
  }

  p {
    margin-top: 4px;
    color: var(--muted);
    font-size: 13px;
    line-height: 1.5;
  }
}

:deep(.base-dialog__close) {
  flex: 0 0 auto;
  color: var(--muted);
}

.base-dialog__body {
  min-width: 0;
  margin-top: 12px;
}

.base-dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 20px;
}

.base-dialog__button {
  min-width: 72px;
  min-height: 34px;
  padding: 0 14px;
  border: 1px solid transparent;
  border-radius: 8px;
  cursor: pointer;
  transition: opacity 120ms ease;
  font-size: 13px;
  &:hover,
  &:focus-visible {
    opacity: 0.76;
  }

  &:focus-visible {
    outline: 2px solid var(--focus-ring);
    outline-offset: 2px;
  }

  &:disabled {
    cursor: not-allowed;
    opacity: 0.48;
  }

  &--cancel {
    border-color: var(--border);
    background: var(--interactive-bg);
    color: var(--text);
  }

  &--primary {
    background: var(--accent-strong);
    color: var(--text-inverse);
  }

  &--danger {
    background: var(--danger);
    color: var(--text-inverse);
  }
}

.base-dialog-enter-active,
.base-dialog-leave-active {
  transition: background-color 180ms ease;

  .base-dialog__panel {
    transition:
      opacity 180ms ease,
      transform 180ms ease;
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
