<script setup>
import CustomSelect from '@/components/CustomSelect.vue'
import ToggleCheckbox from '@/components/ToggleCheckbox.vue'
import SettingsField from './SettingsField.vue'
import SettingsFormGrid from './SettingsFormGrid.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'
import {
  CLOSE_BUTTON_BEHAVIOR_OPTIONS,
  WINDOW_TARGET_MODE_OPTIONS
} from '@/constants/gestureEditorOptions'

defineProps({
  draft: { type: Object, required: true }
})

const emit = defineEmits(['queue-persist', 'flush-persist'])

const closeButtonOptions = CLOSE_BUTTON_BEHAVIOR_OPTIONS
const targetWindowModeOptions = WINDOW_TARGET_MODE_OPTIONS

function commit() {
  emit('queue-persist')
  emit('flush-persist')
}

</script>

<template>
  <SettingsSectionCard
    title="应用行为"
  >
    <SettingsFormGrid>
      <div class="app-behavior-settings__checks">
        <ToggleCheckbox
          v-model="draft.appBehavior.launchAtStartup"
          label="开机自启动"
          @change="commit"
        />
        <ToggleCheckbox
          v-model="draft.appBehavior.showConfigWindowOnLaunch"
          label="启动时显示配置窗口"
          note="关闭后会在后台启动，可从托盘打开配置。"
          @change="commit"
        />
        <ToggleCheckbox
          v-model="draft.appBehavior.runAsAdministrator"
          label="以管理员身份打开"
          note="保存后下次启动时生效。"
          @change="commit"
        />
        <ToggleCheckbox
          v-model="draft.appBehavior.disableGesturesInFullscreen"
          label="全屏时禁用鼠标手势"
          @change="commit"
        />
        <ToggleCheckbox
          v-model="draft.appBehavior.disableEdgeActionsInFullscreen"
          label="全屏时禁用边缘操作"
          @change="commit"
        />
      </div>
      <SettingsField label="关闭按钮">
        <CustomSelect
          v-model="draft.appBehavior.closeButtonBehavior"
          :options="closeButtonOptions"
          placeholder="选择关闭行为"
          @change="commit"
        />
      </SettingsField>
      <SettingsField label="目标窗口">
        <CustomSelect
          v-model="draft.appBehavior.targetWindowMode"
          :options="targetWindowModeOptions"
          placeholder="选择目标窗口"
          @change="commit"
        />
      </SettingsField>
    </SettingsFormGrid>
  </SettingsSectionCard>
</template>

<style scoped lang="scss">
.app-behavior-settings__checks {
  display: grid;
  grid-column: 1 / -1;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

@media (max-width: 720px) {
  .app-behavior-settings__checks {
    grid-template-columns: 1fr;
  }
}
</style>
