<script setup lang="ts">
import { computed } from 'vue'
import AppButton from '@/components/ui/AppButton.vue'

interface UpdateInfo {
  source?: string
  latestVersion?: string
}

const props = withDefaults(
  defineProps<{
    appVersion?: string
    updateSource?: string
    autoCheck?: boolean
    checking?: boolean
    status?: string
    info?: UpdateInfo | null
    error?: string
    progress?: number
    phase?: string
  }>(),
  {
    appVersion: 'v0.0.0',
    updateSource: 'gitee',
    autoCheck: true,
    checking: false,
    status: 'idle',
    info: null,
    error: '',
    progress: 0,
    phase: ''
  }
)

const emit = defineEmits<{
  check: []
  'show-details': []
}>()

const phaseLabels: Record<string, string> = {
  downloading: '下载更新包',
  verifying: '校验更新包',
  extracting: '解压更新包',
  elevating: '请求管理员权限'
}

const sourceLabel = computed(() => (props.updateSource === 'github' ? 'GitHub' : 'Gitee'))

const statusText = computed(() => {
  if (props.checking || props.status === 'checking') {
    return '正在检查更新…'
  }

  if (props.status === 'available') {
    const source = props.info?.source === 'github' ? 'GitHub' : props.info?.source === 'gitee' ? 'Gitee' : ''
    const suffix = source ? `（${source}）` : ''
    return `发现新版本 ${props.info?.latestVersion ?? ''}${suffix}`
  }

  if (props.status === 'none') {
    return '当前已是最新版本'
  }

  if (props.status === 'updated') {
    return `已完成更新，当前版本 ${props.appVersion}`
  }

  if (props.status === 'error') {
    return props.error || '检查更新失败'
  }

  if (props.status === 'downloading') {
    return `${phaseLabels[props.phase] ?? '正在下载更新'} ${props.progress}%`
  }

  if (props.status === 'restarting') {
    return '更新已准备就绪，正在重启'
  }

  return ''
})

const statusTone = computed(() => {
  if (props.status === 'available') {
    return 'accent'
  }

  if (props.status === 'none' || props.status === 'updated') {
    return 'success'
  }

  if (props.status === 'error') {
    return 'error'
  }

  return 'muted'
})

const canShowDetails = computed(() => props.status === 'available' && Boolean(props.info))
</script>

<template>
  <section class="section-card">
    <div class="section-head">
      <h3 class="section-title">关于</h3>
    </div>
    <div class="about-panel__intro">
      <p>
        WuGesture 是一个 Windows 鼠标手势工具：按住右键或中键划出轨迹，就能执行快捷键、窗口控制、
        音量或亮度调节，也可以启动程序。
      </p>
      <dl class="about-panel__repos">
        <div class="about-panel__repo">
          <dt>GitHub</dt>
          <dd>
            <a
              href="https://github.com/illlustionary/WuGesture"
              target="_blank"
              rel="noreferrer"
              >github.com/illlustionary/WuGesture</a
            >
          </dd>
        </div>
        <div class="about-panel__repo">
          <dt>Gitee</dt>
          <dd>
            <a
              href="https://gitee.com/neko_nya/WuGesture"
              target="_blank"
              rel="noreferrer"
              >gitee.com/neko_nya/WuGesture</a
            >
          </dd>
        </div>
      </dl>
    </div>
    <dl class="about-panel__facts">
      <div class="about-panel__fact">
        <dt>版本</dt>
        <dd>{{ appVersion }}</dd>
      </div>
      <div class="about-panel__fact">
        <dt>更新源</dt>
        <dd>{{ sourceLabel }}</dd>
      </div>
      <div class="about-panel__fact">
        <dt>启动时自动检查更新</dt>
        <dd>{{ autoCheck ? '已开启' : '已关闭' }}</dd>
      </div>
    </dl>
    <p class="about-panel__hint">更新源和自动检查可以在“设置 > 更新”中修改。</p>
  </section>

  <section class="section-card">
    <div class="section-head">
      <h3 class="section-title">更新</h3>
    </div>
    <div class="about-panel__actions">
      <AppButton
        class="about-panel__button"
        :disabled="checking"
        @click="emit('check')"
      >
        {{ checking ? '检查中…' : '检查更新' }}
      </AppButton>
      <AppButton
        v-if="canShowDetails"
        variant="outlined"
        class="about-panel__button"
        @click="emit('show-details')"
      >
        查看更新
      </AppButton>
      <span
        v-if="statusText"
        class="about-panel__status"
        :class="`about-panel__status--${statusTone}`"
      >
        {{ statusText }}
      </span>
    </div>
    <p class="about-panel__hint">检查更新只比较版本号，不会自动下载；确认后才会下载并重启安装。</p>
  </section>
</template>

<style scoped lang="scss">
.about-panel__intro {
  display: grid;
  gap: var(--md-sys-spacing-sm);
  color: var(--md-sys-color-on-surface);
  font-size: var(--md-sys-typescale-body-medium-size);
  line-height: 1.7;
}

.about-panel__repos {
  display: grid;
  gap: var(--md-sys-spacing-xs);
  margin: 0;
}

.about-panel__repo {
  display: flex;
  align-items: baseline;
  gap: var(--md-sys-spacing-md);
  margin: 0;

  dt {
    flex: 0 0 auto;
    min-width: 52px;
    color: var(--md-sys-color-on-surface-variant);
    font-size: var(--md-sys-typescale-body-small-size);
  }

  dd {
    margin: 0;
    min-width: 0;
    font-size: var(--md-sys-typescale-body-medium-size);
  }

  a {
    color: var(--md-sys-color-primary);
    text-decoration: none;
    overflow-wrap: anywhere;

    &:hover {
      text-decoration: underline;
    }

    &:focus-visible {
      outline: none;
      border-radius: 2px;
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--md-sys-color-primary) 24%, transparent);
    }
  }
}

.about-panel__facts {
  display: grid;
  gap: var(--md-sys-spacing-sm);
  margin: 0;
  padding: var(--md-sys-spacing-md) var(--md-sys-spacing-lg);
  border-radius: var(--md-sys-shape-corner-small);
  background: var(--md-sys-color-surface-container);
}

.about-panel__fact {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: var(--md-sys-spacing-lg);
  margin: 0;

  dt {
    color: var(--md-sys-color-on-surface-variant);
    font-size: var(--md-sys-typescale-body-small-size);
  }

  dd {
    margin: 0;
    color: var(--md-sys-color-on-surface);
    font-size: var(--md-sys-typescale-body-medium-size);
    font-weight: 500;
  }
}

.about-panel__hint {
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: 1.6;
}

.about-panel__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--md-sys-spacing-md);
}

.about-panel__button {
  min-width: 108px;
}

.about-panel__status {
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: 1.5;

  &--accent {
    color: var(--md-sys-color-primary);
  }

  &--success {
    color: var(--md-sys-color-success);
  }

  &--error {
    color: var(--md-sys-color-error);
  }
}
</style>
