<script setup>
import { computed } from 'vue'
import BaseInput from '@/components/BaseInput.vue'
import IconActionButton from '@/components/IconActionButton.vue'
import SettingsField from './SettingsField.vue'
import SettingsFormGrid from './SettingsFormGrid.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'

const props = defineProps({
  draft: { type: Object, required: true },
  ready: { type: Boolean, required: true },
  testState: { type: String, required: true },
  testing: { type: Boolean, required: true }
})

const emit = defineEmits(['queue-persist', 'flush-persist', 'test', 'restore', 'save'])

const webDavTestState = computed(() => {
  if (props.ready && props.testState === 'success') {
    return 'success'
  }

  if (props.testState === 'error') {
    return 'error'
  }

  return 'idle'
})
</script>

<template>
  <SettingsSectionCard
    title="WebDAV"
    :note="ready ? '当前 WebDAV 配置已测试通过。' : '备份或恢复前需要先测试当前 WebDAV 配置。'"
  >
    <template #actions>
      <IconActionButton
        icon="test"
        :label="testing ? '测试中' : '测试'"
        class="secondary-button webdav-test-button"
        :class="`webdav-test-button--${webDavTestState}`"
        color="var(--accent-strong)"
        :disabled="testing"
        @click="emit('test')"
      />
      <IconActionButton
        icon="cloud-upload"
        label="备份"
        class="primary-button webdav-action-button"
        :color="ready ? 'var(--accent-strong)' : 'var(--muted)'"
        :disabled="!ready || testing"
        @click="emit('save')"
      />
      <IconActionButton
        icon="cloud-download"
        label="恢复"
        class="secondary-button webdav-action-button"
        :color="ready ? 'var(--accent-strong)' : 'var(--muted)'"
        :disabled="!ready || testing"
        @click="emit('restore')"
      />
    </template>

    <SettingsFormGrid>
      <SettingsField label="地址">
        <BaseInput
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
        <BaseInput
          v-model.trim="draft.webDav.remotePath"
          type="text"
          placeholder="wugesture/"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField label="账号">
        <BaseInput
          v-model.trim="draft.webDav.userName"
          type="text"
          autocomplete="username"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField label="密码">
        <BaseInput
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

:deep(.webdav-test-button--success) {
  border-color: var(--success-border);
}

:deep(.webdav-test-button--error) {
  border-color: var(--danger-border);
}

:deep(.webdav-action-button) {
  opacity: 0.48;
}

:deep(.webdav-action-button:not(:disabled)) {
  opacity: 1;
}
</style>
