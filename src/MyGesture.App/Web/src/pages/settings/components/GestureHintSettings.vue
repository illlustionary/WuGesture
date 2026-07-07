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
    title="底部提示窗"
    description="激活规则后的提示窗字体、颜色和尺寸。"
  >
    <SettingsFormGrid>
      <SettingsField
        label="字体大小"
        :note="`${draft.gestureHint.fontSize} px`"
      >
        <input
          v-model.number="draft.gestureHint.fontSize"
          type="range"
          min="10"
          max="48"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField label="字体颜色">
        <input
          v-model="draft.gestureHint.textColor"
          type="color"
          class="settings-color"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField label="背景颜色">
        <input
          v-model="draft.gestureHint.backgroundColor"
          type="color"
          class="settings-color"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="背景透明度"
        :note="`${draft.gestureHint.backgroundOpacity}%`"
      >
        <input
          v-model.number="draft.gestureHint.backgroundOpacity"
          type="range"
          min="0"
          max="100"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="宽度"
        :note="draft.gestureHint.autoWidth ? '自适应' : `${draft.gestureHint.widthPercent}%`"
      >
        <input
          v-model.number="draft.gestureHint.widthPercent"
          type="range"
          min="10"
          max="90"
          :disabled="draft.gestureHint.autoWidth"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <div class="field-card">
        <ToggleCheckbox
          v-model="draft.gestureHint.autoWidth"
          label="自动适应内容宽度"
          @change="commit"
        />
      </div>
      <SettingsField
        label="高度"
        :note="`${draft.gestureHint.heightPercent}%`"
      >
        <input
          v-model.number="draft.gestureHint.heightPercent"
          type="range"
          min="5"
          max="40"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="圆角"
        :note="`${draft.gestureHint.cornerRadius} px`"
      >
        <input
          v-model.number="draft.gestureHint.cornerRadius"
          type="range"
          min="0"
          max="80"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="距离底部"
        :note="`${draft.gestureHint.bottomOffsetPercent}%`"
      >
        <input
          v-model.number="draft.gestureHint.bottomOffsetPercent"
          type="range"
          min="0"
          max="100"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
    </SettingsFormGrid>
  </SettingsSectionCard>
</template>
