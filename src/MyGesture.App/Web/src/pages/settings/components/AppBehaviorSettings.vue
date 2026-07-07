<script setup>
import SettingsField from './SettingsField.vue'
import SettingsFormGrid from './SettingsFormGrid.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'

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
      <SettingsField
        label="开机自启动"
        check
      >
        <input
          v-model="draft.appBehavior.launchAtStartup"
          type="checkbox"
          @change="commit"
        />
      </SettingsField>
      <SettingsField
        label="以管理员身份打开"
        note="保存后下次启动时生效。"
        check
      >
        <input
          v-model="draft.appBehavior.runAsAdministrator"
          type="checkbox"
          @change="commit"
        />
      </SettingsField>
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
