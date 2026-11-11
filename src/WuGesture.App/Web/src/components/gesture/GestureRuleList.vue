<script setup lang="ts">
import AppIcon from '@/components/ui/AppIcon.vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'
import { ACTION_TYPES, WHEEL_DIRECTIONS } from '@/constants/gestureEditorOptions'

interface RuleRow {
  id: string | number
  actionName?: string
  patternText?: string
  wheelModifier?: string
  mouseButton?: string
  keysText?: string
  actionType?: string
  [key: string]: unknown
}

interface DirectionDetails {
  label: string
  rotation: number
}

interface GestureSegment extends DirectionDetails {
  value: string
  isDirection: boolean
  isWheel?: boolean
}

const DIRECTION_DETAILS: Record<string, DirectionDetails> = {
  Up: { label: '向上', rotation: 180 },
  Down: { label: '向下', rotation: 0 },
  Left: { label: '向左', rotation: 90 },
  Right: { label: '向右', rotation: -90 },
  UpLeft: { label: '向左上', rotation: 135 },
  UpRight: { label: '向右上', rotation: -135 },
  DownLeft: { label: '向左下', rotation: 45 },
  DownRight: { label: '向右下', rotation: -45 }
}

const BUTTON_LABELS: Record<string, string> = {
  right: '右键',
  middle: '中键'
}

defineProps<{
  rules: RuleRow[]
  getGestureMnemonic?: ((rule: RuleRow) => string) | null
  getActionLabel?: ((rule: RuleRow) => string) | null
}>()

const emit = defineEmits<{
  remove: [id: string | number]
  edit: [id: string | number]
  rename: [rule: RuleRow, value: string]
}>()

function gestureSegments(rule: RuleRow): GestureSegment[] {
  const segments: GestureSegment[] = String(rule?.patternText ?? '')
    .split(/[\s,，]+/)
    .map(part => part.trim())
    .filter(Boolean)
    .map(value => {
      const direction = DIRECTION_DETAILS[value]

      return direction
        ? { value, ...direction, isDirection: true }
        : { value, label: value, rotation: 0, isDirection: false }
    })

  const modifier = String(rule?.wheelModifier ?? '').toLowerCase()
  if (modifier === WHEEL_DIRECTIONS.up || modifier === WHEEL_DIRECTIONS.down) {
    const isDown = modifier === WHEEL_DIRECTIONS.down
    segments.push({
      value: `wheel-${modifier}`,
      label: isDown ? '滚轮向下' : '滚轮向上',
      rotation: isDown ? 0 : 180,
      isDirection: true,
      isWheel: true
    })
  }

  return segments
}

function mouseButtonLabel(rule: RuleRow): string {
  return BUTTON_LABELS[String(rule?.mouseButton ?? '').toLowerCase()] ?? '右键'
}

function actionKeys(rule: RuleRow): string[] {
  if (isWindowAction(rule)) {
    return []
  }

  return String(rule?.keysText ?? '')
    .split('+')
    .map(part => part.trim())
    .filter(Boolean)
}

function isWindowAction(rule: RuleRow): boolean {
  return String(rule?.actionType ?? '').toLowerCase() === ACTION_TYPES.window
}

function emitRename(rule: RuleRow, event: Event) {
  emit('rename', rule, (event.target as HTMLInputElement).value)
}
</script>

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
          @dblclick="emit('edit', rule.id)"
        >
          <input
            :value="rule.actionName"
            class="gesture-table__cell gesture-table__cell--name"
            placeholder="名称"
            spellcheck="false"
            @blur="emitRename(rule, $event)"
          />
          <button
            type="button"
            class="gesture-table__cell gesture-pattern-button gesture"
            @click="emit('edit', rule.id)"
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
                <template v-if="segment.isWheel">
                  <span class="gesture-pattern-button__wheel-text">滚轮</span>
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
                <template v-else-if="segment.isDirection">
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
            @click.stop="emit('edit', rule.id)"
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
            @click="emit('remove', rule.id)"
          />
        </div>
      </div>

      <div
        v-else
        class="gesture-table__empty empty-state empty-state--compact"
      >
        <strong>还没有手势规则</strong>
        <span>点击右上角“添加手势”创建第一条规则，支持方向手势、方向 + 滚轮修饰、快捷键和窗口控制。</span>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.gesture-table {
  display: flex;
  flex-direction: column;
  gap: 0;
  overflow: hidden;
  border: 1px solid var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-medium);
  background: var(--md-sys-color-surface-container-low);

  &__body {
    padding: 0;
  }

  &__head,
  &__row,
  &__empty {
    margin: 0;
    padding: 0 var(--md-sys-spacing-md);
  }

  &__head,
  &__row {
    display: grid;
    grid-template-columns: minmax(150px, 1fr) minmax(230px, 1.45fr) minmax(170px, 1.25fr) 44px;
    gap: var(--md-sys-spacing-sm);
    align-items: center;
  }

  &__head {
    text-align: left;
    min-height: 40px;
    color: var(--md-sys-color-on-surface-variant);
    font-size: var(--md-sys-typescale-label-medium-size);
    line-height: var(--md-sys-typescale-label-medium-line-height);
    font-weight: var(--md-sys-typescale-label-medium-weight);
    background: var(--md-sys-color-surface-container);
    border-bottom: 1px solid var(--md-sys-color-outline-variant);

    .gesture {
      text-align: center;
    }
  }

  &__head-name,
  &__head-command {
    padding-inline: var(--md-sys-spacing-md);
  }

  &__rows {
    display: grid;
    gap: 0;
  }

  /* List 行：56 高、hover 状态层、底部 1px 分割线。 */
  &__row {
    min-height: var(--md-sys-size-row-single);
    padding-block: 0;
    border: 0;
    border-bottom: 1px solid var(--md-sys-color-outline-variant);
    border-radius: 0;
    background: transparent;
    transition: background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

    &:hover {
      background: color-mix(in srgb, var(--md-sys-color-on-surface) 6%, transparent);
    }

    &:focus-within {
      background: color-mix(in srgb, var(--md-sys-color-on-surface) 4%, transparent);
    }
  }

  &__row:last-child {
    border-bottom: 0;
  }

  &__cell {
    width: 100%;
    min-height: 40px;
    padding: 0 var(--md-sys-spacing-md);
    border: none;
    border-radius: var(--md-sys-shape-corner-small);
    background-color: transparent;
  }

  &__cell--name {
    padding-inline: var(--md-sys-spacing-md);
    border: 1px solid transparent;
    color: var(--md-sys-color-on-surface);
    text-align: left;
    font-weight: var(--md-sys-typescale-label-large-weight);
    transition:
      border-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
      box-shadow var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
      background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

    &:hover {
      background: var(--md-sys-color-surface-container-highest);
    }

    &:focus {
      outline: none;
      border-color: transparent;
      background: var(--md-sys-color-surface-container-highest);
      box-shadow: inset 0 -2px 0 0 var(--md-sys-color-primary);
    }
  }

  :deep(.gesture-table__remove) {
    flex-shrink: 0;
    justify-self: end;
    color: var(--md-sys-color-on-surface-variant);
    opacity: 1;
    pointer-events: auto;

    &:hover,
    &:focus-visible {
      color: var(--md-sys-color-error);
    }

    svg {
      width: 18px;
      height: 18px;
    }
  }

  &__empty {
    display: grid;
    gap: var(--md-sys-spacing-xs);
    min-height: 146px;
    align-content: center;
    justify-items: center;
    margin: var(--md-sys-spacing-md);
    padding: var(--md-sys-spacing-xl) var(--md-sys-spacing-lg);
    border: 1px dashed var(--md-sys-color-outline);
    border-radius: var(--md-sys-shape-corner-medium);
    background: transparent;
    color: var(--md-sys-color-on-surface-variant);
    text-align: center;

    strong {
      color: var(--md-sys-color-on-surface);
      font-size: var(--md-sys-typescale-title-small-size);
      line-height: var(--md-sys-typescale-title-small-line-height);
      font-weight: var(--md-sys-typescale-title-small-weight);
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
  gap: var(--md-sys-spacing-sm);
  min-height: 40px;
  padding: 0 var(--md-sys-spacing-md);
  overflow: hidden;
  border: none;
  appearance: none;
  background: transparent;
  color: inherit;
  text-align: center;
  cursor: pointer;
  transition:
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover {
    background: color-mix(in srgb, var(--md-sys-color-on-surface) 6%, transparent);
  }

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

  &__wheel-text {
    font-size: var(--md-sys-typescale-label-small-size);
    font-weight: var(--md-sys-typescale-label-small-weight);
    letter-spacing: 0.02em;
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
    color: var(--md-sys-color-on-surface-variant);
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
  color: var(--md-sys-color-on-surface);
  text-align: left;
  white-space: normal;
  cursor: pointer;
  transition:
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover {
    background: color-mix(in srgb, var(--md-sys-color-on-surface) 6%, transparent);
  }

  &__empty {
    color: var(--md-sys-color-on-surface-variant);
  }
}
</style>
