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
        v-if="rules.length > 0"
        class="gesture-table__rows"
      >
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
              <span class="keycap keycap--subtle keycap--mouse">
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
          <IconActionButton
            icon="delete"
            label="删除规则"
            tone="danger"
            class="gesture-table__remove icon-button icon-button--danger"
            @click="$emit('remove', rule.id)"
          />
        </div>
      </div>

      <div
        v-else
        class="gesture-table__empty empty-state empty-state--compact"
      >
        <strong>还没有手势规则</strong>
        <span>点击右上角“添加手势”创建第一条规则，支持快捷键和窗口控制。</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import IconActionButton from './IconActionButton.vue'
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
  border: 1px solid rgba(18, 30, 42, 0.08);
  border-radius: 24px;
  background: linear-gradient(
    180deg,
    rgba(255, 255, 255, 0.78),
    rgba(245, 248, 252, 0.72)
  );
  box-shadow: var(--shadow-soft);
  backdrop-filter: blur(18px) saturate(1.15);

  &__body {
    padding: 10px;
  }

  &__head,
  &__row,
  &__empty {
    margin: 0;
    padding: 0 10px;
  }

  &__head,
  &__row {
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
    text-align: center;
    min-height: 38px;
    color: rgba(101, 113, 128, 0.94);
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    background: linear-gradient(
      180deg,
      rgba(255, 255, 255, 0.78),
      rgba(242, 246, 250, 0.82)
    );
    border-bottom: 1px solid rgba(18, 30, 42, 0.06);
    & > :not(:last-child) {
      margin-left: 11px;
    }
  }

  &__rows {
    display: grid;
    gap: 8px;
  }

  &__row {
    min-height: 66px;
    padding-block: 2px;
    border: 1px solid rgba(18, 30, 42, 0.06);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.8);
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.6);
    transition:
      background-color 140ms ease,
      box-shadow 140ms ease,
      border-color 140ms ease,
      transform 140ms ease;

    &:hover {
      background: rgba(255, 255, 255, 0.94);
      border-color: rgba(18, 30, 42, 0.1);
      box-shadow:
        inset 0 1px 0 rgba(255, 255, 255, 0.72),
        0 10px 24px rgba(18, 30, 42, 0.05);
      transform: translateY(-1px);
    }
  }

  &__cell {
    width: 100%;
    min-height: 40px;
    padding: 10px 12px;
    border: none;
    border-radius: 12px;
    background-color: transparent;
  }

  &__cell--name {
    padding-inline: 14px;
  }

  &__remove {
    flex-shrink: 0;
    justify-self: end;
    opacity: 0;
    transform: translateY(2px);
    pointer-events: none;
    svg {
      width: 16px;
      height: 16px;
    }
  }

  &__row:hover &__remove {
    opacity: 1;
    transform: translateY(0);
    pointer-events: auto;
  }

  &__empty {
    display: grid;
    gap: 4px;
    min-height: 146px;
    align-content: center;
    justify-items: center;
    margin: 4px 0 0;
    padding: 20px 18px;
    border: 1px dashed rgba(18, 30, 42, 0.14);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.68);
    color: var(--muted);
    text-align: center;

    strong {
      color: var(--text);
      font-size: 14px;
      font-weight: 700;
    }

    span {
      max-width: 28rem;
      line-height: 1.5;
    }
  }
}

.gesture-pattern-button {
  display: flex;
  align-items: center;
  gap: 8px;
  min-height: 40px;
  padding: 10px 12px;
  overflow: hidden;
  border: none;
  appearance: none;
  background: transparent;
  color: inherit;
  text-align: left;
  cursor: pointer;
  transition:
    background-color 140ms ease,
    color 140ms ease;

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
  border: none;
  appearance: none;
  background: transparent;
  color: inherit;
  text-align: left;
  white-space: normal;
  cursor: pointer;
  color: var(--text);
  transition:
    background-color 140ms ease,
    color 140ms ease;

  &__empty {
    color: var(--muted);
  }
}
</style>
