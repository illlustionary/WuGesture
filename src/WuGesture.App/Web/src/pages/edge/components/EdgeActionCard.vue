<script setup>
import { EDGE_TRIGGER_TYPES } from '@/constants/gestureEditorOptions'

defineProps({
  action: { type: Object, required: true },
  title: { type: String, required: true },
  subtitle: { type: String, required: true },
  name: { type: String, required: true },
  summary: { type: String, required: true }
})

const emit = defineEmits(['open'])
</script>

<template>
  <article
    class="edge-card"
    :class="{ 'is-disabled': !action.enabled }"
    role="button"
    tabindex="0"
    @click="emit('open', action)"
    @keydown.enter.prevent="emit('open', action)"
  >
    <header class="edge-card__head">
      <span
        class="edge-status"
        :class="{ 'is-enabled': action.enabled }"
      />
      <div>
        <strong>{{ title }}</strong>
        <small>{{ subtitle }}</small>
      </div>
    </header>

    <div class="edge-card__summary">
      <span>{{ name }}</span>
      <strong>{{ summary }}</strong>
      <small v-if="action.triggerType === EDGE_TRIGGER_TYPES.friction"> 摩擦 {{ action.frictionCount }} 次 </small>
    </div>
  </article>
</template>

<style scoped lang="scss">
@use '../../../styles/tokens/state' as *;

.edge-card {
  @include state-layer(var(--md-sys-color-on-surface));

  display: grid;
  gap: var(--md-sys-spacing-md);
  min-width: 0;
  padding: var(--md-sys-spacing-md);
  border: 1px solid var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-medium);
  background: var(--md-sys-color-surface-container);
  text-align: left;
  cursor: pointer;

  &:hover,
  &:focus-visible {
    border-color: var(--md-sys-color-outline);
    outline: none;
  }

  &:focus-visible {
    box-shadow: 0 0 0 2px var(--md-sys-color-primary);
  }

  &.is-disabled {
    opacity: var(--md-sys-state-disabled-content-opacity);
  }

  &__head {
    display: flex;
    align-items: center;
    gap: var(--md-sys-spacing-sm);

    strong,
    small {
      display: block;
    }

    strong {
      color: var(--md-sys-color-on-surface);
      font-size: var(--md-sys-typescale-title-small-size);
      line-height: var(--md-sys-typescale-title-small-line-height);
      font-weight: var(--md-sys-typescale-title-small-weight);
    }

    small {
      color: var(--md-sys-color-on-surface-variant);
      font-size: var(--md-sys-typescale-body-small-size);
      line-height: var(--md-sys-typescale-body-small-line-height);
    }
  }

  &__summary {
    display: grid;
    gap: var(--md-sys-spacing-xs);

    span {
      color: var(--md-sys-color-on-surface-variant);
      font-size: var(--md-sys-typescale-label-large-size);
      line-height: var(--md-sys-typescale-label-large-line-height);
    }

    strong {
      color: var(--md-sys-color-on-surface);
      font-size: var(--md-sys-typescale-body-medium-size);
      line-height: var(--md-sys-typescale-body-medium-line-height);
      font-weight: 600;
    }

    small {
      color: var(--md-sys-color-on-surface-variant);
      font-size: var(--md-sys-typescale-body-small-size);
      line-height: var(--md-sys-typescale-body-small-line-height);
    }
  }
}

.edge-status {
  flex: 0 0 auto;
  width: 12px;
  height: 12px;
  border-radius: var(--md-sys-shape-corner-full);
  background: var(--md-sys-color-outline-variant);

  &.is-enabled {
    background: var(--md-sys-color-primary);
    box-shadow: 0 0 0 4px var(--md-sys-color-primary-container);
  }
}
</style>
