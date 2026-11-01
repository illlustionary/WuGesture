<script setup>
import { computed, ref } from 'vue'
import BaseInput from '@/components/form/BaseInput.vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'
import AppIcon from '@/components/ui/AppIcon.vue'
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

const showPassword = ref(false)

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
    :note="
      ready
        ? '当前 WebDAV 配置已测试通过，可备份或恢复配置。'
        : '备份或恢复前，请先测试当前 WebDAV 配置。测试可能会创建尚不存在的远程目录。'
    "
  >
    <template #actions>
      <IconActionButton
        icon="test"
        :label="testing ? '测试中...' : '测试连接'"
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
        note="可填写备份文件名或目录；目录末尾加 /。留空时使用 gestures.json。"
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
        <div class="webdav-password">
          <BaseInput
            v-model="draft.webDav.password"
            :type="showPassword ? 'text' : 'password'"
            autocomplete="current-password"
            class="webdav-password__input"
            @input="emit('queue-persist')"
            @change="emit('flush-persist')"
          />
          <button
            type="button"
            class="webdav-password__toggle"
            :aria-label="showPassword ? '隐藏密码' : '显示密码'"
            :aria-pressed="showPassword"
            :title="showPassword ? '隐藏密码' : '显示密码'"
            @click="showPassword = !showPassword"
          >
            <AppIcon
              :name="showPassword ? 'eye-off' : 'eye'"
              aria-hidden="true"
            />
          </button>
        </div>
      </SettingsField>
    </SettingsFormGrid>
  </SettingsSectionCard>
</template>

<style scoped lang="scss">
.webdav-password {
  position: relative;
  display: flex;
  align-items: center;
}

.webdav-password__input {
  padding-right: 44px;
}

.webdav-password__toggle {
  position: absolute;
  right: 4px;
  display: inline-grid;
  place-items: center;
  width: 34px;
  height: 34px;
  padding: 0;
  border: none;
  border-radius: var(--radius-sm);
  background: transparent;
  color: var(--muted);
  cursor: pointer;
  transition:
    color 120ms ease,
    background-color 120ms ease;

  &:hover {
    color: var(--text);
    background: var(--interactive-hover-bg);
  }

  &:focus-visible {
    outline: none;
    color: var(--text);
    box-shadow: 0 0 0 3px var(--focus-ring);
  }

  :deep(svg) {
    width: 18px;
    height: 18px;
    display: block;
  }
}

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
