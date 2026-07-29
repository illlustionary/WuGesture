<script setup>
import CustomSelect from '@/components/CustomSelect.vue'
import BaseInput from '@/components/BaseInput.vue'
import BaseRange from '@/components/BaseRange.vue'
import IconActionButton from '@/components/IconActionButton.vue'
import ToggleCheckbox from '@/components/ToggleCheckbox.vue'
import SettingsField from './SettingsField.vue'
import SettingsFormGrid from './SettingsFormGrid.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'
import { LEVEL_OSD_POSITIONS } from '@/constants/gestureEditorOptions'
import { GESTURE_EDITOR_LIMITS } from '@/constants/gestureEditorLimits'

defineProps({
  draft: { type: Object, required: true },
  previewStyle: { type: Object, required: true }
})

const emit = defineEmits(['queue-persist', 'flush-persist', 'preview'])

function commit() {
  emit('queue-persist')
  emit('flush-persist')
}
</script>

<template>
  <SettingsSectionCard
    title="音量/亮度提示"
  >
    <template #actions>
      <IconActionButton
        icon="test"
        label="测试音量提示"
        class="secondary-button"
        color="var(--accent-strong)"
        @click="emit('preview', 'volume')"
      />
      <IconActionButton
        icon="test"
        label="测试亮度提示"
        class="secondary-button"
        color="var(--accent-strong)"
        @click="emit('preview', 'brightness')"
      />
    </template>

    <div class="field-card">
      <ToggleCheckbox
        v-model="draft.levelOsd.enabled"
        label="显示音量/亮度提示"
        note="关闭后调节音量或亮度时不再显示提示窗。"
        @change="commit"
      />
    </div>

    <div class="level-osd-preview">
      <div
        class="level-osd-preview__bubble"
        :class="`level-osd-preview__bubble--${draft.levelOsd.position}`"
        :style="previewStyle"
      >
        <div class="level-osd-preview__icon">
          <span />
          <span />
          <span />
        </div>
        <div class="level-osd-preview__track">
          <span />
        </div>
        <strong>72%</strong>
      </div>
    </div>

    <SettingsFormGrid>
      <SettingsField
        label="显示时长"
        :note="
          draft.levelOsd.displayDurationMs === 0
            ? draft.levelOsd.fadeDurationMs === 0
              ? '立即消失'
              : '立即淡出'
            : `${draft.levelOsd.displayDurationMs} ms`
        "
      >
        <BaseRange
          v-model.number="draft.levelOsd.displayDurationMs"
          :min="GESTURE_EDITOR_LIMITS.levelOsdDisplayDuration.min"
          :max="GESTURE_EDITOR_LIMITS.levelOsdDisplayDuration.max"
          step="100"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>

      <SettingsField
        label="淡出时长"
        :note="
          draft.levelOsd.fadeDurationMs === 0
            ? '立即消失'
            : `${draft.levelOsd.fadeDurationMs} ms`
        "
      >
        <BaseRange
          v-model.number="draft.levelOsd.fadeDurationMs"
          :min="GESTURE_EDITOR_LIMITS.levelOsdFadeDuration.min"
          :max="GESTURE_EDITOR_LIMITS.levelOsdFadeDuration.max"
          step="20"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>

      <SettingsField label="背景颜色">
        <BaseInput
          v-model="draft.levelOsd.backgroundColor"
          type="color"
          class="settings-color"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>

      <SettingsField
        label="背景透明度"
        :note="`${draft.levelOsd.backgroundOpacity}%`"
      >
        <BaseRange
          v-model.number="draft.levelOsd.backgroundOpacity"
          :min="GESTURE_EDITOR_LIMITS.opacityPercent.min"
          :max="GESTURE_EDITOR_LIMITS.opacityPercent.max"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>

      <SettingsField label="文字颜色">
        <BaseInput
          v-model="draft.levelOsd.textColor"
          type="color"
          class="settings-color"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>

      <SettingsField label="进度条底色">
        <BaseInput
          v-model="draft.levelOsd.trackColor"
          type="color"
          class="settings-color"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>

      <SettingsField label="音量强调色">
        <BaseInput
          v-model="draft.levelOsd.volumeColor"
          type="color"
          class="settings-color"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>

      <SettingsField label="亮度强调色">
        <BaseInput
          v-model="draft.levelOsd.brightnessColor"
          type="color"
          class="settings-color"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>

      <SettingsField label="位置">
        <CustomSelect
          v-model="draft.levelOsd.position"
          :options="LEVEL_OSD_POSITIONS"
          placeholder="选择位置"
          @change="commit"
        />
      </SettingsField>

      <SettingsField
        label="宽度"
        :note="`${draft.levelOsd.width} px`"
      >
        <BaseRange
          v-model.number="draft.levelOsd.width"
          :min="GESTURE_EDITOR_LIMITS.levelOsdWidth.min"
          :max="GESTURE_EDITOR_LIMITS.levelOsdWidth.max"
          step="10"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>

      <SettingsField
        label="高度"
        :note="`${draft.levelOsd.height} px`"
      >
        <BaseRange
          v-model.number="draft.levelOsd.height"
          :min="GESTURE_EDITOR_LIMITS.levelOsdHeight.min"
          :max="GESTURE_EDITOR_LIMITS.levelOsdHeight.max"
          step="10"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>

      <SettingsField
        label="圆角"
        :note="`${draft.levelOsd.cornerRadius} px`"
      >
        <BaseRange
          v-model.number="draft.levelOsd.cornerRadius"
          :min="GESTURE_EDITOR_LIMITS.levelOsdCornerRadius.min"
          :max="Math.min(
            GESTURE_EDITOR_LIMITS.levelOsdCornerRadius.max,
            Math.min(draft.levelOsd.width, draft.levelOsd.height) / 2
          )"
          step="1"
          @input="emit('queue-persist')"
          @change="emit('flush-persist')"
        />
      </SettingsField>

      <SettingsField
        label="水平偏移"
        :note="`${draft.levelOsd.offsetX} px`"
      >
        <BaseInput
          v-model.number="draft.levelOsd.offsetX"
          type="number"
          :min="GESTURE_EDITOR_LIMITS.levelOsdOffset.min"
          :max="GESTURE_EDITOR_LIMITS.levelOsdOffset.max"
          step="10"
          @change="commit"
        />
      </SettingsField>

      <SettingsField
        label="垂直偏移"
        :note="`${draft.levelOsd.offsetY} px`"
      >
        <BaseInput
          v-model.number="draft.levelOsd.offsetY"
          type="number"
          :min="GESTURE_EDITOR_LIMITS.levelOsdOffset.min"
          :max="GESTURE_EDITOR_LIMITS.levelOsdOffset.max"
          step="10"
          @change="commit"
        />
      </SettingsField>
    </SettingsFormGrid>
  </SettingsSectionCard>
</template>

<style scoped lang="scss">
.level-osd-preview {
  position: relative;
  min-height: 220px;
  margin-bottom: 14px;
  overflow: hidden;
  border: 1px solid var(--border-subtle);
  border-radius: 18px;
  background:
    linear-gradient(var(--border-muted) 1px, transparent 1px),
    linear-gradient(90deg, var(--border-muted) 1px, transparent 1px),
    var(--panel-inset);
  background-size: 24px 24px;
}

.level-osd-preview__bubble {
  position: absolute;
  display: grid;
  grid-template-rows: 1fr auto auto;
  place-items: center;
  gap: 8px;
  padding: 12px;
  color: var(--level-osd-text);
  background: var(--level-osd-background);
  box-shadow: var(--shadow-popover);
  transition:
    top 160ms ease,
    right 160ms ease,
    bottom 160ms ease,
    left 160ms ease,
    transform 160ms ease,
    width 160ms ease,
    height 160ms ease,
    border-radius 160ms ease,
    opacity 160ms ease;

  &--center {
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%) translate(var(--level-osd-offset-x), var(--level-osd-offset-y));
  }

  &--top-center {
    top: 10px;
    left: 50%;
    transform: translateX(-50%) translate(var(--level-osd-offset-x), var(--level-osd-offset-y));
  }

  &--bottom-center {
    bottom: 10px;
    left: 50%;
    transform: translateX(-50%) translate(var(--level-osd-offset-x), var(--level-osd-offset-y));
  }

  &--top-left {
    top: 10px;
    left: 10px;
    transform: translate(var(--level-osd-offset-x), var(--level-osd-offset-y));
  }

  &--top-right {
    top: 10px;
    right: 10px;
    transform: translate(var(--level-osd-offset-x), var(--level-osd-offset-y));
  }

  &--bottom-left {
    bottom: 10px;
    left: 10px;
    transform: translate(var(--level-osd-offset-x), var(--level-osd-offset-y));
  }

  &--bottom-right {
    right: 10px;
    bottom: 10px;
    transform: translate(var(--level-osd-offset-x), var(--level-osd-offset-y));
  }
}

.level-osd-preview__icon {
  display: flex;
  align-items: flex-end;
  gap: 3px;
  height: 36px;

  span {
    display: block;
    width: 5px;
    border-radius: 999px;
    background: #9edfff;

    &:nth-child(1) {
      height: 12px;
    }

    &:nth-child(2) {
      height: 22px;
    }

    &:nth-child(3) {
      height: 32px;
    }
  }
}

.level-osd-preview__track {
  width: 76%;
  height: 7px;
  overflow: hidden;
  border-radius: 999px;
  background: var(--level-osd-track);

  span {
    display: block;
    width: 72%;
    height: 100%;
    border-radius: inherit;
    background: var(--level-osd-accent);
  }
}

.level-osd-preview__bubble strong {
  font-size: 13px;
  font-weight: 600;
}
</style>
