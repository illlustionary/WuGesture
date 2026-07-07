<script setup>
import SettingsSectionCard from './SettingsSectionCard.vue'

defineProps({
  trailPreviewStyle: { type: Object, required: true },
  hintPreviewStyle: { type: Object, required: true }
})
</script>

<template>
  <SettingsSectionCard
    title="实时预览"
    description="这里展示轨迹线和底部提示窗的当前外观。"
  >
    <div class="settings-preview">
      <div
        class="settings-preview__trail"
        :style="trailPreviewStyle"
      >
        <div class="settings-preview__container">
          <span class="settings-preview__key settings-preview__key--inactive">
            未激活
          </span>
          <div class="settings-preview__path settings-preview__path--inactive" />
        </div>
        <div class="settings-preview__container">
          <span class="settings-preview__key settings-preview__key--active">
            激活
          </span>
          <div class="settings-preview__path settings-preview__path--active" />
        </div>
      </div>

      <div
        class="settings-preview__hint"
        :style="hintPreviewStyle"
      >
        <strong>已触发：关闭标签</strong>
        <span>这里是底部提示窗示例。</span>
      </div>
    </div>
  </SettingsSectionCard>
</template>

<style scoped lang="scss">
.settings-preview {
  display: grid;
  gap: 14px;
  grid-template-columns: minmax(0, 1fr) 320px;
  align-items: center;

  &__trail {
    display: grid;
    gap: 18px;
    padding: 24px;
    min-height: 180px;
    border-radius: 22px;
    background:
      radial-gradient(
        circle at 20% 30%,
        var(--accent-soft),
        transparent 28%
      ),
      var(--panel-inset);
    border: 1px solid var(--border-subtle);
  }

  &__container {
    display: flex;
    align-items: center;
    flex-wrap: nowrap;
  }

  &__path {
    height: 0;
    border-radius: 999px;

    &--inactive {
      width: 72%;
      border-top: var(--trail-inactive-width) solid var(--trail-inactive-stroke);
    }

    &--active {
      width: 72%;
      border-top: var(--trail-active-width) solid var(--trail-active-stroke);
    }
  }

  &__key {
    display: inline-flex;
    align-items: center;
    min-height: 28px;
    flex-basis: 80px;
    padding: 0 10px;
    border-radius: 999px;
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

  &__hint {
    display: grid;
    gap: 8px;
    align-self: end;
    justify-self: center;
    margin-bottom: var(--hint-bottom-offset);
    padding: 16px 28px;
    border-radius: var(--hint-radius);
    color: var(--hint-color);
    background: var(--hint-background-rgba);
    box-shadow: var(--shadow-popover);
    border: 1px solid var(--border-inverse);

    strong {
      font-size: var(--hint-font-size);
      line-height: 1.1;
    }

    span {
      color: var(--hint-muted-color);
      font-size: 13px;
    }
  }
}

@media (max-width: 960px) {
  .settings-preview {
    grid-template-columns: 1fr;
  }
}
</style>
