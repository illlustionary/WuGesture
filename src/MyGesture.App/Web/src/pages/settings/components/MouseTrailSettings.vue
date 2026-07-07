<script setup>
import SettingsField from './SettingsField.vue'
import SettingsFormGrid from './SettingsFormGrid.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'

defineProps({
  draft: { type: Object, required: true }
})

const emit = defineEmits(['queue-persist', 'flush-persist'])
</script>

<template>
  <SettingsSectionCard
    title="轨迹线"
    description="手势拖动时显示的路径样式。"
  >
    <SettingsFormGrid>
      <SettingsField label="未激活颜色">
        <input
          v-model="draft.mouseTrail.inactiveColor"
          type="color"
          class="settings-color"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="未激活透明度"
        :note="`${draft.mouseTrail.inactiveOpacity}%`"
      >
        <input
          v-model.number="draft.mouseTrail.inactiveOpacity"
          type="range"
          min="0"
          max="100"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="未激活粗细"
        :note="`${draft.mouseTrail.inactiveThickness} px`"
      >
        <input
          v-model.number="draft.mouseTrail.inactiveThickness"
          type="range"
          min="1"
          max="20"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField label="激活颜色">
        <input
          v-model="draft.mouseTrail.activeColor"
          type="color"
          class="settings-color"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="激活透明度"
        :note="`${draft.mouseTrail.activeOpacity}%`"
      >
        <input
          v-model.number="draft.mouseTrail.activeOpacity"
          type="range"
          min="0"
          max="100"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="激活粗细"
        :note="`${draft.mouseTrail.activeThickness} px`"
      >
        <input
          v-model.number="draft.mouseTrail.activeThickness"
          type="range"
          min="1"
          max="20"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
    </SettingsFormGrid>
  </SettingsSectionCard>
</template>
