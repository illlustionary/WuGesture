<script setup>
import IconActionButton from './IconActionButton.vue'
import { RouterLink } from 'vue-router'

defineProps({
  statusText: { type: String, required: true },
  statusState: { type: String, required: true },
  tabs: { type: Array, required: true }
})

defineEmits(['open-settings'])
</script>

<template>
  <header class="app-bar surface-card">
    <div
      class="status-badge"
      :data-state="statusState"
    >
      <span class="status-badge__dot" />
      {{ statusText }}
    </div>
    <nav
      class="tabs"
      aria-label="规则作用域"
    >
      <RouterLink
        v-for="tab in tabs"
        :key="tab.to"
        :to="tab.to"
        class="tab"
        active-class="active"
      >
        {{ tab.label }}
      </RouterLink>
    </nav>
    <IconActionButton
      icon="setting"
      label="更多设置"
      class="menu-button"
      @click="$emit('open-settings')"
    />
  </header>
</template>

<style scoped lang="scss">
.app-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
  padding: 10px 12px;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-height: 30px;
  padding: 0 10px 0 8px;
  border: 1px solid var(--border);
  border-radius: 999px;
  color: var(--muted);
  background: rgba(255, 255, 255, 0.7);
  box-shadow: 0 8px 16px rgba(18, 30, 42, 0.05);
  font-size: 12px;
  font-weight: 600;

  &__dot {
    width: 8px;
    height: 8px;
    border-radius: 999px;
    background: currentColor;
    opacity: 0.45;
  }

  &[data-state='running'] {
    color: var(--accent-strong);

    .status-badge__dot {
      opacity: 1;
      background: var(--accent);
      box-shadow: 0 0 0 4px var(--focus-ring);
    }
  }
}

.tabs {
  position: relative;
  flex: 1;
  display: flex;
  gap: 4px;
  min-width: 0;
  padding: 4px;
}

.tab {
  flex: 1;
  min-width: 0;
  min-height: 32px;
  padding: 0 14px;
  border: 0;
  color: var(--muted);
  background: transparent;
  text-decoration: none;
  display: flex;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  position: relative;
  border-radius: 10px;
  transition:
    background-color 120ms ease,
    color 120ms ease;

  &:hover {
    color: var(--text);
    background: var(--interactive-hover-bg);
  }

  &.active {
    color: var(--accent-strong);
    // border-bottom: 2px solid var(--accent-strong);
    &::after {
      background-color: var(--accent-strong);
      transform: scale(1);
    }
  }
  &::after {
    content: '';
    position: absolute;
    bottom: 0;
    left: 0;
    width: 100%;
    height: 2px;
    background-color: transparent;
    transition: 0.3s ease-in-out;
    transform: scaleX(0);
    transform-origin: center center;
  }
}

.menu-button {
  color: var(--muted);

  &:hover {
    color: var(--text);
  }
}
</style>
