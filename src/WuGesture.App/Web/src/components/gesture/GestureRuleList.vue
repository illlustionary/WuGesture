<template>
  <div class="gesture-table">
    <header class="gesture-table__head">
      <span class="gesture-table__head-name">名称</span>
      <span class="gesture">手势</span>
      <span class="gesture-table__head-command">命令</span>
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
              v-if="gestureSegments(rule).length > 0"
              class="gesture-pattern-button__keys"
            >
              <span class="keycap keycap--subtle keycap--mouse">
                {{ mouseButtonLabel(rule) }}
              </span>
              <span
                v-for="(segment, index) in gestureSegments(rule)"
                :key="`${rule.id}-${segment.value}-${index}`"
                class="keycap"
              >
                <template v-if="segment.isDirection">
                  <AppIcon
                    name="arrow"
                    class="gesture-pattern-button__arrow"
                    :style="{ '--gesture-arrow-rotation': `${segment.rotation}deg` }"
                    aria-hidden="true"
                  />
                  <span class="gesture-pattern-button__screen-reader-text">
                    {{ segment.label }}
                  </span>
                </template>
                <template v-else>
                  {{ segment.label }}
                </template>
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
import AppIcon from '@/components/ui/AppIcon.vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'
import { ACTION_TYPES } from '@/constants/gestureEditorOptions'
const DIRECTION_DETAILS = {
  Up: { label: '向上', rotation: 180 },
  Down: { label: '向下', rotation: 0 },
  Left: { label: '向左', rotation: 90 },
  Right: { label: '向右', rotation: -90 },
  UpLeft: { label: '向左上', rotation: 135 },
  UpRight: { label: '向右上', rotation: -135 },
  DownLeft: { label: '向左下', rotation: 45 },
  DownRight: { label: '向右下', rotation: -45 }
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
    .map(part => part.trim())
    .filter(Boolean)
    .map(value => {
      const direction = DIRECTION_DETAILS[value]

      return direction ? { value, ...direction, isDirection: true } : { value, label: value, isDirection: false }
    })
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
  border-radius: var(--radius-md);
  background: var(--panel-soft);

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
    display: grid;
    grid-template-columns: minmax(150px, 1fr) minmax(230px, 1.45fr) minmax(170px, 1.25fr) 44px;
    gap: 8px;
    align-items: center;
  }

  &__head {
    text-align: left;
    min-height: 38px;
    color: var(--muted-strong);
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    background: var(--panel-muted);
    border-bottom: 1px solid var(--border-muted);

    .gesture {
      text-align: center;
    }
  }

  &__head-name,
  &__head-command {
    padding-inline: 12px;
  }

  &__rows {
    display: grid;
    gap: 0;
  }

  &__row {
    min-height: 60px;
    padding-block: 1px;
    border: 0;
    border-bottom: 1px solid var(--border-muted);
    border-radius: 0;
    background: var(--interactive-bg);
    transition:
      background-color 140ms ease,
      border-color 140ms ease;

    &:hover {
      background: var(--interactive-hover-bg);
      box-shadow: inset 2px 0 0 var(--accent);
    }
  }

  &__row:last-child {
    border-bottom: 0;
  }

  &__cell {
    width: 100%;
    min-height: 40px;
    padding: 8px 12px;
    border: none;
    border-radius: var(--radius-md);
    background-color: transparent;
  }

  &__cell--name {
    padding-inline: 12px;
    border: 1px solid transparent;
    color: var(--text);
    text-align: left;
    font-weight: 600;
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

  :deep(.gesture-table__remove) {
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

  &__row:hover :deep(.gesture-table__remove),
  &__row:focus-within :deep(.gesture-table__remove) {
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

  &__keys {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    flex-wrap: wrap;
    gap: 6px;
    min-width: 0;
  }

  &__arrow {
    display: block;
    width: 14px;
    height: 14px;
    fill: currentColor;
    transform: rotate(var(--gesture-arrow-rotation));
  }

  &__screen-reader-text {
    position: absolute;
    width: 1px;
    height: 1px;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
  }

  &__empty {
    color: var(--muted);
  }
}

.command-button {
  display: flex;
  align-items: center;
  justify-content: flex-start;
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
