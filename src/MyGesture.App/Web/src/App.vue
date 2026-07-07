<script setup>
import { useRouter, RouterView } from 'vue-router'
import AppHeader from './components/AppHeader.vue'
import IconActionButton from './components/IconActionButton.vue'
import GestureRuleDialog from './components/GestureRuleDialog.vue'
import { useGestureEditorStore } from './composables/gestureEditorStore'
import CrosshairIcon from './assets/crosshair.svg'
import FolderIcon from './assets/folder.svg'

const editor = useGestureEditorStore()
const router = useRouter()
const tabs = [
  { to: '/global', label: '全局' },
  { to: '/category', label: '分类' },
  { to: '/app', label: '程序' },
  { to: '/edge', label: '边缘操作' }
]

editor.initialize()

function openSettingsPage() {
  router.push('/settings')
}
</script>

<template>
  <div class="app-shell">
    <AppHeader
      :status-text="editor.state.statusText"
      :status-state="editor.state.statusState"
      :tabs="tabs"
      @open-settings="openSettingsPage"
    />

    <RouterView />

    <div
      v-if="editor.state.applicationPickerOpen"
      class="modal-backdrop"
      @click.self="editor.closeApplicationPicker()"
    >
      <section
        class="modal-panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="application-picker-title"
      >
        <div class="modal-panel__head">
          <div>
            <h3 id="application-picker-title">添加程序</h3>
            <!-- <p>
              {{
                editor.state.applicationPickerScopeKind === 'app'
                  ? '选择一种方式添加程序规则。'
                  : '选择一种方式把程序加入当前分类。'
              }}
            </p> -->
          </div>
          <IconActionButton
            icon="close"
            label="关闭"
            class="ghost-button"
            tone="muted"
            @click="editor.closeApplicationPicker()"
          />
        </div>

        <div class="picker-options">
          <button
            type="button"
            class="picker-option"
            @pointerdown.prevent="editor.pickApplicationWindow()"
            @click.prevent
          >
            <CrosshairIcon
              class="picker-option__icon"
              aria-hidden="true"
            />
            <span class="picker-option__text">
              <strong>拖动准星选择窗口</strong>
              <span>使用准星拖动选择</span>
            </span>
          </button>

          <button
            type="button"
            class="picker-option"
            @click="editor.selectApplication()"
          >
            <FolderIcon
              class="picker-option__icon"
              aria-hidden="true"
            />
            <span class="picker-option__text">
              <strong>浏览 exe 文件</strong>
              <span>使用文件资源管理器选择</span>
            </span>
          </button>
        </div>
      </section>
    </div>

    <GestureRuleDialog
      :open="editor.state.gestureEditorOpen"
      :draft="editor.state.gestureDraft"
      :message="editor.state.gestureRecognitionMessage"
      :is-recording-hotkey="editor.isRecordingHotkey"
      :is-recording-gesture="editor.state.gestureRecordingActive"
      :get-gesture-mnemonic="editor.getGestureMnemonic"
      :window-operations="editor.windowOperations"
      :volume-operations="editor.volumeOperations"
      :brightness-operations="editor.brightnessOperations"
      @close="editor.closeGestureEditor"
      @persist="editor.persistGestureEditor"
      @record="editor.startGestureRecording"
      @record-hotkey="editor.startRecording"
    />
  </div>
</template>

<style scoped lang="scss">
.app-shell {
  padding: 20px 50px 26px;
}

.picker-options {
  display: grid;
  gap: 10px;
}

.picker-option {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 16px;
  text-align: left;
  border-radius: 18px;
  border: 1px solid var(--border);
  background: var(--interactive-bg);
  transition:
    background-color 120ms ease,
    border-color 120ms ease;

  &:hover {
    border-color: var(--border-strong);
    background: var(--interactive-hover-bg);
  }

  &__icon {
    width: 24px;
    height: 24px;
    flex: 0 0 auto;
    color: var(--accent-strong);
  }

  &__text {
    display: grid;
    gap: 4px;
    min-width: 0;
  }

  &__text > span {
    color: var(--muted);
    font-size: 13px;
  }
}

@media (max-width: 720px) {
  .app-shell {
    width: calc(100% - 12px);
    padding-top: 10px;
  }
}
</style>
