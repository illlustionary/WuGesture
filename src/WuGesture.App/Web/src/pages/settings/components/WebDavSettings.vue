<script setup>
import { computed, ref } from 'vue'
import BaseInput from '@/components/form/BaseInput.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppIcon from '@/components/ui/AppIcon.vue'
import SettingsField from './SettingsField.vue'
import SettingsFormGrid from './SettingsFormGrid.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'

const props = defineProps({
  draft: { type: Object, required: true },
  ready: { type: Boolean, required: true },
  testState: { type: String, required: true },
  operation: { type: String, default: '' }
})

const emit = defineEmits(['queue-persist', 'flush-persist', 'test', 'restore', 'save'])

const showPassword = ref(false)
const busy = computed(() => Boolean(props.operation))

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

    <template #footer>
      <AppButton
        variant="outlined"
        class="webdav-test-button"
        :class="`webdav-test-button--${webDavTestState}`"
        :disabled="busy"
        @click="emit('test')"
      >
        <template #leading>
          <AppIcon name="link" />
        </template>
        {{ operation === 'test' ? '测试中…' : '测试连接' }}
      </AppButton>
      <AppButton
        variant="filled"
        :disabled="!ready || busy"
        @click="emit('save')"
      >
        <template #leading>
          <AppIcon name="cloud-upload" />
        </template>
        {{ operation === 'save' ? '备份中…' : '备份' }}
      </AppButton>
      <AppButton
        variant="outlined"
        :disabled="!ready || busy"
        @click="emit('restore')"
      >
        <template #leading>
          <AppIcon name="cloud-download" />
        </template>
        {{ operation === 'restore' ? '恢复中…' : '恢复' }}
      </AppButton>
    </template>
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
  right: var(--md-sys-spacing-xs);
  display: inline-grid;
  place-items: center;
  width: 34px;
  height: 34px;
  padding: 0;
  border: none;
  border-radius: var(--md-sys-shape-corner-small);
  background: transparent;
  color: var(--md-sys-color-on-surface-variant);
  cursor: pointer;
  transition:
    color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    background-color var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover {
    color: var(--md-sys-color-on-surface);
    background: var(--md-sys-color-surface-container-high);
  }

  &:focus-visible {
    outline: none;
    color: var(--md-sys-color-on-surface);
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--md-sys-color-primary) 24%, transparent);
  }

  :deep(svg) {
    width: 18px;
    height: 18px;
    display: block;
  }
}

.webdav-test-button.webdav-test-button--success {
  border-color: var(--md-sys-color-success);
}

.webdav-test-button.webdav-test-button--error {
  border-color: var(--md-sys-color-error);
}
</style>
