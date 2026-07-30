<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter, RouterView } from 'vue-router'
import AppSidebar from '@/components/AppSidebar.vue'
import AppTitleBar from '@/components/AppTitleBar.vue'
import WindowResizeGrip from '@/components/WindowResizeGrip.vue'
import BaseDialog from '@/components/BaseDialog.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import QuickSearchDialog from '@/components/QuickSearchDialog.vue'
import GestureRuleDialog from '@/components/GestureRuleDialog.vue'
import ScopeCreateDialog from '@/components/ScopeCreateDialog.vue'
import ScopePriorityNotice from '@/components/ScopePriorityNotice.vue'
import AppIcon from '@/components/AppIcon.vue'
import { useAppearanceTheme } from '@/composables/useAppearanceTheme'
import { useGestureEditorEventListeners } from '@/composables/useGestureEditorEventListeners'
import { useQuickSearch } from '@/composables/useQuickSearch'
import { useSidebarScopeActions } from '@/composables/useSidebarScopeActions'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import { useGestureEditorNavigation } from '@/gestureEditor/modules/useGestureEditorNavigation'
import { useGestureEditorLifecycleStore } from '@/gestureEditor/stores/useGestureEditorLifecycleStore'
import { useGestureEditorOverlayStore } from '@/gestureEditor/stores/useGestureEditorOverlayStore'

const lifecycle = useGestureEditorLifecycleStore()
const overlay = useGestureEditorOverlayStore()
const route = useRoute()
const router = useRouter()
useGestureEditorEventListeners(router)
const editor = useGestureEditorContext()
const navigation = useGestureEditorNavigation()
const { isDarkTheme, toggleTheme } = useAppearanceTheme(editor)
const quickSearch = useQuickSearch()
const routeOrder = ['global', 'category', 'app', 'edge', 'exclusions', 'settings']
const transitionDirection = ref('right')
const priorityNoticeOpen = ref(false)
const resetConfirmOpen = ref(false)
const routeTransitionName = computed(() =>
  transitionDirection.value === 'right' ? 'route-slide-right' : 'route-slide-left'
)

const {
  categoryDraft,
  categoryDialogOpen,
  categoryRenameDraft,
  categoryRenameDialogOpen,
  appRenameDialogOpen,
  appRenameDraft,
  closeAppRenameDialog,
  closeCategoryDialog,
  closeCategoryRenameDialog,
  confirmSidebarAppRename,
  confirmSidebarCategory,
  confirmSidebarCategoryRename,
  openAppRenameDialog,
  openCategoryDialog,
  openCategoryRenameDialog,
  openSidebarAppPicker,
  removeSidebarApp,
  removeSidebarCategory
} = useSidebarScopeActions({ editor, route })

lifecycle.initialize()

watch(
  () => route.path,
  (nextPath, previousPath) => {
    const nextIndex = routeOrder.indexOf(navigation.getRouteGroup(nextPath))
    const previousIndex = routeOrder.indexOf(navigation.getRouteGroup(previousPath))

    if (nextIndex === -1 || previousIndex === -1 || nextIndex === previousIndex) {
      transitionDirection.value = 'right'
      return
    }

    transitionDirection.value = nextIndex > previousIndex ? 'right' : 'left'
  }
)

function openResetConfirm() {
  resetConfirmOpen.value = true
}

function closeResetConfirm() {
  resetConfirmOpen.value = false
}

function confirmResetSettings() {
  editor.resetUiSettings()
  closeResetConfirm()
}
</script>

<template>
  <div class="app-shell">
    <AppTitleBar
      :app-info="lifecycle.appInfo"
      :is-maximized="lifecycle.windowMaximized"
      :is-window-resizing="lifecycle.windowResizing"
      :status-state="lifecycle.statusState"
      @close-window="lifecycle.closeWindow"
      @minimize-window="lifecycle.minimizeWindow"
      @start-window-drag="lifecycle.startWindowDrag"
      @toggle-gesture-paused="lifecycle.toggleGesturePaused"
      @toggle-window-maximize="lifecycle.toggleWindowMaximize"
    />

    <div class="app-shell__body">
      <AppSidebar
        :is-dark-theme="isDarkTheme"
        @open-search="quickSearch.open"
        @open-help="priorityNoticeOpen = true"
        @open-category-dialog="openCategoryDialog"
        @open-category-rename="openCategoryRenameDialog"
        @delete-category="removeSidebarCategory"
        @open-app-picker="openSidebarAppPicker"
        @open-app-rename="openAppRenameDialog"
        @delete-app="removeSidebarApp"
        @export-config="editor.exportConfigToLocal"
        @import-config="editor.importConfigFromLocal"
        @reset-settings="openResetConfirm"
        @toggle-theme="toggleTheme"
      />

      <main class="app-shell__content">
        <RouterView v-slot="{ Component, route: viewRoute }">
          <div class="route-transition-frame">
            <Transition :name="routeTransitionName">
              <div :key="viewRoute.fullPath" class="app-route-view">
                <component :is="Component" />
              </div>
            </Transition>
          </div>
        </RouterView>
      </main>
    </div>

    <WindowResizeGrip />

    <BaseDialog
      :open="overlay.applicationPickerOpen"
      :title="overlay.applicationPickerTarget === 'exclusion' ? '添加排除项' : '添加程序'"
      title-id="application-picker-title"
      :show-close="true"
      :show-actions="false"
      @close="overlay.closeApplicationPicker()"
    >
      <div class="picker-options">
        <button
          type="button"
          class="picker-option"
          @pointerdown.prevent="overlay.pickApplicationWindow()"
          @click.prevent
        >
          <AppIcon name="crosshair" class="picker-option__icon" aria-hidden="true" />
          <span class="picker-option__text">
            <strong>拖动选择窗口</strong>
            <span>拖动鼠标到目标窗口后松开</span>
          </span>
        </button>

        <button type="button" class="picker-option" @click="overlay.selectApplication()">
          <AppIcon name="folder" class="picker-option__icon" aria-hidden="true" />
          <span class="picker-option__text">
            <strong>浏览 exe 文件</strong>
            <span>使用文件资源管理器选择</span>
          </span>
        </button>
      </div>
    </BaseDialog>

    <BaseDialog
      :open="priorityNoticeOpen"
      title="规则生效顺序"
      title-id="scope-priority-title"
      :show-close="true"
      :show-actions="false"
      @close="priorityNoticeOpen = false"
    >
      <ScopePriorityNotice />
    </BaseDialog>

    <GestureRuleDialog
      :open="overlay.gestureEditorOpen"
      :draft="overlay.gestureDraft"
      :message="overlay.gestureRecognitionMessage"
      :is-recording-hotkey="overlay.isRecordingHotkey"
      :is-recording-gesture="overlay.gestureRecordingActive"
      :get-gesture-mnemonic="overlay.getGestureMnemonic"
      :window-operations="overlay.windowOperations"
      :volume-operations="overlay.volumeOperations"
      :brightness-operations="overlay.brightnessOperations"
      @close="overlay.closeGestureEditor"
      @persist="overlay.persistGestureEditor"
      @record="overlay.startGestureRecording"
      @record-hotkey="overlay.startRecording"
    />

    <QuickSearchDialog
      :open="quickSearch.isOpen"
      :items="quickSearch.searchItems"
      @close="quickSearch.close"
      @select="quickSearch.select"
    />

    <ScopeCreateDialog
      v-model="categoryDraft"
      :open="categoryDialogOpen"
      title="新增分类"
      description="输入一个分类名称，创建后会出现在左侧菜单。"
      label="分类名称"
      placeholder="例如：浏览器"
      @close="closeCategoryDialog"
      @confirm="confirmSidebarCategory"
    />

    <ScopeCreateDialog
      v-model="categoryRenameDraft"
      :open="categoryRenameDialogOpen"
      title="重命名分类"
      description="输入新的分类名称，失焦或按回车后保存。"
      label="分类名称"
      placeholder="输入分类名称"
      @close="closeCategoryRenameDialog"
      @confirm="confirmSidebarCategoryRename"
    />

    <ScopeCreateDialog
      v-model="appRenameDraft"
      :open="appRenameDialogOpen"
      title="重命名程序"
      description="输入新的程序名称，失焦或按回车后保存。"
      label="程序名称"
      placeholder="输入程序名称"
      @close="closeAppRenameDialog"
      @confirm="confirmSidebarAppRename"
    />

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
  </div>
</template>

<style scoped lang="scss">
.app-shell {
  position: relative;
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
}

.app-shell__body {
  display: flex;
  min-height: 0;
  flex: 1 1 auto;
}

.app-shell__content {
  display: flex;
  flex-direction: column;
  min-width: 0;
  flex: 1 1 auto;
  min-height: 0;
  overflow-y: auto;
  scrollbar-width: none;

  &::-webkit-scrollbar {
    display: none;
  }
}

.route-transition-frame {
  position: relative;
  display: flex;
  flex: 1 1 auto;
  min-height: 100%;
}

.app-route-view {
  display: flex;
  flex: 1 1 auto;
  width: 100%;
  min-height: 100%;
}

.route-slide-right-enter-active,
.route-slide-left-enter-active {
  position: relative;
  z-index: 1;
  transition:
    opacity 190ms ease,
    transform 190ms ease;
}

.route-slide-right-leave-active,
.route-slide-left-leave-active {
  position: absolute;
  inset: 0;
  z-index: 0;
  width: 100%;
  pointer-events: none;
  transition:
    opacity 140ms ease,
    transform 140ms ease;
}

.route-slide-right-enter-from {
  opacity: 0;
  transform: translateX(16px);
}

.route-slide-right-leave-to {
  opacity: 0;
  transform: translateX(-8px);
}

.route-slide-left-enter-from {
  opacity: 0;
  transform: translateX(-16px);
}

.route-slide-left-leave-to {
  opacity: 0;
  transform: translateX(8px);
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
    min-width: 720px;
  }
}
</style>
