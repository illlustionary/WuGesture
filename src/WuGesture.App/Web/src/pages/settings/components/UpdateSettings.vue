<script setup>
import CustomSelect from '@/components/form/CustomSelect.vue'
import ToggleCheckbox from '@/components/form/ToggleCheckbox.vue'
import { UPDATE_SOURCE_OPTIONS } from '@/constants/gestureEditorOptions'
import SettingsField from './SettingsField.vue'
import SettingsFormGrid from './SettingsFormGrid.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'

defineProps({
  draft: { type: Object, required: true }
})

const emit = defineEmits(['queue-persist', 'flush-persist'])

const sourceOptions = UPDATE_SOURCE_OPTIONS

function commit() {
  emit('queue-persist')
  emit('flush-persist')
}
</script>

<template>
  <SettingsSectionCard
    title="更新"
    note="Gitee 位于国内，通常访问更稳定；Gitee 暂时没有可用的发行版本时会自动比较 GitHub。只有发行版本号高于当前版本时才会提示更新。"
  >
    <SettingsFormGrid>
      <SettingsField label="更新源">
        <CustomSelect
          v-model="draft.update.source"
          :options="sourceOptions"
          placeholder="选择更新源"
          @change="commit"
        />
      </SettingsField>
      <div class="update-settings__checks">
        <ToggleCheckbox
          v-model="draft.update.checkForUpdates"
          label="启动时自动检查更新"
          note="每次启动只检查一次；发现新版本会弹窗提示，不会自动安装。"
          @change="commit"
        />
      </div>
    </SettingsFormGrid>
  </SettingsSectionCard>
</template>

<style scoped lang="scss">
.update-settings__checks {
  display: grid;
  grid-column: 1 / -1;
}
</style>
