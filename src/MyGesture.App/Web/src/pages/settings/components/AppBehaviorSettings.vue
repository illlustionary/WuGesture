<script setup>
import SettingsField from './SettingsField.vue'
import SettingsFormGrid from './SettingsFormGrid.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'
import ToggleCheckbox from '../../../components/ToggleCheckbox.vue'

defineProps({
  draft: { type: Object, required: true }
})

const emit = defineEmits(['queue-persist', 'flush-persist'])

function commit() {
  emit('queue-persist')
  emit('flush-persist')
}
</script>

<template>
  <SettingsSectionCard
    title="应用行为"
    description="启动权限、开机启动和关闭按钮行为。"
  >
    <SettingsFormGrid>
      <div class="app-behavior-settings__checks">
        <ToggleCheckbox
          v-model="draft.appBehavior.launchAtStartup"
          label="开机自启动"
          @change="commit"
        />
        <ToggleCheckbox
          v-model="draft.appBehavior.runAsAdministrator"
          label="以管理员身份打开"
          note="保存后下次启动时生效。"
          @change="commit"
        />
      </div>
      <SettingsField label="关闭按钮">
        <select
          v-model="draft.appBehavior.closeButtonBehavior"
          @change="commit"
        >
          <option value="minimize-to-tray">最小化到托盘</option>
          <option value="minimize-to-taskbar">最小化到任务栏</option>
          <option value="exit">直接关闭</option>
        </select>
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
