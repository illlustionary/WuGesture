<script setup>
import IconActionButton from './IconActionButton.vue'

defineProps({
  open: { type: Boolean, required: true },
  title: { type: String, required: true },
  message: { type: String, default: '' },
  confirmText: { type: String, default: '确认' },
  cancelText: { type: String, default: '取消' },
  tone: {
    type: String,
    default: 'danger',
    validator: value => ['danger', 'primary'].includes(value)
  }
})

const emit = defineEmits(['close', 'confirm'])

function confirm() {
  emit('confirm')
}
</script>

<template>
  <Transition name="confirm-dialog">
    <div
      v-if="open"
      class="modal-backdrop confirm-dialog__backdrop"
      @click.self="emit('close')"
    >
      <section
        class="modal-panel confirm-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-dialog-title"
        tabindex="-1"
        @keydown.esc.prevent="emit('close')"
      >
        <div class="confirm-dialog__head">
          <h3 id="confirm-dialog-title">{{ title }}</h3>
          <IconActionButton
            icon="close"
            label="关闭"
            class="ghost-button"
            tone="muted"
            @click="emit('close')"
          />
        </div>

        <div
          v-if="message || $slots.default"
          class="confirm-dialog__body"
        >
          <slot>{{ message }}</slot>
        </div>

        <div class="confirm-dialog__actions">
          <button
            type="button"
            class="confirm-dialog__button confirm-dialog__button--cancel"
            @click="emit('close')"
          >
            {{ cancelText }}
          </button>
          <button
            type="button"
            class="confirm-dialog__button confirm-dialog__button--confirm"
            :class="`confirm-dialog__button--${tone}`"
            @click="confirm"
          >
            {{ confirmText }}
          </button>
        </div>
      </section>
    </div>
  </Transition>
</template>

<style scoped lang="scss">
.confirm-dialog {
  width: min(420px, calc(100vw - 36px));
  transform-origin: center;
}

.confirm-dialog__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  min-height: 36px;

  h3 {
    min-width: 0;
    font-size: 16px;
    font-weight: 700;
  }
}

.confirm-dialog__body {
  padding-top: 14px;
  color: var(--muted);
  font-size: 13px;
  line-height: 1.55;
}

.confirm-dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: 14px;
  margin-top: 22px;
}

.confirm-dialog__button {
  min-height: 32px;
  min-width: 52px;
  padding: 0;
  border: none;
  border-radius: 0;
  background: transparent;
  box-shadow: none;
  font-weight: 650;
  opacity: 0.9;
  cursor: pointer;
  transition:
    color 120ms ease,
    opacity 120ms ease;

  &:hover,
  &:focus-visible {
    outline: none;
    opacity: 1;
  }

  &--cancel {
    color: var(--muted);
  }

  &--cancel:hover,
  &--cancel:focus-visible {
    color: var(--text);
  }

  &--primary {
    color: var(--accent-strong);
  }

  &--primary:hover,
  &--primary:focus-visible {
    color: var(--accent-strong);
  }

  &--danger {
    color: var(--danger);
  }

  &--danger:hover,
  &--danger:focus-visible {
    color: var(--danger);
  }
}

.confirm-dialog-enter-active,
.confirm-dialog-leave-active {
  transition: background-color 160ms ease;

  .confirm-dialog {
    transition:
      opacity 160ms ease,
      transform 180ms cubic-bezier(0.2, 0.85, 0.24, 1);
  }
}

.confirm-dialog-enter-from,
.confirm-dialog-leave-to {
  background: transparent;

  .confirm-dialog {
    opacity: 0;
    transform: scale(0);
  }
}
</style>
