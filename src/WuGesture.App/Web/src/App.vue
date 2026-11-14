<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter, RouterView } from 'vue-router'
import AppSidebar from '@/components/sidebar/AppSidebar.vue'
import BaseDialog from '@/components/dialog/BaseDialog.vue'
import HelpDialog from '@/components/dialog/HelpDialog.vue'
import QuickSearchDialog from '@/components/dialog/QuickSearchDialog.vue'
import UpdateDialog from '@/components/dialog/UpdateDialog.vue'
import WelcomeDialog from '@/components/dialog/WelcomeDialog.vue'
import GestureRuleDialog from '@/components/gesture/GestureRuleDialog.vue'
import ScopeCreateDialog from '@/components/scope/ScopeCreateDialog.vue'
import AppIcon from '@/components/ui/AppIcon.vue'
import { useAppearanceTheme } from '@/composables/useAppearanceTheme'
import { useGestureEditorEventListeners } from '@/composables/useGestureEditorEventListeners'
import { useQuickSearch } from '@/composables/useQuickSearch'
import { useSidebarScopeActions } from '@/composables/useSidebarScopeActions'
import { useGestureEditorContext } from '@/gestureEditor/context/gestureEditorContext'
import { useGestureEditorNavigation } from '@/gestureEditor/modules/useGestureEditorNavigation'
import { useGestureEditorOverlayStore } from '@/gestureEditor/stores/useGestureEditorOverlayStore'

const overlay = useGestureEditorOverlayStore()
const route = useRoute()
const router = useRouter()
useGestureEditorEventListeners(router)
const editor = useGestureEditorContext()
const navigation = useGestureEditorNavigation()
const { isDarkTheme, themeMode, cycleTheme } = useAppearanceTheme(editor)
const quickSearch = useQuickSearch()
const routeOrder = ['global', 'category', 'app', 'edge', 'exclusions', 'settings', 'about']
const transitionDirection = ref('right')
const helpOpen = ref(false)
const welcomeOpen = ref(false)
const welcomePrompted = ref(false)
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

editor.initialize()

// 首次引导要等主机配置真正到达后再判断：state 里先是默认值，提前判断会先闪一个弹窗。
// 每个会话最多自动弹一次，重新观看由帮助弹窗触发。
watch(
  () => [editor.state.hostConfigLoaded, editor.state.uiSettings?.onboarding?.welcomeCompleted],
  ([configLoaded, welcomeCompleted]) => {
    if (!configLoaded || welcomeCompleted || welcomePrompted.value) {
      return
    }

    welcomePrompted.value = true
    welcomeOpen.value = true
  },
  { immediate: true }
)

function completeWelcome() {
  welcomeOpen.value = false

  const settings = editor.getUiSettingsSnapshot()
  if (settings.onboarding.welcomeCompleted) {
    return
  }

  settings.onboarding.welcomeCompleted = true
  editor.saveUiSettings(settings)
}

function replayWelcome() {
  helpOpen.value = false
  welcomeOpen.value = true
}

// 全局弹窗挂在 App 上，路由切换时不会随页面卸载；侧栏层级高于弹窗遮罩，用户可以在弹窗打开时
// 切换页面，因此这里在路径变化时统一关闭这些弹窗。更新弹窗属于全局更新流程，不在此处关闭。
function closeRouteDialogs() {
  if (overlay.applicationPickerOpen) {
    overlay.closeApplicationPicker()
  }

  if (overlay.gestureEditorOpen) {
    overlay.closeGestureEditor()
  }

  if (quickSearch.isOpen) {
    quickSearch.close()
  }

  helpOpen.value = false

  if (categoryDialogOpen.value) {
    closeCategoryDialog()
  }

  if (categoryRenameDialogOpen.value) {
    closeCategoryRenameDialog()
  }

  if (appRenameDialogOpen.value) {
    closeAppRenameDialog()
  }
}

watch(
  () => route.path,
  (nextPath, previousPath) => {
    closeRouteDialogs()

    const nextIndex = routeOrder.indexOf(navigation.getRouteGroup(nextPath))
    const previousIndex = routeOrder.indexOf(navigation.getRouteGroup(previousPath))

    if (nextIndex === -1 || previousIndex === -1 || nextIndex === previousIndex) {
      transitionDirection.value = 'right'
      return
    }

    transitionDirection.value = nextIndex > previousIndex ? 'right' : 'left'
  }
)
</script>

<template>
  <div class="app-shell">
    <div class="app-shell__body">
      <AppSidebar
        :is-dark-theme="isDarkTheme"
        :theme-mode="themeMode"
        @open-search="quickSearch.open"
        @open-help="helpOpen = true"
        @open-category-dialog="openCategoryDialog"
        @open-category-rename="openCategoryRenameDialog"
        @delete-category="removeSidebarCategory"
        @open-app-picker="openSidebarAppPicker"
        @open-app-rename="openAppRenameDialog"
        @delete-app="removeSidebarApp"
        @cycle-theme="cycleTheme"
      />

      <main class="app-shell__content">
        <RouterView v-slot="{ Component, route: viewRoute }">
          <div class="route-transition-frame">
            <Transition :name="routeTransitionName">
              <div
                :key="viewRoute.fullPath"
                class="app-route-view"
              >
                <component :is="Component" />
              </div>
            </Transition>
          </div>
        </RouterView>
      </main>
    </div>

    <BaseDialog
      :open="overlay.applicationPickerOpen"
      :title="
        overlay.applicationPickerTarget === 'exclusion'
          ? '添加排除项'
          : overlay.applicationPickerTarget === 'program'
            ? '选择程序'
            : '添加程序'
      "
      title-id="application-picker-title"
      :show-close="true"
      :show-actions="false"
      backdrop-class="application-picker-backdrop"
      @close="overlay.closeApplicationPicker()"
    >
      <div class="picker-options">
        <button
          type="button"
          class="picker-option"
          @pointerdown.prevent="overlay.pickApplicationWindow()"
          @click.prevent
        >
          <AppIcon
            name="crosshair"
            class="picker-option__icon"
            aria-hidden="true"
          />
          <span class="picker-option__text">
            <strong>拖动选择窗口</strong>
            <span>拖动鼠标到目标窗口后松开</span>
          </span>
        </button>

        <button
          type="button"
          class="picker-option"
          @click="overlay.selectApplication()"
        >
          <AppIcon
            name="folder"
            class="picker-option__icon"
            aria-hidden="true"
          />
          <span class="picker-option__text">
            <strong>浏览 exe 文件</strong>
            <span>使用文件资源管理器选择</span>
          </span>
        </button>
      </div>
    </BaseDialog>

    <WelcomeDialog
      :open="welcomeOpen"
      @complete="completeWelcome"
    />

    <HelpDialog
      :open="helpOpen"
      @close="helpOpen = false"
      @replay="replayWelcome"
    />

    <GestureRuleDialog
      :open="overlay.gestureEditorOpen"
      :draft="overlay.gestureDraft"
      :message="overlay.gestureRecognitionMessage"
      :is-recording-hotkey="overlay.isRecordingHotkey"
      :is-recording-gesture="overlay.gestureRecordingActive"
      :window-operations="overlay.windowOperations"
      :volume-operations="overlay.volumeOperations"
      :brightness-operations="overlay.brightnessOperations"
      @close="overlay.closeGestureEditor"
      @persist="overlay.persistGestureEditor"
      @record="overlay.startGestureRecording"
      @record-hotkey="overlay.startRecording"
      @select-program="overlay.openProgramPicker"
      @open-application-folder="overlay.openApplicationFolder"
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

    <UpdateDialog
      :open="editor.state.updateDialogOpen"
      :status="editor.state.updateStatus"
      :info="editor.state.updateInfo"
      :progress="editor.state.updateProgress"
      :phase="editor.state.updatePhase"
      :error="editor.state.updateError"
      @close="editor.dismissUpdate()"
      @update="editor.startUpdate()"
      @cancel="editor.cancelUpdate()"
      @retry="editor.retryUpdate()"
      @open-release-page="editor.openReleasePage()"
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
  /* 预留滚动条槽位，滚动条出现/消失时内容不横向跳动。 */
  scrollbar-gutter: stable;
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

:deep(.application-picker-backdrop) {
  z-index: 30;
}

.picker-option {
  display: flex;
  align-items: center;
  gap: var(--md-sys-spacing-md);
  width: 100%;
  padding: var(--md-sys-spacing-lg);
  text-align: left;
  border: 1px solid var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-medium);
  background: var(--md-sys-color-surface-container);
  transition:
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    border-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover {
    border-color: var(--md-sys-color-outline);
    background: var(--md-sys-color-surface-container-high);
  }

  &__icon {
    flex: 0 0 auto;
    width: 24px;
    height: 24px;
    color: var(--md-sys-color-primary);
  }

  &__text {
    display: grid;
    gap: var(--md-sys-spacing-xs);
    min-width: 0;
  }

  &__text > span {
    color: var(--md-sys-color-on-surface-variant);
    font-size: var(--md-sys-typescale-body-medium-size);
    line-height: var(--md-sys-typescale-body-medium-line-height);
  }
}

@media (max-width: 720px) {
  .app-shell {
    min-width: 720px;
  }
}
</style>
