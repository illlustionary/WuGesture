<script setup>
import { computed, ref } from 'vue'
import ConfirmDialog from '@/components/dialog/ConfirmDialog.vue'
import AppShell from '@/components/layout/AppShell.vue'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import AppBehaviorSettings from '@/pages/settings/components/AppBehaviorSettings.vue'
import ConfigManagement from '@/pages/settings/components/ConfigManagement.vue'
import GestureHintSettings from '@/pages/settings/components/GestureHintSettings.vue'
import GestureSensitivitySettings from '@/pages/settings/components/GestureSensitivitySettings.vue'
import LevelOsdSettings from '@/pages/settings/components/LevelOsdSettings.vue'
import MouseTrailSettings from '@/pages/settings/components/MouseTrailSettings.vue'
import SidebarSettings from '@/pages/settings/components/SidebarSettings.vue'
import UpdateSettings from '@/pages/settings/components/UpdateSettings.vue'
import WebDavSettings from '@/pages/settings/components/WebDavSettings.vue'
import { useUiSettingsDraft } from '@/pages/settings/composables/useUiSettingsDraft'

const props = defineProps({
  section: { type: String, default: 'mouse-trail' }
})

const settingsStore = useGestureEditorContext()
const {
  draft,
  trailPreviewStyle,
  hintPreviewStyle,
  levelOsdPreviewStyle,
  queuePersistDraft,
  flushPersistDraft,
  discardPendingPersist,
  resetSettings: resetDraftSettings
} = useUiSettingsDraft(settingsStore)
const webDavDraftSignature = computed(() => settingsStore.getWebDavSignature(draft.webDav))
const webDavReady = computed(
  () => Boolean(draft.webDav.address) && settingsStore.state.webDavTestedSignature === webDavDraftSignature.value
)
const restoreConfirmOpen = ref(false)
const resetConfirmOpen = ref(false)

function exportConfig() {
  flushPersistDraft()
  settingsStore.exportConfigToLocal()
}

function importConfig() {
  discardPendingPersist()
  settingsStore.importConfigFromLocal()
}

function resetSettings() {
  resetDraftSettings()
  resetConfirmOpen.value = false
}

function saveToWebDav() {
  flushPersistDraft()
  settingsStore.saveConfigToWebDav()
}

function requestRestoreFromWebDav() {
  restoreConfirmOpen.value = true
}

function restoreFromWebDav() {
  flushPersistDraft()
  settingsStore.restoreConfigFromWebDav()
  restoreConfirmOpen.value = false
}

function testWebDav() {
  flushPersistDraft()
  settingsStore.testWebDavConnection()
}

function previewLevelOsd(kind) {
  flushPersistDraft()
  settingsStore.previewLevelOsd(kind)
}
</script>

<template>
  <AppShell layout-class="page-shell__grid--single page-shell__grid--settings">
    <template #right>
      <section class="page-stack">
        <ConfigManagement
          v-if="props.section === 'config'"
          @export="exportConfig"
          @import="importConfig"
          @reset="resetConfirmOpen = true"
        />
        <MouseTrailSettings
          v-else-if="props.section === 'mouse-trail'"
          :draft="draft"
          :preview-style="trailPreviewStyle"
          @queue-persist="queuePersistDraft"
          @flush-persist="flushPersistDraft"
        />
        <LevelOsdSettings
          v-else-if="props.section === 'level-osd'"
          :draft="draft"
          :preview-style="levelOsdPreviewStyle"
          @queue-persist="queuePersistDraft"
          @flush-persist="flushPersistDraft"
          @preview="previewLevelOsd"
        />
        <GestureHintSettings
          v-else-if="props.section === 'gesture-hint'"
          :draft="draft"
          :preview-style="hintPreviewStyle"
          @queue-persist="queuePersistDraft"
          @flush-persist="flushPersistDraft"
        />
        <GestureSensitivitySettings
          v-else-if="props.section === 'sensitivity'"
          :draft="draft"
          @queue-persist="queuePersistDraft"
          @flush-persist="flushPersistDraft"
        />
        <AppBehaviorSettings
          v-else-if="props.section === 'app-behavior'"
          :draft="draft"
          @queue-persist="queuePersistDraft"
          @flush-persist="flushPersistDraft"
          class="app-behavior-card"
        />
        <SidebarSettings
          v-else-if="props.section === 'sidebar'"
          :draft="draft"
          @queue-persist="queuePersistDraft"
          @flush-persist="flushPersistDraft"
        />
        <WebDavSettings
          v-else-if="props.section === 'webdav'"
          :draft="draft"
          :ready="webDavReady"
          :test-state="settingsStore.state.webDavTestState"
          :testing="settingsStore.state.webDavTesting"
          @queue-persist="queuePersistDraft"
          @flush-persist="flushPersistDraft"
          @test="testWebDav"
          @restore="requestRestoreFromWebDav"
          @save="saveToWebDav"
        />
        <UpdateSettings
          v-else-if="props.section === 'update'"
          :draft="draft"
          @queue-persist="queuePersistDraft"
          @flush-persist="flushPersistDraft"
        />
      </section>
    </template>
  </AppShell>
  <ConfirmDialog
    :open="restoreConfirmOpen"
    title="从 WebDAV 恢复配置"
    message="这会使用 WebDAV 中的备份覆盖本地配置，当前本地更改将丢失。"
    confirm-text="确认恢复"
    cancel-text="取消"
    tone="danger"
    @close="restoreConfirmOpen = false"
    @confirm="restoreFromWebDav"
  />
  <ConfirmDialog
    :open="resetConfirmOpen"
    title="恢复默认设置"
    message="这会恢复轨迹线、底部提示窗、应用行为、排除项、WebDAV 和更新设置。手势规则、分类、程序和边缘操作不会改变。"
    confirm-text="恢复默认"
    cancel-text="取消"
    tone="danger"
    @close="resetConfirmOpen = false"
    @confirm="resetSettings"
  />
</template>

<style lang="scss">
:deep(.settings-color) {
  width: 100%;
  min-height: 44px;
  padding: 4px;
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--panel-control);
}
</style>
