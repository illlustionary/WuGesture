<script setup>
import SettingsField from './SettingsField.vue'
import SettingsFormGrid from './SettingsFormGrid.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'
import BaseInput from '@/components/BaseInput.vue'
import BaseRange from '@/components/BaseRange.vue'
import ToggleCheckbox from '@/components/ToggleCheckbox.vue'
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
  <SettingsSectionCard title="底部提示窗">
    <div class="field-card">
      <ToggleCheckbox
        v-model="draft.gestureHint.enabled"
        label="显示触发提示"
        note="关闭后不再显示手势触发后的弹窗。"
        @change="commit"
      />
    </div>

    <div class="gesture-hint-preview">
      <div
        class="gesture-hint-preview__bubble"
        :style="previewStyle"
      >
        <strong>触发的规则</strong>
      </div>
    </div>

    <SettingsFormGrid>
      <SettingsField
        label="显示时长"
        :note="
          draft.gestureHint.displayDurationMs === 0
            ? draft.gestureHint.fadeDurationMs === 0
              ? '立即消失'
              : '立即淡出'
            : `${draft.gestureHint.displayDurationMs} ms`
        "
      >
        <BaseRange
          v-model.number="draft.gestureHint.displayDurationMs"
          :min="GESTURE_EDITOR_LIMITS.hintDisplayDuration.min"
          :max="GESTURE_EDITOR_LIMITS.hintDisplayDuration.max"
          step="100"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="淡出时长"
        :note="draft.gestureHint.fadeDurationMs === 0 ? '立即消失' : `${draft.gestureHint.fadeDurationMs} ms`"
      >
        <BaseRange
          v-model.number="draft.gestureHint.fadeDurationMs"
          :min="GESTURE_EDITOR_LIMITS.hintFadeDuration.min"
          :max="GESTURE_EDITOR_LIMITS.hintFadeDuration.max"
          step="20"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="字体大小"
        :note="`${draft.gestureHint.fontSize} px`"
      >
        <BaseRange
          v-model.number="draft.gestureHint.fontSize"
          :min="GESTURE_EDITOR_LIMITS.hintFontSize.min"
          :max="GESTURE_EDITOR_LIMITS.hintFontSize.max"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField label="字体颜色">
        <BaseInput
          v-model="draft.gestureHint.textColor"
          type="color"
          class="settings-color"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField label="背景颜色">
        <BaseInput
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
        <BaseRange
          v-model.number="draft.gestureHint.backgroundOpacity"
          :min="GESTURE_EDITOR_LIMITS.opacityPercent.min"
          :max="GESTURE_EDITOR_LIMITS.opacityPercent.max"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="宽度"
        :note="draft.gestureHint.autoWidth ? '自适应' : `${draft.gestureHint.widthPercent}%`"
      >
        <BaseRange
          v-model.number="draft.gestureHint.widthPercent"
          :min="GESTURE_EDITOR_LIMITS.hintWidthPercent.min"
          :max="GESTURE_EDITOR_LIMITS.hintWidthPercent.max"
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
        <BaseRange
          v-model.number="draft.gestureHint.heightPercent"
          :min="GESTURE_EDITOR_LIMITS.hintHeightPercent.min"
          :max="GESTURE_EDITOR_LIMITS.hintHeightPercent.max"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="圆角"
        :note="`${draft.gestureHint.cornerRadius} px`"
      >
        <BaseRange
          v-model.number="draft.gestureHint.cornerRadius"
          :min="GESTURE_EDITOR_LIMITS.hintCornerRadius.min"
          :max="GESTURE_EDITOR_LIMITS.hintCornerRadius.max"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
      <SettingsField
        label="距离底部"
        :note="`${draft.gestureHint.bottomOffsetPercent}%`"
      >
        <BaseRange
          v-model.number="draft.gestureHint.bottomOffsetPercent"
          :min="GESTURE_EDITOR_LIMITS.hintBottomOffsetPercent.min"
          :max="GESTURE_EDITOR_LIMITS.hintBottomOffsetPercent.max"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>
    </SettingsFormGrid>
  </SettingsSectionCard>
</template>

<style scoped lang="scss">
.gesture-hint-preview {
  display: flex;
  align-items: flex-end;
  justify-content: center;
  min-height: 180px;
  margin-bottom: 14px;
  padding: 18px;
  border: 1px solid var(--border-subtle);
  border-radius: 18px;
  background: var(--panel-inset);
  overflow: hidden;
}

.gesture-hint-preview__bubble {
  display: grid;
  gap: 8px;
  justify-items: start;
  margin-bottom: var(--hint-bottom-offset);
  padding: 16px 28px;
  border: 1px solid var(--border-inverse);
  border-radius: var(--hint-radius);
  color: var(--hint-color);
  background: var(--hint-background-rgba);
  box-shadow: var(--shadow-popover);

  strong {
    font-size: var(--hint-font-size);
    display: flex;
    align-items: center;
    justify-content: center;
  }
}
</style>
