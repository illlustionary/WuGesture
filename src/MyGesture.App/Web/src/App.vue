<script setup>
import { RouterView } from 'vue-router'
import AppHeader from './components/AppHeader.vue'
import GestureRuleDialog from './components/GestureRuleDialog.vue'
import { useGestureEditorStore } from './composables/gestureEditorStore'

const editor = useGestureEditorStore()
const tabs = [
  { to: '/global', label: '全局' },
  { to: '/category', label: '分类' },
  { to: '/app', label: '程序' }
]

editor.initialize()
</script>

<template>
  <div class="app-shell">
    <AppHeader
      :status-text="editor.state.statusText"
      :status-state="editor.state.statusState"
      :tabs="tabs"
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
            <p>选择一种方式把程序加入当前分类。</p>
          </div>
          <button
            type="button"
            class="ghost-button"
            @click="editor.closeApplicationPicker()"
          >
            关闭
          </button>
        </div>

        <div class="picker-options">
          <button
            type="button"
            class="picker-option"
            @pointerdown.prevent="editor.pickApplicationWindow()"
            @click.prevent
          >
            <strong>拖动准星选择窗口</strong>
            <span>从正在打开的目标窗口读取程序名称和路径。</span>
          </button>

          <button
            type="button"
            class="picker-option"
            @click="editor.selectApplication()"
          >
            <strong>浏览 exe 文件</strong>
            <span>从磁盘选择程序文件作为备用添加方式。</span>
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
      @close="editor.closeGestureEditor"
      @confirm="editor.saveGestureEditor"
      @record="editor.startGestureRecording"
      @record-hotkey="editor.startRecording"
    />
  </div>
</template>

<style scoped lang="scss">
.app-shell {
  width: min(1360px, calc(100% - 20px));
  margin: 0 auto;
  padding: 18px 0 24px;
}

.picker-options {
  display: grid;
  gap: 10px;
}

.picker-option {
  display: grid;
  gap: 4px;
  width: 100%;
  padding: 14px;
  text-align: left;
  border-radius: 16px;
  border: 1px solid var(--border);
  background: linear-gradient(180deg, #ffffff, #f5f8fb);

  &:hover {
    background: linear-gradient(180deg, #f9fcfe, #eaf3f8);
  }

  span {
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
