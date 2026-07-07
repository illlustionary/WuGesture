<script setup>
import { computed } from 'vue'
import AppShell from '../../components/AppShell.vue'
import IconActionButton from '../../components/IconActionButton.vue'
import { useGestureEditorStore } from '../../composables/gestureEditorStore'
import AppBehaviorSettings from './components/AppBehaviorSettings.vue'
import GestureHintSettings from './components/GestureHintSettings.vue'
import MouseTrailSettings from './components/MouseTrailSettings.vue'
import SettingsPreviewPanel from './components/SettingsPreviewPanel.vue'
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
        @click="resetSettings"
      />
    </template>

    <template #right>
      <section class="page-stack">
        <SettingsPreviewPanel
          :trail-preview-style="trailPreviewStyle"
          :hint-preview-style="hintPreviewStyle"
        />
        <MouseTrailSettings
          :draft="draft"
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
          @queue-persist="queuePersistDraft"
          @flush-persist="flushPersistDraft"
        />
      </section>
    </template>
  </AppShell>
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

:deep(select),
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
