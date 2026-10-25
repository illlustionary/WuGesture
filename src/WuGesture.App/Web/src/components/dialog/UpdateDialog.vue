<script setup>
import { computed } from 'vue'
import BaseDialog from './BaseDialog.vue'

const props = defineProps({
  open: { type: Boolean, required: true },
  status: { type: String, default: 'idle' },
  info: { type: Object, default: null },
  progress: { type: Number, default: 0 },
  phase: { type: String, default: '' },
  error: { type: String, default: '' }
})

const emit = defineEmits(['close', 'update', 'cancel', 'retry', 'open-release-page'])

const phaseLabels = {
  downloading: '下载更新包',
  verifying: '校验更新包',
  extracting: '解压更新包',
  elevating: '请求管理员权限'
}

const isBusy = computed(() => props.status === 'downloading' || props.status === 'restarting')
const sourceLabel = computed(() => (props.info?.source === 'github' ? 'GitHub' : 'Gitee'))
const notes = computed(() => String(props.info?.notes ?? '').trim())
const description = computed(() => {
  if (props.status === 'downloading' || props.status === 'restarting') {
    return '下载完成后 WuGesture 会自动退出、替换程序文件并重新启动，当前配置不会丢失。'
  }

  if (props.status === 'available') {
    return `当前版本 ${props.info?.currentVersion ?? ''}，可更新到 ${props.info?.latestVersion ?? ''}。`
  }

  if (props.status === 'error') {
    return props.error || '更新没有完成，请稍后重试或手动下载安装包。'
  }

  return ''
})
const phaseLabel = computed(() => phaseLabels[props.phase] ?? '准备更新')
const sourceNote = computed(() => {
  const used = props.info?.source
  const preferred = props.info?.preferredSource
  if (!used || !preferred || used === preferred) {
    return ''
  }

  const usedLabel = used === 'github' ? 'GitHub' : 'Gitee'
  const preferredLabel = preferred === 'github' ? 'GitHub' : 'Gitee'
  return `${preferredLabel} 暂未提供可用发行版，已自动改用 ${usedLabel}`
})
const assetSize = computed(() => {
  const bytes = Number(props.info?.assetSize) || 0
  return bytes > 0 ? `${(bytes / 1024 / 1024).toFixed(1)} MB` : ''
})
</script>

<template>
  <BaseDialog
    :open="open"
    :title="status === 'error' ? '更新失败' : status === 'restarting' ? '正在重启' : '发现新版本'"
    title-id="update-dialog-title"
    :description="description"
    :show-close="!isBusy"
    :close-on-mask="!isBusy"
    :show-actions="false"
    panel-class="update-dialog"
    @close="emit('close')"
  >
    <div
      v-if="status === 'available' || status === 'error' || status === 'downloading'"
      class="update-dialog__body"
    >
      <div
        v-if="info"
        class="update-dialog__versions"
      >
        <span class="update-dialog__version">{{ info.currentVersion }}</span>
        <span
          class="update-dialog__arrow"
          aria-hidden="true"
          >→</span
        >
        <span class="update-dialog__version update-dialog__version--latest">{{ info.latestVersion }}</span>
      </div>
      <p
        v-if="info"
        class="update-dialog__meta"
      >
        来源：{{ sourceLabel }}
        <template v-if="info.publishedAt"> · {{ info.publishedAt }}</template>
        <template v-if="assetSize"> · {{ assetSize }}</template>
      </p>
      <p
        v-if="sourceNote"
        class="update-dialog__source-note"
      >
        {{ sourceNote }}
      </p>

      <div
        v-if="status === 'downloading'"
        class="update-dialog__progress"
      >
        <div class="update-dialog__progress-track">
          <div
            class="update-dialog__progress-value"
            :style="{ width: `${progress}%` }"
          />
        </div>
        <span class="update-dialog__progress-label">{{ phaseLabel }} {{ progress }}%</span>
      </div>

      <pre
        v-if="status === 'available' && notes"
        class="update-dialog__notes"
        >{{ notes }}</pre>
    </div>

    <template #actions>
      <template v-if="status === 'available'">
        <button
          type="button"
          class="update-dialog__action update-dialog__action--secondary"
          @click="emit('close')"
        >
          稍后
        </button>
        <button
          type="button"
          class="update-dialog__action update-dialog__action--primary"
          @click="emit('update')"
        >
          立即更新
        </button>
      </template>
      <template v-else-if="status === 'downloading'">
        <button
          type="button"
          class="update-dialog__action update-dialog__action--secondary"
          @click="emit('cancel')"
        >
          取消
        </button>
      </template>
      <template v-else-if="status === 'error'">
        <button
          type="button"
          class="update-dialog__action update-dialog__action--secondary"
          @click="emit('close')"
        >
          关闭
        </button>
        <button
          type="button"
          class="update-dialog__action update-dialog__action--secondary"
          @click="emit('open-release-page')"
        >
          打开发布页
        </button>
        <button
          type="button"
          class="update-dialog__action update-dialog__action--primary"
          @click="emit('retry')"
        >
          重试
        </button>
      </template>
    </template>
  </BaseDialog>
</template>

<style scoped lang="scss">
:deep(.base-dialog__panel.update-dialog) {
  width: min(520px, calc(100vw - 36px));
}

.update-dialog__versions {
  display: flex;
  align-items: center;
  gap: 10px;
  color: var(--text);
  font-size: 15px;
  font-weight: 600;
}

.update-dialog__version--latest {
  color: var(--accent-strong);
}

.update-dialog__arrow {
  color: var(--muted);
}

.update-dialog__meta {
  margin-top: 6px;
  color: var(--muted);
  font-size: 12px;
}

.update-dialog__source-note {
  margin-top: 4px;
  color: var(--text-subtle);
  font-size: 12px;
  line-height: 1.5;
}

.update-dialog__notes {
  max-height: 220px;
  margin-top: 14px;
  padding: 12px;
  overflow: auto;
  border: 1px solid var(--border);
  border-radius: var(--radius-md, 10px);
  background: var(--panel-control);
  color: var(--text);
  font-family: inherit;
  font-size: 12px;
  line-height: 1.65;
  white-space: pre-wrap;
  word-break: break-word;
}

.update-dialog__progress {
  display: grid;
  gap: 8px;
  margin-top: 16px;
}

.update-dialog__progress-track {
  height: 8px;
  overflow: hidden;
  border-radius: 999px;
  background: var(--panel-control);
}

.update-dialog__progress-value {
  height: 100%;
  border-radius: inherit;
  background: var(--accent-strong);
  transition: width 180ms ease;
}

.update-dialog__progress-label {
  color: var(--muted);
  font-size: 12px;
}

.update-dialog__action {
  min-width: 72px;
  min-height: 34px;
  padding: 0 14px;
  border: 1px solid transparent;
  border-radius: var(--radius-sm);
  cursor: pointer;
  font-size: 13px;
  transition: opacity 120ms ease;

  &:hover,
  &:focus-visible {
    opacity: 0.76;
  }

  &:focus-visible {
    outline: 2px solid var(--focus-ring);
    outline-offset: 2px;
  }

  &--secondary {
    border-color: var(--border);
    background: var(--interactive-bg);
    color: var(--text);
  }

  &--primary {
    background: var(--accent-strong);
    color: var(--text-inverse);
  }
}
</style>
