<script setup lang="ts">
import { computed } from 'vue'
import AppButton from '@/components/ui/AppButton.vue'
import BaseDialog from './BaseDialog.vue'

interface UpdateInfo {
  source?: string
  preferredSource?: string
  currentVersion?: string
  latestVersion?: string
  publishedAt?: string
  assetSize?: number
  notes?: string
}

const props = withDefaults(
  defineProps<{
    open: boolean
    status?: string
    info?: UpdateInfo | null
    progress?: number
    phase?: string
    error?: string
  }>(),
  {
    status: 'idle',
    info: null,
    progress: 0,
    phase: '',
    error: ''
  }
)

const emit = defineEmits<{
  close: []
  update: []
  cancel: []
  retry: []
  'open-release-page': []
}>()

const phaseLabels: Record<string, string> = {
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
        <AppButton
          variant="text"
          @click="emit('close')"
        >
          稍后
        </AppButton>
        <AppButton @click="emit('update')">立即更新</AppButton>
      </template>
      <template v-else-if="status === 'downloading'">
        <AppButton
          variant="text"
          @click="emit('cancel')"
        >
          取消
        </AppButton>
      </template>
      <template v-else-if="status === 'error'">
        <AppButton
          variant="text"
          @click="emit('close')"
        >
          关闭
        </AppButton>
        <AppButton
          variant="outlined"
          @click="emit('open-release-page')"
        >
          打开发布页
        </AppButton>
        <AppButton @click="emit('retry')">重试</AppButton>
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
  gap: var(--md-sys-spacing-sm);
  color: var(--md-sys-color-on-surface);
  font-size: var(--md-sys-typescale-title-medium-size);
  line-height: var(--md-sys-typescale-title-medium-line-height);
  font-weight: var(--md-sys-typescale-title-medium-weight);
}

.update-dialog__version--latest {
  color: var(--md-sys-color-primary);
}

.update-dialog__arrow {
  color: var(--md-sys-color-on-surface-variant);
}

.update-dialog__meta {
  margin-top: var(--md-sys-spacing-sm);
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
}

.update-dialog__source-note {
  margin-top: var(--md-sys-spacing-xs);
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: 1.5;
}

.update-dialog__notes {
  max-height: 220px;
  margin-top: var(--md-sys-spacing-md);
  padding: var(--md-sys-spacing-md);
  overflow: auto;
  border: 1px solid var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-small);
  background: var(--md-sys-color-surface-container);
  color: var(--md-sys-color-on-surface);
  font-family: inherit;
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: 1.65;
  white-space: pre-wrap;
  word-break: break-word;
}

.update-dialog__progress {
  display: grid;
  gap: var(--md-sys-spacing-sm);
  margin-top: var(--md-sys-spacing-lg);
}

.update-dialog__progress-track {
  height: 8px;
  overflow: hidden;
  border-radius: var(--md-sys-shape-corner-full);
  background: var(--md-sys-color-surface-container-highest);
}

.update-dialog__progress-value {
  height: 100%;
  border-radius: inherit;
  background: var(--md-sys-color-primary);
  transition: width var(--md-sys-motion-duration-medium) var(--md-sys-motion-easing-standard);
}

.update-dialog__progress-label {
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
}
</style>
