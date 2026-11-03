<script setup>
import { computed, nextTick, ref, watch } from 'vue'
import BaseDialog from './BaseDialog.vue'

const props = defineProps({
  open: { type: Boolean, default: false }
})

const emit = defineEmits(['complete'])

const steps = [
  {
    key: 'gesture',
    title: '按住右键，划一笔',
    lead: '按住右键向下划，松开就是「粘贴」。',
    note: '划得太短会当作普通右键单击，菜单照常弹出。'
  },
  {
    key: 'scope',
    title: '同一个手势，不同范围',
    lead: '程序 › 分类 › 全局，越具体越优先。',
    note: '在「程序」页添加程序后，就能为它单独覆盖手势。'
  },
  {
    key: 'feedback',
    title: '效果看得见，也能调',
    lead: '轨迹线、手势提示和音量亮度都在「设置」里。',
    note: '觉得不跟手，就去「设置 > 手势灵敏度」调整。'
  }
]

const stepIndex = ref(0)
const primaryButton = ref(null)
const step = computed(() => steps[stepIndex.value])
const isLastStep = computed(() => stepIndex.value === steps.length - 1)

watch(
  () => props.open,
  open => {
    if (!open) {
      return
    }

    stepIndex.value = 0
    focusPrimary()
  }
)

watch(stepIndex, () => focusPrimary())

function focusPrimary() {
  nextTick(() => primaryButton.value?.focus())
}

function next() {
  if (isLastStep.value) {
    emit('complete')
    return
  }

  stepIndex.value += 1
}

function previous() {
  if (stepIndex.value > 0) {
    stepIndex.value -= 1
  }
}
</script>

<template>
  <BaseDialog
    :open="open"
    title="欢迎使用 WuGesture"
    description="三步了解手势怎么用；之后可以从侧栏底部的「?」重新查看。"
    title-id="welcome-dialog-title"
    :show-close="true"
    :show-actions="false"
    panel-class="welcome-dialog"
    @close="emit('complete')"
  >
    <div
      class="welcome-dialog__body"
      aria-live="polite"
    >
      <div class="welcome-dialog__visual">
        <svg
          v-if="step.key === 'gesture'"
          viewBox="0 0 200 140"
          aria-hidden="true"
        >
          <rect
            x="76"
            y="4"
            width="48"
            height="70"
            rx="24"
            class="wd-mouse"
          />
          <path
            d="M100 4a24 24 0 0 1 24 24v4h-24z"
            class="wd-mouse-button"
          />
          <path
            d="M100 4v28M76 32h48"
            class="wd-mouse-line"
          />
          <path
            d="M100 82v20"
            class="wd-arrow"
          />
          <path
            d="M94 96l6 7 6-7"
            class="wd-arrow"
          />
          <rect
            x="62"
            y="112"
            width="76"
            height="24"
            rx="12"
            class="wd-result"
          />
          <text
            x="100"
            y="128"
            text-anchor="middle"
            class="wd-result-text"
          >
            粘贴
          </text>
        </svg>

        <svg
          v-else-if="step.key === 'scope'"
          viewBox="0 0 200 140"
          aria-hidden="true"
        >
          <rect
            x="20"
            y="10"
            width="112"
            height="28"
            rx="14"
            class="wd-pill wd-pill--accent"
          />
          <text
            x="76"
            y="29"
            text-anchor="middle"
            class="wd-pill-text wd-pill-text--accent"
          >
            程序
          </text>
          <rect
            x="20"
            y="48"
            width="112"
            height="28"
            rx="14"
            class="wd-pill"
          />
          <text
            x="76"
            y="67"
            text-anchor="middle"
            class="wd-pill-text"
          >
            分类
          </text>
          <rect
            x="20"
            y="86"
            width="112"
            height="28"
            rx="14"
            class="wd-pill"
          />
          <text
            x="76"
            y="105"
            text-anchor="middle"
            class="wd-pill-text"
          >
            全局
          </text>
          <path
            d="M156 104V36"
            class="wd-arrow"
          />
          <path
            d="M150 43l6-7 6 7"
            class="wd-arrow"
          />
        </svg>

        <svg
          v-else
          viewBox="0 0 200 140"
          aria-hidden="true"
        >
          <path
            d="M16 108c26-4 24-34 50-34s24 30 50 26 26-32 60-40"
            class="wd-trail"
          />
          <rect
            x="56"
            y="12"
            width="88"
            height="26"
            rx="13"
            class="wd-result"
          />
          <text
            x="100"
            y="30"
            text-anchor="middle"
            class="wd-result-text"
          >
            粘贴
          </text>
          <rect
            x="56"
            y="116"
            width="88"
            height="8"
            rx="4"
            class="wd-track"
          />
          <rect
            x="56"
            y="116"
            width="56"
            height="8"
            rx="4"
            class="wd-track-fill"
          />
        </svg>
      </div>

      <div class="welcome-dialog__copy">
        <h4 class="welcome-dialog__title">{{ step.title }}</h4>
        <p class="welcome-dialog__lead">{{ step.lead }}</p>
        <p class="welcome-dialog__note">{{ step.note }}</p>
      </div>
    </div>

    <template #actions>
      <div class="welcome-dialog__actions">
        <button
          type="button"
          class="welcome-dialog__skip"
          @click="emit('complete')"
        >
          跳过
        </button>

        <div
          class="welcome-dialog__progress"
          aria-hidden="true"
        >
          <span
            v-for="(item, index) in steps"
            :key="item.key"
            class="welcome-dialog__dot"
            :class="{ 'is-active': index === stepIndex }"
          />
        </div>

        <button
          v-if="stepIndex > 0"
          type="button"
          class="secondary-button"
          @click="previous"
        >
          上一步
        </button>
        <button
          ref="primaryButton"
          type="button"
          class="primary-button"
          @click="next"
        >
          {{ isLastStep ? '开始使用' : '下一步' }}
        </button>
      </div>
    </template>
  </BaseDialog>
</template>

<style scoped lang="scss">
:deep(.welcome-dialog) {
  width: min(560px, calc(100vw - 36px));
}

.welcome-dialog__body {
  display: grid;
  grid-template-columns: minmax(0, 172px) minmax(0, 1fr);
  gap: 22px;
  align-items: center;
}

.welcome-dialog__visual {
  display: grid;
  place-items: center;
  padding: 8px;
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  background: var(--interactive-bg);

  svg {
    width: 100%;
    height: auto;
  }

  text {
    font-family: inherit;
  }
}

.welcome-dialog__copy {
  display: grid;
  gap: 8px;
}

.welcome-dialog__title {
  font-size: 15px;
  font-weight: 700;
}

.welcome-dialog__lead {
  color: var(--text);
  font-size: 13px;
  line-height: 1.65;
}

.welcome-dialog__note {
  color: var(--muted);
  font-size: 12px;
  line-height: 1.6;
}

.wd-mouse {
  fill: var(--panel-control);
  stroke: var(--border-strong);
  stroke-width: 1.5;
}

.wd-mouse-button {
  fill: var(--accent-soft);
  stroke: var(--accent-border-strong);
  stroke-width: 1.5;
}

.wd-mouse-line {
  fill: none;
  stroke: var(--border-strong);
  stroke-width: 1.5;
}

.wd-arrow {
  fill: none;
  stroke: var(--accent-strong);
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.wd-result {
  fill: var(--accent-soft);
  stroke: var(--accent-border-strong);
  stroke-width: 1;
}

.wd-result-text {
  fill: var(--accent-strong);
  font-size: 12px;
  font-weight: 700;
}

.wd-pill {
  fill: var(--panel-control);
  stroke: var(--border);
  stroke-width: 1;
}

.wd-pill--accent {
  fill: var(--accent-soft);
  stroke: var(--accent-border-strong);
}

.wd-pill-text {
  fill: var(--muted);
  font-size: 12px;
  font-weight: 600;
}

.wd-pill-text--accent {
  fill: var(--accent-strong);
}

.wd-trail {
  fill: none;
  stroke: var(--accent-strong);
  stroke-width: 3;
  stroke-linecap: round;
}

.wd-track {
  fill: var(--border-strong);
}

.wd-track-fill {
  fill: var(--accent-strong);
}

.welcome-dialog__actions {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.welcome-dialog__skip {
  padding: 0 4px;
  border: 0;
  background: none;
  color: var(--muted);
  font-size: 13px;
  cursor: pointer;

  &:hover {
    color: var(--text);
  }

  &:focus-visible {
    outline: 2px solid var(--focus-ring);
    outline-offset: 2px;
  }
}

.welcome-dialog__progress {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 0 auto;
}

.welcome-dialog__dot {
  width: 6px;
  height: 6px;
  border-radius: 999px;
  background: var(--border-strong);
  transition:
    width 160ms ease,
    background-color 160ms ease;

  &.is-active {
    width: 18px;
    background: var(--accent-strong);
  }
}

@media (max-width: 520px) {
  .welcome-dialog__body {
    grid-template-columns: minmax(0, 1fr);
    gap: 14px;
  }

  .welcome-dialog__copy {
    text-align: center;
  }
}
</style>
