<script setup>
import { RouterLink } from "vue-router";

defineProps({
  statusText: { type: String, required: true },
  statusState: { type: String, required: true },
  tabs: { type: Array, required: true }
});
</script>

<template>
  <header class="app-bar">
    <div class="app-bar__meta">
      <div
        class="status-badge"
        :data-state="statusState"
      >
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
      <button
        type="button"
        class="menu-button"
        aria-label="更多菜单"
      >
        ☰
      </button>
    </div>
  </header>
</template>

<style scoped lang="scss">
.app-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 14px;
  padding: 14px 18px;
  border: 1px solid var(--border);
  border-radius: 24px;
  background: rgba(255, 255, 255, 0.72);
  backdrop-filter: blur(16px);
  box-shadow: var(--shadow);

  &__meta {
    display: flex;
    align-items: center;
    gap: 12px;
  }
}

.status-badge {
  display: inline-flex;
  align-items: center;
  min-height: 34px;
  padding: 0 12px;
  border: 1px solid var(--border);
  border-radius: 999px;
  color: var(--muted);
  background: rgba(255, 255, 255, 0.72);
  font-size: 13px;
}

.tabs {
  display: inline-flex;
  align-items: stretch;
  gap: 0;
  padding: 3px;
  border: 1px solid var(--border);
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.62);
}

.tab {
  min-width: 94px;
  min-height: 34px;
  padding: 0 18px;
  border: 0;
  border-radius: 999px;
  color: var(--muted);
  background: transparent;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  justify-content: center;

  &.active {
    color: var(--accent-strong);
    background: linear-gradient(180deg, #ffffff, #edf4f8);
    box-shadow: 0 10px 18px rgba(18, 30, 42, 0.07);
  }

  &:hover {
    background: rgba(29, 81, 109, 0.1);
  }
}

.menu-button {
  width: 36px;
  height: 36px;
  border-radius: 999px;
  color: var(--accent-strong);
  background: linear-gradient(180deg, #ffffff, #edf3f7);
  box-shadow: 0 8px 18px rgba(18, 30, 42, 0.08);
}

@media (max-width: 1100px) {
  .app-bar {
    align-items: flex-start;
    flex-direction: column;

    &__meta {
      width: 100%;
      flex-wrap: wrap;
    }
  }

  .tabs {
    width: 100%;
  }
}

@media (max-width: 720px) {
  .app-bar {
    flex-direction: column;
  }

  .tabs {
    width: 100%;
  }

  .tab {
    flex: 1 1 0;
    min-width: 0;
  }
}
</style>
