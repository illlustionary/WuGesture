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
</script>

<template>
  <AppShell
    title="设置"
    layout-class="page-shell__grid--single page-shell__grid--settings"
  >
    <template #actions>
      <IconActionButton
        icon="reset"
        label="恢复默认设置"
        class="secondary-button"
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
