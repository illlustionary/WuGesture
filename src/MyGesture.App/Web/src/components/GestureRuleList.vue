<template>
  <div class="gesture-table">
    <header class="gesture-table__head">
      <span>名称</span>
      <span>手势</span>
      <span>命令</span>
      <span class="gesture-table__head-actions" />
    </header>

    <div class="gesture-table__body">
      <div
        v-for="rule in rules"
        :key="rule.id"
        class="gesture-table__row"
        @dblclick="$emit('edit', rule.id)"
      >
        <input
          :value="rule.actionName"
          class="gesture-table__cell gesture-table__cell--name"
          placeholder="名称"
          spellcheck="false"
          @blur="$emit('rename', rule, $event.target.value)"
        />
        <button
          type="button"
          class="gesture-table__cell gesture-pattern-button"
          @click="$emit('edit', rule.id)"
        >
          <span
            class="gesture-pattern-button__mouse"
            aria-hidden="true"
          >
            <svg
              viewBox="0 0 24 24"
              fill="none"
              aria-hidden="true"
            >
              <rect
                x="7"
                y="3"
                width="10"
                height="18"
                rx="5"
                stroke="currentColor"
                stroke-width="1.5"
              />
              <path
                d="M12 7v3"
                stroke="currentColor"
                stroke-width="1.5"
                stroke-linecap="round"
              />
              <path
                d="M9 10.5h6"
                stroke="currentColor"
                stroke-width="1.5"
                stroke-linecap="round"
              />
            </svg>
          </span>
          <span
            v-if="gestureSegments(rule).length > 0"
            class="gesture-pattern-button__keys"
          >
            <span
              class="keycap keycap--subtle"
              :class="{ 'keycap--mouse': true }"
            >
              {{ mouseButtonLabel(rule) }}
            </span>
            <span
              v-for="segment in gestureSegments(rule)"
              :key="segment"
              class="keycap"
            >
              {{ segment }}
            </span>
          </span>
          <span
            v-else
            class="gesture-pattern-button__empty"
          >
            {{ getGestureMnemonic?.(rule) || '未录制' }}
          </span>
        </button>
        <button
          type="button"
          class="gesture-table__cell command-button"
          @click.stop="$emit('edit', rule.id)"
        >
          <template v-if="isWindowAction(rule)">
            <span class="keycap keycap--subtle">{{
              getActionLabel?.(rule) || '窗口控制'
            }}</span>
          </template>
          <template v-else-if="actionKeys(rule).length > 0">
            <span
              v-for="key in actionKeys(rule)"
              :key="key"
              class="keycap"
            >
              {{ key }}
            </span>
          </template>
          <span
            v-else
            class="command-button__empty"
          >
            {{ getActionLabel?.(rule) || '点击设置' }}
          </span>
        </button>
        <button
          type="button"
          class="gesture-table__remove icon-button icon-button--danger"
          aria-label="删除规则"
          @click="$emit('remove', rule.id)"
        >
          <svg
            viewBox="0 0 24 24"
            fill="none"
            aria-hidden="true"
          >
            <path
              d="M9 4.5h6m-8 3h10m-8 0v9m6-9v9m-8-9h10l-.8 10.2a1.5 1.5 0 0 1-1.5 1.3H9.3a1.5 1.5 0 0 1-1.5-1.3L7 7.5Z"
              stroke="currentColor"
              stroke-width="1.5"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
const DIRECTION_LABELS = {
  Up: '↑',
  Down: '↓',
  Left: '←',
  Right: '→',
  UpLeft: '↖',
  UpRight: '↗',
  DownLeft: '↙',
  DownRight: '↘'
}

const BUTTON_LABELS = {
  right: '右键',
  middle: '中键'
}

defineProps({
  rules: { type: Array, required: true },
  getGestureMnemonic: { type: Function, default: null },
  getActionLabel: { type: Function, default: null }
})

defineEmits(['remove', 'edit', 'rename'])

function gestureSegments(rule) {
  return String(rule?.patternText ?? '')
    .split(/[\s,，]+/)
    .map(part => DIRECTION_LABELS[part.trim()] ?? part.trim())
    .filter(Boolean)
}

function mouseButtonLabel(rule) {
  return BUTTON_LABELS[String(rule?.mouseButton ?? '').toLowerCase()] ?? '右键'
}

function actionKeys(rule) {
  if (isWindowAction(rule)) {
    return []
  }

  return String(rule?.keysText ?? '')
    .split('+')
    .map(part => part.trim())
    .filter(Boolean)
}

function isWindowAction(rule) {
  return String(rule?.actionType ?? '').toLowerCase() === 'window'
}
</script>

<style scoped lang="scss">
.gesture-table {
  display: flex;
  flex-direction: column;
  gap: 0;
  overflow: hidden;
  border: 1px solid var(--border);
  border-radius: 22px;
  background: rgba(255, 255, 255, 0.82);
  box-shadow: var(--shadow-soft);
  backdrop-filter: blur(18px) saturate(1.15);

  &__head,
  &__body {
    display: grid;
  }

  &__head,
  &__row {
    padding: 0 10px;
    display: flex;
    gap: 10px;
    align-items: center;
    & > :not(:last-child) {
      flex: 1;
    }
    & > :last-child {
      flex-shrink: 0;
      width: 44px;
    }
  }

  &__head {
    min-height: 42px;
    color: var(--muted);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    background: rgba(0, 122, 255, 0.08);
    border-bottom: 1px solid rgba(18, 30, 42, 0.08);
    & > :not(:last-child) {
      margin-left: 11px;
    }
  }

  &__row {
    min-height: 60px;
    border-top: 1px solid rgba(18, 30, 42, 0.06);
    transition:
      background-color 140ms ease,
      box-shadow 140ms ease;

    &:hover {
      background: rgba(0, 122, 255, 0.04);
      box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.6);
    }
  }

  &__cell {
    width: 100%;
    min-height: 36px;
    padding: 8px 10px;
    border: 1px solid rgba(18, 30, 42, 0.08);
    border-radius: 14px;
    background: rgba(255, 255, 255, 0.9);
    transition:
      border-color 120ms ease,
      box-shadow 120ms ease,
      background-color 120ms ease;

    &:focus {
      outline: none;
      border-color: rgba(0, 122, 255, 0.52);
      box-shadow: 0 0 0 4px rgba(0, 122, 255, 0.12);
    }
  }

  &__cell--name {
    padding-inline: 12px;
  }

  &__remove {
    flex-shrink: 0;
    justify-self: end;
    opacity: 0;
    transform: translateY(2px);
    transition:
      opacity 120ms ease,
      transform 120ms ease,
      background-color 120ms ease;

    svg {
      width: 16px;
      height: 16px;
    }
  }

  &__row:hover &__remove,
  &__row:focus-within &__remove {
    opacity: 1;
    transform: translateY(0);
  }
}

.gesture-pattern-button {
  display: flex;
  align-items: center;
  gap: 8px;
  min-height: 36px;
  padding: 8px 10px;
  overflow: hidden;
  text-align: left;
  white-space: normal;
  border-top: 0;
  border-bottom: 0;
  cursor: pointer;
  background: rgba(255, 255, 255, 0.9);

  &__mouse {
    display: inline-grid;
    place-items: center;
    width: 28px;
    height: 28px;
    flex: 0 0 auto;
    color: var(--accent-strong);
    background: rgba(0, 122, 255, 0.08);
    border-radius: 10px;

    svg {
      width: 18px;
      height: 18px;
    }
  }

  &__keys {
    display: inline-flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 6px;
    min-width: 0;
  }

  &__empty {
    color: var(--muted);
  }
}

.command-button {
  display: flex;
  align-items: center;
  gap: 6px;
  overflow: hidden;
  text-align: left;
  white-space: normal;
  cursor: pointer;
  color: var(--text);
  background: rgba(255, 255, 255, 0.9);

  &__empty {
    color: var(--muted);
  }
}

.gesture-pattern-button:hover,
.command-button:hover {
  background: rgba(0, 122, 255, 0.08);
}
</style>
