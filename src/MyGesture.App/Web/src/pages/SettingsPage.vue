<script setup>
import { computed, onBeforeUnmount, reactive, watch } from 'vue'
import AppShell from '../components/AppShell.vue'
import IconActionButton from '../components/IconActionButton.vue'
import { useGestureEditorStore } from '../composables/gestureEditorStore'

const editor = useGestureEditorStore()
const draft = reactive(createDraft(editor.getUiSettingsSnapshot()))
let persistTimer = 0
let hasPendingPersist = false
let lastLocalPersistAt = 0

const trailPreviewStyle = computed(() => ({
  '--trail-inactive-stroke': hexToRgba(
    draft.mouseTrail.inactiveColor,
    draft.mouseTrail.inactiveOpacity / 100
  ),
  '--trail-inactive-width': `${draft.mouseTrail.inactiveThickness}px`,
  '--trail-active-stroke': hexToRgba(
    draft.mouseTrail.activeColor,
    draft.mouseTrail.activeOpacity / 100
  ),
  '--trail-active-width': `${draft.mouseTrail.activeThickness}px`
}))

const hintPreviewStyle = computed(() => ({
  width: draft.gestureHint.autoWidth
    ? 'fit-content'
    : `${draft.gestureHint.widthPercent}%`,
  maxWidth: '100%',
  height: `${Math.max(80, draft.gestureHint.heightPercent * 3)}px`,
  '--hint-color': draft.gestureHint.textColor,
  '--hint-background-rgba': hexToRgba(
    draft.gestureHint.backgroundColor,
    draft.gestureHint.backgroundOpacity / 100
  ),
  '--hint-muted-color': hexToRgba(draft.gestureHint.textColor, 0.72),
  '--hint-font-size': `${draft.gestureHint.fontSize}px`,
  '--hint-radius': `${draft.gestureHint.cornerRadius}px`,
  '--hint-bottom-offset': `${Math.min(80, draft.gestureHint.bottomOffsetPercent * 0.8)}px`
}))

watch(
  () => editor.state.uiSettings,
  () => {
    if (
      hasPendingPersist ||
      persistTimer ||
      Date.now() - lastLocalPersistAt < 600
    ) {
      return
    }

    Object.assign(draft, createDraft(editor.getUiSettingsSnapshot()))
  },
  { deep: true, immediate: true }
)

function persistDraft() {
  lastLocalPersistAt = Date.now()
  editor.saveUiSettings(createDraft(draft))
}

function queuePersistDraft() {
  hasPendingPersist = true
  if (persistTimer) {
    clearTimeout(persistTimer)
  }

  persistTimer = window.setTimeout(() => {
    persistTimer = 0
    flushPersistDraft()
  }, 250)
}

function flushPersistDraft() {
  if (persistTimer) {
    clearTimeout(persistTimer)
    persistTimer = 0
  }

  if (!hasPendingPersist) {
    return
  }

  hasPendingPersist = false
  persistDraft()
}

function resetSettings() {
  if (persistTimer) {
    clearTimeout(persistTimer)
    persistTimer = 0
  }
  hasPendingPersist = false
  lastLocalPersistAt = 0
  editor.resetUiSettings()
  Object.assign(draft, createDraft(editor.getUiSettingsSnapshot()))
}

onBeforeUnmount(() => {
  if (hasPendingPersist) {
    flushPersistDraft()
  }
})

function createDraft(settings) {
  const mouseTrail = normalizeObjectKeys(
    settings?.mouseTrail ?? settings?.MouseTrail
  )
  const gestureHint = normalizeObjectKeys(
    settings?.gestureHint ?? settings?.GestureHint
  )
  const appBehavior = normalizeObjectKeys(
    settings?.appBehavior ?? settings?.AppBehavior
  )
  const legacyThickness = mouseTrail.thickness ?? mouseTrail.Thickness
  return {
    mouseTrail: {
      inactiveColor: mouseTrail.inactiveColor ?? '#AAAAAA',
      activeColor: mouseTrail.activeColor ?? '#87CEEB',
      inactiveThickness: mouseTrail.inactiveThickness ?? legacyThickness ?? 3,
      activeThickness: mouseTrail.activeThickness ?? legacyThickness ?? 3,
      thickness: legacyThickness ?? mouseTrail.inactiveThickness ?? 3,
      inactiveOpacity: mouseTrail.inactiveOpacity ?? 74,
      activeOpacity: mouseTrail.activeOpacity ?? 100
    },
    gestureHint: {
      fontFamily: gestureHint.fontFamily ?? 'Segoe UI Semibold',
      fontSize: gestureHint.fontSize ?? 22,
      textColor: gestureHint.textColor ?? '#FFFFFF',
      backgroundColor: gestureHint.backgroundColor ?? '#12181F',
      backgroundOpacity: gestureHint.backgroundOpacity ?? 90,
      width: gestureHint.width ?? 540,
      widthPercent: gestureHint.widthPercent ?? 28,
      autoWidth: Boolean(gestureHint.autoWidth ?? false),
      height: gestureHint.height ?? 120,
      heightPercent: gestureHint.heightPercent ?? 11,
      cornerRadius: gestureHint.cornerRadius ?? 28,
      bottomOffset: gestureHint.bottomOffset ?? 140,
      bottomOffsetPercent: gestureHint.bottomOffsetPercent ?? 13
    },
    appBehavior: {
      launchAtStartup: Boolean(appBehavior.launchAtStartup ?? false),
      runAsAdministrator: Boolean(appBehavior.runAsAdministrator ?? false),
      closeButtonBehavior: normalizeCloseButtonBehavior(
        appBehavior.closeButtonBehavior
      )
    }
  }
}

function normalizeCloseButtonBehavior(value) {
  return ['minimize-to-tray', 'minimize-to-taskbar', 'exit'].includes(value)
    ? value
    : 'minimize-to-tray'
}

function normalizeObjectKeys(source) {
  if (!source || typeof source !== 'object') {
    return {}
  }

  const normalized = {}
  for (const [key, value] of Object.entries(source)) {
    normalized[key.charAt(0).toLowerCase() + key.slice(1)] = value
  }

  return normalized
}

function hexToRgba(hex, alpha = 1) {
  const normalized = String(hex ?? '')
    .trim()
    .replace('#', '')
  const expanded =
    normalized.length === 3
      ? normalized
          .split('')
          .map(char => `${char}${char}`)
          .join('')
      : normalized

  if (!/^[0-9a-fA-F]{6}$/.test(expanded)) {
    return `rgba(0, 0, 0, ${alpha})`
  }

  const red = Number.parseInt(expanded.slice(0, 2), 16)
  const green = Number.parseInt(expanded.slice(2, 4), 16)
  const blue = Number.parseInt(expanded.slice(4, 6), 16)
  return `rgba(${red}, ${green}, ${blue}, ${Math.max(0, Math.min(1, alpha))})`
}
</script>

<template>
  <AppShell
    title="设置"
    description="调整轨迹显示和手势触发后的底部提示窗外观。"
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
      <section class="settings-page">
        <section class="settings-panel settings-panel--preview">
          <div class="settings-panel__head">
            <div>
              <h3>实时预览</h3>
              <p>这里展示轨迹线和底部提示窗的当前外观。</p>
            </div>
          </div>

          <div class="settings-preview">
            <div
              class="settings-preview__trail"
              :style="trailPreviewStyle"
            >
              <div class="settings-preview__container">
                <span
                  class="settings-preview__key settings-preview__key--inactive"
                  >未激活</span
                >
                <div
                  class="settings-preview__path settings-preview__path--inactive"
                />
              </div>
              <div class="settings-preview__container">
                <span
                  class="settings-preview__key settings-preview__key--active"
                  >激活</span
                >
                <div
                  class="settings-preview__path settings-preview__path--active"
                />
              </div>
            </div>

            <div
              class="settings-preview__hint"
              :style="hintPreviewStyle"
            >
              <strong>已触发：关闭标签</strong>
              <span>这里是底部提示窗示例。</span>
            </div>
          </div>
        </section>

        <section class="settings-panel">
          <div class="settings-panel__head">
            <div>
              <h3>轨迹线</h3>
              <p>手势拖动时显示的路径样式。</p>
            </div>
          </div>

          <div class="settings-grid">
            <label>
              <span>未激活颜色</span>
              <input
                v-model="draft.mouseTrail.inactiveColor"
                type="color"
                class="settings-color"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
            </label>
            <label>
              <span>未激活透明度</span>
              <input
                v-model.number="draft.mouseTrail.inactiveOpacity"
                type="range"
                min="0"
                max="100"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
              <small>{{ draft.mouseTrail.inactiveOpacity }}%</small>
            </label>
            <label>
              <span>未激活粗细</span>
              <input
                v-model.number="draft.mouseTrail.inactiveThickness"
                type="range"
                min="1"
                max="20"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
              <small>{{ draft.mouseTrail.inactiveThickness }} px</small>
            </label>
            <label>
              <span>激活颜色</span>
              <input
                v-model="draft.mouseTrail.activeColor"
                type="color"
                class="settings-color"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
            </label>
            <label>
              <span>激活透明度</span>
              <input
                v-model.number="draft.mouseTrail.activeOpacity"
                type="range"
                min="0"
                max="100"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
              <small>{{ draft.mouseTrail.activeOpacity }}%</small>
            </label>
            <label>
              <span>激活粗细</span>
              <input
                v-model.number="draft.mouseTrail.activeThickness"
                type="range"
                min="1"
                max="20"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
              <small>{{ draft.mouseTrail.activeThickness }} px</small>
            </label>
          </div>
        </section>

        <section class="settings-panel">
          <div class="settings-panel__head">
            <div>
              <h3>应用行为</h3>
              <p>启动权限、开机启动和关闭按钮行为。</p>
            </div>
          </div>

          <div class="settings-grid settings-grid--behavior">
            <label class="settings-check">
              <span>开机自启动</span>
              <input
                v-model="draft.appBehavior.launchAtStartup"
                type="checkbox"
                @change="queuePersistDraft(); flushPersistDraft()"
              />
            </label>
            <label class="settings-check">
              <span>以管理员身份打开</span>
              <input
                v-model="draft.appBehavior.runAsAdministrator"
                type="checkbox"
                @change="queuePersistDraft(); flushPersistDraft()"
              />
              <small>保存后下次启动时生效。</small>
            </label>
            <label>
              <span>关闭按钮</span>
              <select
                v-model="draft.appBehavior.closeButtonBehavior"
                @change="queuePersistDraft(); flushPersistDraft()"
              >
                <option value="minimize-to-tray">最小化到托盘</option>
                <option value="minimize-to-taskbar">最小化到任务栏</option>
                <option value="exit">直接关闭</option>
              </select>
            </label>
          </div>
        </section>

        <section class="settings-panel">
          <div class="settings-panel__head">
            <div>
              <h3>底部提示窗</h3>
              <p>激活规则后的提示窗字体、颜色和尺寸。</p>
            </div>
          </div>

          <div class="settings-grid">
            <label>
              <span>字体大小</span>
              <input
                v-model.number="draft.gestureHint.fontSize"
                type="range"
                min="10"
                max="48"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
              <small>{{ draft.gestureHint.fontSize }} px</small>
            </label>
            <label>
              <span>字体颜色</span>
              <input
                v-model="draft.gestureHint.textColor"
                type="color"
                class="settings-color"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
            </label>
            <label>
              <span>背景颜色</span>
              <input
                v-model="draft.gestureHint.backgroundColor"
                type="color"
                class="settings-color"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
            </label>
            <label>
              <span>背景透明度</span>
              <input
                v-model.number="draft.gestureHint.backgroundOpacity"
                type="range"
                min="0"
                max="100"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
              <small>{{ draft.gestureHint.backgroundOpacity }}%</small>
            </label>
            <label>
              <span>宽度</span>
              <input
                v-model.number="draft.gestureHint.widthPercent"
                type="range"
                min="10"
                max="90"
                :disabled="draft.gestureHint.autoWidth"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
              <small>{{
                draft.gestureHint.autoWidth
                  ? '自适应'
                  : `${draft.gestureHint.widthPercent}%`
              }}</small>
            </label>
            <label class="settings-check">
              <span>适应宽度</span>
              <input
                v-model="draft.gestureHint.autoWidth"
                type="checkbox"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
            </label>
            <label>
              <span>高度</span>
              <input
                v-model.number="draft.gestureHint.heightPercent"
                type="range"
                min="5"
                max="40"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
              <small>{{ draft.gestureHint.heightPercent }}%</small>
            </label>
            <label>
              <span>圆角</span>
              <input
                v-model.number="draft.gestureHint.cornerRadius"
                type="range"
                min="0"
                max="80"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
              <small>{{ draft.gestureHint.cornerRadius }} px</small>
            </label>
            <label>
              <span>距离底部</span>
              <input
                v-model.number="draft.gestureHint.bottomOffsetPercent"
                type="range"
                min="0"
                max="100"
                @input="queuePersistDraft"
                @change="flushPersistDraft"
              />
              <small>{{ draft.gestureHint.bottomOffsetPercent }}%</small>
            </label>
          </div>
        </section>
      </section>
    </template>
  </AppShell>
</template>

<style scoped lang="scss">
.settings-page {
  display: grid;
  gap: 18px;
}

.settings-panel {
  padding: 22px;
  border: 1px solid var(--border);
  border-radius: 26px;
  background: rgba(255, 255, 255, 0.9);
  box-shadow: var(--shadow-soft);
  backdrop-filter: blur(20px) saturate(1.12);

  &__head {
    display: flex;
    justify-content: space-between;
    gap: 10px;
    margin-bottom: 16px;

    h3 {
      font-size: 16px;
      font-weight: 700;
    }

    p {
      margin-top: 4px;
      color: var(--muted);
      font-size: 13px;
    }
  }
}

.settings-preview {
  display: grid;
  gap: 14px;
  grid-template-columns: minmax(0, 1fr) 320px;
  align-items: center;

  &__trail {
    display: grid;
    gap: 18px;
    padding: 24px;
    min-height: 180px;
    border-radius: 22px;
    background:
      radial-gradient(
        circle at 20% 30%,
        rgba(0, 122, 255, 0.06),
        transparent 28%
      ),
      rgba(248, 251, 255, 0.96);
    border: 1px solid rgba(18, 30, 42, 0.08);
  }

  &__container {
    display: flex;
    align-items: center;
    flex-wrap: nowrap;
  }
  &__path {
    height: 0;
    border-radius: 999px;

    &--inactive {
      width: 72%;
      border-top: var(--trail-inactive-width) solid var(--trail-inactive-stroke);
    }

    &--active {
      width: 72%;
      border-top: var(--trail-active-width) solid var(--trail-active-stroke);
    }
  }

  &__key {
    display: inline-flex;
    align-items: center;
    min-height: 28px;
    flex-basis: 80px;
    padding: 0 10px;
    border-radius: 999px;
    color: #292929;
    font-size: 12px;
    font-weight: 700;

    &--inactive {
      background: var(--trail-inactive-color);
      opacity: 0.8;
    }

    &--active {
      background: var(--trail-active-color);
    }
  }

  &__hint {
    display: grid;
    gap: 8px;
    align-self: end;
    justify-self: center;
    margin-bottom: var(--hint-bottom-offset);
    padding: 16px 28px;
    border-radius: var(--hint-radius);
    color: var(--hint-color);
    background: var(--hint-background-rgba);
    box-shadow: 0 18px 34px rgba(18, 30, 42, 0.16);
    border: 1px solid rgba(255, 255, 255, 0.12);

    strong {
      font-size: var(--hint-font-size);
      line-height: 1.1;
    }

    span {
      color: var(--hint-muted-color);
      font-size: 13px;
    }
  }
}

.settings-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;

  label {
    display: grid;
    gap: 8px;
    padding: 14px;
    border-radius: 18px;
    background: rgba(248, 251, 255, 0.96);
    border: 1px solid rgba(18, 30, 42, 0.08);

    span {
      color: var(--muted);
      font-size: 13px;
    }

    small {
      color: var(--muted);
      font-size: 12px;
    }
  }
}

.settings-color {
  width: 100%;
  min-height: 44px;
  padding: 4px;
  border: 1px solid var(--border);
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.92);
}

.settings-grid select {
  min-height: 42px;
  padding: 0 12px;
  border: 1px solid var(--border);
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.92);
  color: var(--text);
}

.settings-check {
  align-content: center;

  input {
    width: 22px;
    height: 22px;
    accent-color: #007aff;
  }
}

@media (max-width: 960px) {
  .settings-preview,
  .settings-grid {
    grid-template-columns: 1fr;
  }
}
</style>
