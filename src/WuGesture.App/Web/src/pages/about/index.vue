<script setup>
import AppShell from '@/components/layout/AppShell.vue'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import AboutUpdatePanel from '@/pages/about/components/AboutUpdatePanel.vue'

const editor = useGestureEditorContext()

function checkForUpdate() {
  editor.checkForUpdate()
}
</script>

<template>
  <AppShell layout-class="page-shell__grid--single">
    <template #right>
      <section class="page-stack">
        <AboutUpdatePanel
          :app-version="editor.state.appVersion"
          :update-source="editor.state.uiSettings.update.source"
          :auto-check="editor.state.uiSettings.update.checkForUpdates"
          :checking="editor.state.updateChecking"
          :status="editor.state.updateStatus"
          :info="editor.state.updateInfo"
          :error="editor.state.updateError"
          :progress="editor.state.updateProgress"
          :phase="editor.state.updatePhase"
          @check="checkForUpdate"
          @show-details="editor.openUpdateDialog()"
        />
      </section>
    </template>
  </AppShell>
</template>
