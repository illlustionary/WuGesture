<script setup>
import SettingsField from './SettingsField.vue'
import SettingsFormGrid from './SettingsFormGrid.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'
import BaseInput from '@/components/form/BaseInput.vue'
import BaseRange from '@/components/form/BaseRange.vue'
import ToggleCheckbox from '@/components/form/ToggleCheckbox.vue'
import { GESTURE_EDITOR_LIMITS } from '@/constants/gestureEditorLimits'

defineProps({
  draft: { type: Object, required: true },
  previewStyle: { type: Object, required: true }
})

const emit = defineEmits(['queue-persist', 'flush-persist'])

function commit() {
  emit('queue-persist')
  emit('flush-persist')
}
</script>

<template>
  <SettingsSectionCard title="轨迹线">
    <div class="field-card">
      <ToggleCheckbox
        v-model="draft.mouseTrail.enabled"
        label="显示轨迹线"
        note="关闭后不再绘制手势拖动路径。"
        @change="commit"
      />
    </div>

    <div
      class="mouse-trail-preview"
      :style="previewStyle"
    >
      <div class="mouse-trail-preview__row">
        <span class="mouse-trail-preview__key mouse-trail-preview__key--inactive"> 未激活 </span>
        <div class="mouse-trail-preview__path mouse-trail-preview__path--inactive" />
      </div>
      <div class="mouse-trail-preview__row">
        <span class="mouse-trail-preview__key mouse-trail-preview__key--active"> 激活 </span>
        <div class="mouse-trail-preview__path mouse-trail-preview__path--active" />
      </div>
    </div>

    <SettingsFormGrid>
      <SettingsField label="未激活颜色">
        <BaseInput
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
        <BaseRange
          v-model.number="draft.mouseTrail.inactiveOpacity"
          :min="GESTURE_EDITOR_LIMITS.opacityPercent.min"
          :max="GESTURE_EDITOR_LIMITS.opacityPercent.max"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="未激活粗细"
        :note="`${draft.mouseTrail.inactiveThickness} px`"
      >
        <BaseRange
          v-model.number="draft.mouseTrail.inactiveThickness"
          :min="GESTURE_EDITOR_LIMITS.mouseTrailThickness.min"
          :max="GESTURE_EDITOR_LIMITS.mouseTrailThickness.max"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField label="激活颜色">
        <BaseInput
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
        <BaseRange
          v-model.number="draft.mouseTrail.activeOpacity"
          :min="GESTURE_EDITOR_LIMITS.opacityPercent.min"
          :max="GESTURE_EDITOR_LIMITS.opacityPercent.max"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="激活粗细"
        :note="`${draft.mouseTrail.activeThickness} px`"
      >
        <BaseRange
          v-model.number="draft.mouseTrail.activeThickness"
          :min="GESTURE_EDITOR_LIMITS.mouseTrailThickness.min"
          :max="GESTURE_EDITOR_LIMITS.mouseTrailThickness.max"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
    </SettingsFormGrid>
  </SettingsSectionCard>
</template>

<style scoped lang="scss">
.mouse-trail-preview {
  display: grid;
  gap: 18px;
  min-height: 150px;
  margin-bottom: 14px;
  padding: 24px;
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  background: var(--panel-inset);
}

.mouse-trail-preview__row {
  display: flex;
  align-items: center;
  flex-wrap: nowrap;
}

.mouse-trail-preview__key {
  display: inline-flex;
  align-items: center;
  min-height: 28px;
  flex-basis: 80px;
  padding: 0 10px;
  border-radius: var(--radius-pill);
  color: var(--text-preview);
  font-size: 12px;
  font-weight: 700;

  &--inactive {
    background: var(--trail-inactive-color);
    opacity: 0.8;
  }

  &--active {
    background: var(--trail-active-color);
  }
}

.mouse-trail-preview__path {
  height: 0;
  border-radius: var(--radius-pill);

  &--inactive {
    width: 72%;
    border-top: var(--trail-inactive-width) solid var(--trail-inactive-stroke);
  }

  &--active {
    width: 72%;
    border-top: var(--trail-active-width) solid var(--trail-active-stroke);
  }
}
</style>
