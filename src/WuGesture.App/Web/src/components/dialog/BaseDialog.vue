<script setup lang="ts">
import AppButton from '@/components/ui/AppButton.vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'

type ClassBinding = string | unknown[] | Record<string, unknown>

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
  width: min(480px, 100%);
  max-height: calc(100vh - 40px);
  overflow: auto;
  padding: var(--md-sys-spacing-xl);
  border: 1px solid var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-extra-large);
  background: var(--md-sys-color-surface-container-high);
  box-shadow: var(--md-sys-elevation-level3);
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
  margin-top: var(--md-sys-spacing-lg);
}

.base-dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--md-sys-spacing-sm);
  margin-top: var(--md-sys-spacing-xl);
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
