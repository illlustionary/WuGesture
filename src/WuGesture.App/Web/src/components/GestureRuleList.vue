<template>
  <div class="gesture-table">
    <header class="gesture-table__head">
      <span>名称</span>
      <span class="gesture">手势</span>
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
            class="gesture-table__cell gesture-pattern-button gesture"
            @click="$emit('edit', rule.id)"
          >
            <span
              class="gesture-pattern-button__mouse"
              aria-hidden="true"
            >
              <AppIcon name="mouse" class="gesture-pattern-button__mouse-icon" />
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
              <span class="keycap keycap--subtle">{{ getActionLabel?.(rule) || '窗口控制' }}</span>
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
import AppIcon from '@/components/AppIcon.vue'
import { ACTION_TYPES } from '@/constants/gestureEditorOptions'
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
  return String(rule?.actionType ?? '').toLowerCase() === ACTION_TYPES.window
}
</script>

<style scoped lang="scss">
.gesture-table {
  display: flex;
  flex-direction: column;
  gap: 0;
  overflow: hidden;
  border: 1px solid var(--border-subtle);
  border-radius: 10px;
  background: linear-gradient(180deg, var(--panel-soft), var(--panel-muted));
  box-shadow: var(--shadow-soft);

  &__body {
    padding: 0;
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
    .gesture {
      flex: 2;
    }
    & > :last-child {
      flex-shrink: 0;
      width: 44px;
    }
  }

  &__head {
    text-align: center;
    min-height: 38px;
    color: var(--muted-strong);
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    background: linear-gradient(180deg, var(--panel-soft), var(--interactive-hover-bg));
    border-bottom: 1px solid var(--border-muted);
    & > :not(:last-child) {
      margin-left: 11px;
    }
  }

  &__rows {
    display: grid;
    gap: 0;
  }

  &__row {
    min-height: 66px;
    padding-block: 2px;
    border: 1px solid var(--border-muted);
    border-radius: 0;
    background: var(--interactive-bg);
    transition:
      background-color 140ms ease,
      border-color 140ms ease;

    &:hover {
      background: var(--interactive-hover-bg);
      border-color: var(--border-strong);
    }
  }

  &__row + &__row {
    border-top-width: 0;
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
    border: 1px solid transparent;
    color: var(--text);
    text-align: center;
    transition:
      border-color 120ms ease,
      box-shadow 120ms ease,
      background-color 120ms ease;

    &:focus {
      outline: none;
      border-color: var(--accent-border-strong);
      background: var(--panel-control);
      box-shadow: 0 0 0 4px var(--focus-ring);
    }
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
    margin: 0;
    padding: 20px 18px;
    border: 1px dashed var(--border-dashed);
    border-radius: 0;
    background: var(--panel-soft);
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
  flex: 2;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-height: 40px;
  padding: 10px 12px;
  overflow: hidden;
  border: none;
  appearance: none;
  background: transparent;
  color: inherit;
  text-align: center;
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
    background: var(--interactive-icon-bg);
    border-radius: 10px;

    &-icon {
      width: 18px;
      height: 18px;
    }
  }

  &__keys {
    display: inline-flex;
    align-items: center;
    justify-content: center;
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
  justify-content: center;
  gap: 6px;
  overflow: hidden;
  border: none;
  appearance: none;
  background: transparent;
  color: inherit;
  text-align: center;
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
