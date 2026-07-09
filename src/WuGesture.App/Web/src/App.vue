<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter, RouterView } from 'vue-router'
import AppHeader from './components/AppHeader.vue'
import IconActionButton from './components/IconActionButton.vue'
import GestureRuleDialog from './components/GestureRuleDialog.vue'
import { useGestureEditorLifecycleStore } from './composables/gestureEditor/useGestureEditorLifecycleStore'
import { useGestureEditorOverlayStore } from './composables/gestureEditor/useGestureEditorOverlayStore'
import CrosshairIcon from './assets/crosshair.svg'
import FolderIcon from './assets/folder.svg'

const lifecycle = useGestureEditorLifecycleStore()
const overlay = useGestureEditorOverlayStore()
const route = useRoute()
const router = useRouter()
const tabs = [
  { to: '/global', label: '全局' },
  { to: '/category', label: '分类' },
  { to: '/app', label: '程序' },
  { to: '/edge', label: '边缘操作' },
  { to: '/exclusions', label: '排除项' }
]
const routeOrder = [
  '/global',
  '/category',
  '/app',
  '/edge',
  '/exclusions',
  '/settings'
]
const transitionDirection = ref('right')
const routeTransitionName = computed(() =>
  transitionDirection.value === 'right' ? 'route-slide-right' : 'route-slide-left'
)

lifecycle.initialize()

watch(
  () => route.path,
  (nextPath, previousPath) => {
    const nextIndex = routeOrder.indexOf(nextPath)
    const previousIndex = routeOrder.indexOf(previousPath)

    if (nextIndex === -1 || previousIndex === -1 || nextIndex === previousIndex) {
      transitionDirection.value = 'right'
      return
    }

    transitionDirection.value = nextIndex > previousIndex ? 'right' : 'left'
  }
)

function openSettingsPage() {
  router.push('/settings')
}
</script>

<template>
  <div class="app-shell">
    <AppHeader
      :status-text="lifecycle.statusText"
      :status-state="lifecycle.statusState"
      :tabs="tabs"
      @open-settings="openSettingsPage"
    />

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

    <div
      v-if="overlay.applicationPickerOpen"
      class="modal-backdrop"
      @click.self="overlay.closeApplicationPicker()"
    >
      <section
        class="modal-panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="application-picker-title"
      >
        <div class="modal-panel__head">
          <div>
            <h3 id="application-picker-title">
              {{
                overlay.applicationPickerTarget === 'exclusion'
                  ? '添加排除项'
                  : '添加程序'
              }}
            </h3>
            <!-- <p>
              {{
                overlay.applicationPickerScopeKind === 'app'
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
            @click="overlay.closeApplicationPicker()"
          />
        </div>

        <div class="picker-options">
          <button
            type="button"
            class="picker-option"
            @pointerdown.prevent="overlay.pickApplicationWindow()"
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
            @click="overlay.selectApplication()"
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
  </div>
</template>

<style scoped lang="scss">
.app-shell {
  padding: 20px 50px 26px;
}

.route-transition-frame {
  position: relative;
}

.app-route-view {
  display: block;
  width: 100%;
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
    width: calc(100% - 12px);
    padding-top: 10px;
  }
}
</style>
