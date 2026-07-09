<script setup>
import { computed, ref } from 'vue'
import AppShell from '../../components/AppShell.vue'
import ConfirmDialog from '../../components/ConfirmDialog.vue'
import IconActionButton from '../../components/IconActionButton.vue'
import { useGestureEditorStore } from '../../composables/gestureEditorStore'
import AppBehaviorSettings from './components/AppBehaviorSettings.vue'
import GestureHintSettings from './components/GestureHintSettings.vue'
import MouseTrailSettings from './components/MouseTrailSettings.vue'
import WebDavSettings from './components/WebDavSettings.vue'
import { useUiSettingsDraft } from './composables/useUiSettingsDraft'

const editor = useGestureEditorStore()
const {
  draft,
  trailPreviewStyle,
  hintPreviewStyle,
  queuePersistDraft,
  flushPersistDraft,
  resetSettings
} = useUiSettingsDraft(editor)

const webDavDraftSignature = computed(() =>
  editor.getWebDavSignature(draft.webDav)
)
const webDavReady = computed(
  () =>
    Boolean(draft.webDav.address) &&
    editor.state.webDavTestedSignature === webDavDraftSignature.value
)
const resetConfirmOpen = ref(false)

function openResetConfirm() {
  resetConfirmOpen.value = true
}

function closeResetConfirm() {
  resetConfirmOpen.value = false
}

function confirmResetSettings() {
  resetSettings()
  closeResetConfirm()
}

function saveToWebDav() {
  flushPersistDraft()
  editor.saveConfigToWebDav()
}

function restoreFromWebDav() {
  flushPersistDraft()
  editor.restoreConfigFromWebDav()
}

function testWebDav() {
  flushPersistDraft()
  editor.testWebDavConnection()
}

function exportLocalConfig() {
  flushPersistDraft()
  editor.exportConfigToLocal()
}

function importLocalConfig() {
  editor.importConfigFromLocal()
}
</script>

<template>
  <AppShell
    title="设置"
    description="调整轨迹线、提示窗、应用行为和配置同步等"
    layout-class="page-shell__grid--single page-shell__grid--settings"
  >
    <template #actions>
      <IconActionButton
        icon="download"
        label="导出配置"
        class="secondary-button settings-local-action"
        color="var(--accent-strong)"
        @click="exportLocalConfig"
      />
      <IconActionButton
        icon="upload"
        label="导入配置"
        class="secondary-button settings-local-action"
        color="var(--accent-strong)"
        @click="importLocalConfig"
      />
      <IconActionButton
        icon="reset"
        label="恢复默认设置"
        class="secondary-button settings-reset-action"
        color="var(--danger)"
        @click="openResetConfirm"
      />
    </template>

    <template #right>
      <section class="page-stack">
        <MouseTrailSettings
          :draft="draft"
          :preview-style="trailPreviewStyle"
          @queue-persist="queuePersistDraft"
          @flush-persist="flushPersistDraft"
        />
        <AppBehaviorSettings
          :draft="draft"
          @queue-persist="queuePersistDraft"
          @flush-persist="flushPersistDraft"
          class="app-behavior-card"
        />
        <WebDavSettings
          :draft="draft"
          :editor="editor"
          :ready="webDavReady"
          @queue-persist="queuePersistDraft"
          @flush-persist="flushPersistDraft"
          @test="testWebDav"
          @restore="restoreFromWebDav"
          @save="saveToWebDav"
        />
        <GestureHintSettings
          :draft="draft"
          :preview-style="hintPreviewStyle"
          @queue-persist="queuePersistDraft"
          @flush-persist="flushPersistDraft"
        />
      </section>
    </template>
  </AppShell>

  <ConfirmDialog
    :open="resetConfirmOpen"
    title="恢复默认设置"
    message="这会把轨迹线、底部提示窗、应用行为、排除项和 WebDAV 设置恢复为默认值。"
    confirm-text="恢复默认"
    cancel-text="取消"
    tone="danger"
    @close="closeResetConfirm"
    @confirm="confirmResetSettings"
  />
</template>

<style scoped lang="scss">
.section-card {
  position: relative;
}
.section-card:has(.custom-select) {
  z-index: 114;
}

:deep(.settings-local-action .icon-action-button__icon) {
  width: 22px;
  height: 22px;
}

:deep(.settings-reset-action .icon-action-button__icon) {
  width: 18px;
  height: 18px;
}

:deep(.settings-color) {
  width: 100%;
  min-height: 44px;
  padding: 4px;
  border: 1px solid var(--border);
  border-radius: 14px;
  background: var(--panel-control);
}

:deep(input[type='text']),
:deep(input[type='url']),
:deep(input[type='password']) {
  min-height: 42px;
  padding: 0 12px;
  border: 1px solid var(--border);
  border-radius: 14px;
  background: var(--panel-control);
  color: var(--text);
}
</style>
