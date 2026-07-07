<script setup>
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
      <small v-if="action.triggerType === 'friction'">
        摩擦 {{ action.frictionCount }} 次
      </small>
    </div>
  </article>
</template>

<style scoped lang="scss">
.edge-card {
  display: grid;
  gap: 12px;
  min-width: 0;
  padding: 14px;
  border: 1px solid rgba(18, 30, 42, 0.08);
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.78);
  text-align: left;
  cursor: pointer;

  &:hover,
  &:focus-visible {
    border-color: rgba(29, 81, 109, 0.26);
    background: #fff;
    outline: none;
  }

  &.is-disabled {
    opacity: 0.62;
  }

  &__head {
    display: flex;
    align-items: center;
    gap: 10px;

    strong,
    small {
      display: block;
    }

    strong {
      font-size: 14px;
    }

    small {
      color: var(--muted);
      font-size: 12px;
    }
  }

  &__summary {
    display: grid;
    gap: 4px;

    span {
      color: var(--text);
      font-size: 13px;
    }

    strong {
      font-size: 14px;
      font-weight: 700;
    }

    small {
      color: var(--muted);
      font-size: 12px;
    }
  }
}

.edge-status {
  width: 12px;
  height: 12px;
  border-radius: 999px;
  flex: 0 0 auto;
  background: rgba(101, 113, 128, 0.32);

  &.is-enabled {
    background: var(--accent);
    box-shadow: 0 0 0 4px rgba(29, 81, 109, 0.1);
  }
}
</style>
