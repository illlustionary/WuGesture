<script setup>
import { computed } from 'vue'

const props = defineProps({
  appVersion: { type: String, default: 'v0.0.0' },
  updateSource: { type: String, default: 'gitee' },
  autoCheck: { type: Boolean, default: true },
  checking: { type: Boolean, default: false },
  status: { type: String, default: 'idle' },
  info: { type: Object, default: null },
  error: { type: String, default: '' },
  progress: { type: Number, default: 0 },
  phase: { type: String, default: '' }
})

const emit = defineEmits(['check', 'show-details'])

const phaseLabels = {
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
      <p>
        规则按全局、分类和程序三个作用域匹配，另外支持触发角、摩擦边和边缘滚动。配置保存在本机，也可以 通过 WebDAV
        备份。
      </p>
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
      <button
        type="button"
        class="primary-button about-panel__button"
        :disabled="checking"
        @click="emit('check')"
      >
        {{ checking ? '检查中…' : '检查更新' }}
      </button>
      <button
        v-if="canShowDetails"
        type="button"
        class="secondary-button about-panel__button"
        @click="emit('show-details')"
      >
        查看更新
      </button>
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
  gap: 10px;
  color: var(--text);
  font-size: 13px;
  line-height: 1.7;
}

.about-panel__facts {
  display: grid;
  gap: 10px;
  margin: 0;
  padding: 14px 16px;
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--panel-control);
}

.about-panel__fact {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 16px;
  margin: 0;

  dt {
    color: var(--muted);
    font-size: 12px;
  }

  dd {
    margin: 0;
    color: var(--text);
    font-size: 13px;
    font-weight: 600;
  }
}

.about-panel__hint {
  color: var(--text-subtle);
  font-size: 12px;
  line-height: 1.6;
}

.about-panel__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
}

.about-panel__button {
  min-width: 108px;
}

.about-panel__button:disabled {
  cursor: not-allowed;
  opacity: 0.56;
}

.about-panel__status {
  color: var(--muted);
  font-size: 12px;
  line-height: 1.5;

  &--accent {
    color: var(--accent-strong);
  }

  &--success {
    color: var(--success, var(--accent-strong));
  }

  &--error {
    color: var(--danger);
  }
}
</style>
