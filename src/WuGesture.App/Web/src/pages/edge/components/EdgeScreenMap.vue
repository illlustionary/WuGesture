<script setup lang="ts">
import { computed } from 'vue'
import AppIcon from '@/components/ui/AppIcon.vue'
import HoverBubble from '@/components/ui/HoverBubble.vue'
import {
  buildEdgeScreenHotspots,
  findEdgeScreenAction,
  type EdgeScreenActionLike,
  type EdgeScreenHotspot,
  type EdgeScreenTriggerType
} from '@/pages/edge/edgeScreenHotspots'

const props = defineProps<{
  triggerType: EdgeScreenTriggerType
  actions: EdgeScreenActionLike[]
  actionSummary: (action: EdgeScreenActionLike) => string
}>()

const emit = defineEmits<{ open: [action: EdgeScreenActionLike] }>()

const hotspots = computed(() => buildEdgeScreenHotspots(props.triggerType))
const actionsByHotspot = computed(
  () => new Map(hotspots.value.map(hotspot => [hotspot.key, findEdgeScreenAction(props.actions, hotspot)]))
)
const enabledCount = computed(() => props.actions.filter(action => action.enabled).length)

function actionFor(hotspot: EdgeScreenHotspot) {
  return actionsByHotspot.value.get(hotspot.key)
}

function hotspotTooltip(hotspot: EdgeScreenHotspot) {
  const head = [hotspot.label, hotspot.wheelLabel].filter(Boolean).join(' · ')
  const action = actionFor(hotspot)
  if (!action) {
    return `${head} · 未设置`
  }

  if (!action.enabled) {
    return `${head} · 未启用，点击设置`
  }

  const parts = [head, props.actionSummary(action)]
  if (action.triggerType === 'friction' && action.frictionCount) {
    parts.push(`摩擦 ${action.frictionCount} 次`)
  }
  return parts.join(' · ')
}

function hotspotClass(hotspot: EdgeScreenHotspot) {
  return [
    `edge-map__spot--${hotspot.side}`,
    {
      'is-wheel-up': hotspot.wheelDirection === 'up',
      'is-wheel-down': hotspot.wheelDirection === 'down'
    }
  ]
}

function buttonClass(hotspot: EdgeScreenHotspot) {
  return [
    `edge-map__button--${hotspot.triggerType}`,
    {
      'is-enabled': actionFor(hotspot)?.enabled,
      'is-vertical': hotspot.triggerType === 'friction' && (hotspot.side === 'left' || hotspot.side === 'right')
    }
  ]
}

function cornerLabel(label: string) {
  return label.replace('角', '')
}

function openHotspot(hotspot: EdgeScreenHotspot) {
  const action = actionFor(hotspot)
  if (action) {
    emit('open', action)
  }
}
</script>

<template>
  <section class="edge-map section-card">
    <header class="edge-map__head">
      <div class="edge-map__heading">
        <h3 class="section-title">屏幕位置</h3>
        <p class="edge-map__hint">点击屏幕示意图上的位置进行设置</p>
      </div>
      <span class="edge-map__count">{{ enabledCount }} / {{ actions.length }} 已启用</span>
    </header>

    <div class="edge-map__stage">
      <svg
        class="edge-map__monitor"
        viewBox="0 0 320 214"
        aria-hidden="true"
        focusable="false"
      >
        <rect
          class="edge-map__bezel"
          x="6"
          y="6"
          width="308"
          height="176"
          rx="12"
        />
        <rect
          class="edge-map__screen"
          x="16"
          y="16"
          width="288"
          height="156"
          rx="4"
        />
        <path
          class="edge-map__stand"
          d="M146 182h28v12h-28z"
        />
        <rect
          class="edge-map__base"
          x="108"
          y="194"
          width="104"
          height="8"
          rx="4"
        />
      </svg>

      <div
        v-for="hotspot in hotspots"
        :key="hotspot.key"
        class="edge-map__spot"
        :class="hotspotClass(hotspot)"
      >
        <HoverBubble
          :text="hotspotTooltip(hotspot)"
          :max-width="240"
        >
          <button
            type="button"
            class="edge-map__button"
            :class="buttonClass(hotspot)"
            :aria-label="hotspotTooltip(hotspot)"
            @click="openHotspot(hotspot)"
          >
            <template v-if="hotspot.triggerType === 'corner'">
              <span class="edge-map__corner-label">{{ cornerLabel(hotspot.label) }}</span>
            </template>

            <template v-else-if="hotspot.triggerType === 'friction'">
              <AppIcon
                name="move-horizontal"
                :size="16"
                class="edge-map__edge-icon"
              />
              <span class="edge-map__friction-count">×{{ actionFor(hotspot)?.frictionCount ?? 4 }}</span>
            </template>

            <template v-else>
              <span
                class="edge-map__arrow"
                aria-hidden="true"
                >{{ hotspot.wheelDirection === 'down' ? '↓' : '↑' }}</span
              >
            </template>
          </button>
        </HoverBubble>
      </div>
    </div>

    <p class="edge-map__note">对每一块显示器生效；示意图只表示触发位置，不代表实际触发范围。</p>
  </section>
</template>

<style scoped lang="scss">
.edge-map {
  display: flex;
  min-height: 100%;
  flex-direction: column;
  gap: var(--md-sys-spacing-lg);

  &__head {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: var(--md-sys-spacing-md);
  }

  &__heading {
    display: grid;
    min-width: 0;
    gap: var(--md-sys-spacing-xs);
  }

  &__hint,
  &__note {
    margin: 0;
    color: var(--md-sys-color-on-surface-variant);
    font-size: var(--md-sys-typescale-body-small-size);
    line-height: var(--md-sys-typescale-body-small-line-height);
  }

  &__note {
    margin-top: auto;
  }

  &__count {
    flex: 0 0 auto;
    padding: 2px 10px;
    border-radius: var(--md-sys-shape-corner-full);
    background: var(--md-sys-color-surface-container-highest);
    color: var(--md-sys-color-on-surface-variant);
    font-size: var(--md-sys-typescale-label-small-size);
    line-height: 24px;
    white-space: nowrap;
  }

  &__stage {
    position: relative;
    width: 100%;
    max-width: 560px;
    margin: 0 auto;

    // 用百分比内边距撑出 320×214 的比例：`aspect-ratio` 在网格自动行里不参与
    // 固有高度计算，单列堆叠时卡片会塌陷成标题高度，示意图溢出压住下方卡片。
    &::before {
      display: block;
      padding-top: 66.875%; // 214 / 320
      content: '';
    }
  }

  &__monitor {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
  }

  &__bezel {
    fill: var(--md-sys-color-surface-container-highest);
    stroke: var(--md-sys-color-outline-variant);
    stroke-width: 2;
  }

  &__screen {
    fill: var(--md-sys-color-surface-container-lowest);
  }

  &__stand,
  &__base {
    fill: var(--md-sys-color-surface-container-highest);
  }
}

.edge-map__spot {
  position: absolute;
  display: flex;
  transform: translate(-50%, -50%);

  // 内屏在 320×214 画布中的位置：x 16..304，y 16..172。
  &--top-left {
    top: 7.48%;
    left: 5%;
  }

  &--top-right {
    top: 7.48%;
    left: 95%;
  }

  &--bottom-left {
    top: 80.37%;
    left: 5%;
  }

  &--bottom-right {
    top: 80.37%;
    left: 95%;
  }

  &--left {
    top: 43.93%;
    left: 5%;
  }

  &--right {
    top: 43.93%;
    left: 95%;
  }

  &--top {
    top: 7.48%;
    left: 50%;
  }

  &--bottom {
    top: 80.37%;
    left: 50%;
  }

  // 边缘滚动的上下两个方向在边的中点上下/左右排开，避免重叠。
  &--left.is-wheel-up,
  &--right.is-wheel-up {
    top: calc(43.93% - 21px);
  }

  &--left.is-wheel-down,
  &--right.is-wheel-down {
    top: calc(43.93% + 21px);
  }

  &--top.is-wheel-up,
  &--bottom.is-wheel-up {
    left: calc(50% - 21px);
  }

  &--top.is-wheel-down,
  &--bottom.is-wheel-down {
    left: calc(50% + 21px);
  }
}

.edge-map__button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  padding: 0;
  border: 1px solid var(--md-sys-color-outline-variant);
  background: var(--md-sys-color-surface-container-highest);
  color: var(--md-sys-color-on-surface-variant);
  font-family: inherit;
  cursor: pointer;
  transition:
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    border-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    box-shadow var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover {
    border-color: var(--md-sys-color-outline);
    background: color-mix(in srgb, var(--md-sys-color-on-surface) 8%, var(--md-sys-color-surface-container-highest));
  }

  &:focus-visible {
    outline: 2px solid var(--md-sys-color-primary);
    outline-offset: 2px;
  }

  &.is-enabled {
    border-color: var(--md-sys-color-primary);
    background: var(--md-sys-color-primary-container);
    box-shadow: 0 0 0 4px color-mix(in srgb, var(--md-sys-color-primary) 14%, transparent);
    color: var(--md-sys-color-on-primary-container);
  }

  &--corner {
    width: 44px;
    height: 44px;
    border-radius: var(--md-sys-shape-corner-medium);
  }

  &--friction {
    width: 84px;
    height: 30px;
    border-radius: var(--md-sys-shape-corner-full);

    &.is-vertical {
      width: 30px;
      height: 84px;
      flex-direction: column;
    }
  }

  &--wheel {
    width: 36px;
    height: 36px;
    border-radius: var(--md-sys-shape-corner-full);
  }
}

.edge-map__corner-label {
  font-size: 10.5px;
  font-weight: 600;
  letter-spacing: 0.02em;
  line-height: 1;
}

.edge-map__edge-icon {
  flex: 0 0 auto;

  .is-vertical & {
    transform: rotate(90deg);
  }
}

.edge-map__friction-count {
  font-size: var(--md-sys-typescale-label-small-size);
  font-weight: 600;
  line-height: 1;
}

.edge-map__arrow {
  font-size: 16px;
  font-weight: 600;
  line-height: 1;
}

@media (max-width: 920px) {
  .edge-map__stage {
    max-width: 440px;
  }

  .edge-map__button--corner {
    width: 40px;
    height: 40px;
  }
}
</style>
