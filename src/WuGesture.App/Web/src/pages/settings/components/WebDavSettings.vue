<script setup>
import IconActionButton from '../../../components/IconActionButton.vue'
import SettingsField from './SettingsField.vue'
import SettingsFormGrid from './SettingsFormGrid.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'

defineProps({
  draft: { type: Object, required: true },
  editor: { type: Object, required: true },
  ready: { type: Boolean, required: true }
})

const emit = defineEmits([
  'queue-persist',
  'flush-persist',
  'test',
  'restore',
  'save'
])
</script>

<template>
  <SettingsSectionCard
    title="WebDAV"
    description="把当前配置保存到远程，或从远程恢复本机配置。"
    :note="ready ? '当前 WebDAV 配置已测试通过。' : '保存或恢复前需要先测试当前 WebDAV 配置。'"
  >
    <template #actions>
      <IconActionButton
        icon="test"
        :label="editor.state.webDavTesting ? '测试中' : '测试'"
        class="secondary-button"
        :disabled="editor.state.webDavTesting"
        @click="emit('test')"
      />
      <IconActionButton
        icon="download"
        label="恢复"
        class="secondary-button webdav-action-button"
        :color="ready ? 'var(--accent-strong)' : 'var(--muted)'"
        :disabled="!ready || editor.state.webDavTesting"
        @click="emit('restore')"
      />
      <IconActionButton
        icon="upload"
        label="保存"
        class="primary-button webdav-action-button"
        :color="ready ? 'var(--accent-strong)' : 'var(--muted)'"
        :disabled="!ready || editor.state.webDavTesting"
        @click="emit('save')"
      />
    </template>

    <SettingsFormGrid>
      <SettingsField label="地址">
        <input
          v-model.trim="draft.webDav.address"
          type="url"
          placeholder="https://example.com/dav/"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="路径"
        note="留空时使用 gestures.json。"
      >
        <input
          v-model.trim="draft.webDav.remotePath"
          type="text"
          placeholder="wugesture/"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField label="账号">
        <input
          v-model.trim="draft.webDav.userName"
          type="text"
          autocomplete="username"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField label="密码">
        <input
          v-model="draft.webDav.password"
          type="password"
          autocomplete="current-password"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
    </SettingsFormGrid>
  </SettingsSectionCard>
</template>

<style scoped lang="scss">
:deep(button:disabled) {
  cursor: not-allowed;
  box-shadow: none;
}

:deep(.webdav-action-button) {
  opacity: 0.48;
}

:deep(.webdav-action-button:not(:disabled)) {
  opacity: 1;
}
</style>
