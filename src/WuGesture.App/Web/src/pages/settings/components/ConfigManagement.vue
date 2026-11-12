<script setup lang="ts">
import AppButton from '@/components/ui/AppButton.vue'
import SettingsSectionCard from './SettingsSectionCard.vue'

const emit = defineEmits<{
  export: []
  import: []
  reset: []
}>()
</script>

<template>
  <SettingsSectionCard
    title="配置管理"
    note="导出和导入只处理主配置 JSON：包含手势规则、分类、程序、边缘操作、界面与行为设置、WebDAV 和更新设置，不包含窗口位置和大小。导入和导出需要在 WuGesture 桌面应用中完成。"
  >
    <div class="config-management">
      <section class="config-management__group">
        <h4 class="config-management__title">备份与迁移</h4>

        <div class="config-management__item">
          <div class="config-management__copy">
            <span class="config-management__label">导出配置</span>
            <span class="config-management__desc"> 把当前配置保存为 JSON 文件，便于备份或迁移到其他电脑。 </span>
          </div>
          <AppButton
            variant="outlined"
            @click="emit('export')"
          >
            导出
          </AppButton>
        </div>

        <div class="config-management__item">
          <div class="config-management__copy">
            <span class="config-management__label">导入配置</span>
            <span class="config-management__desc">
              选择 JSON 文件覆盖当前配置；选择文件后会再次确认，导入后无法撤销。
            </span>
          </div>
          <AppButton
            variant="outlined"
            @click="emit('import')"
          >
            导入
          </AppButton>
        </div>
      </section>

      <section class="config-management__group config-management__group--danger">
        <h4 class="config-management__title">恢复默认</h4>

        <div class="config-management__item">
          <div class="config-management__copy">
            <span class="config-management__label">恢复默认设置</span>
            <span class="config-management__desc">
              把轨迹线、底部提示窗、应用行为、排除项、WebDAV
              和更新设置恢复为默认值；手势规则、分类、程序和边缘操作不会改变。
            </span>
          </div>
          <AppButton
            variant="danger"
            @click="emit('reset')"
          >
            恢复默认
          </AppButton>
        </div>
      </section>
    </div>
  </SettingsSectionCard>
</template>

<style scoped lang="scss">
.config-management {
  display: grid;
  gap: var(--md-sys-spacing-xl);
}

.config-management__group {
  display: grid;
  gap: var(--md-sys-spacing-md);
}

.config-management__group--danger {
  padding-top: var(--md-sys-spacing-lg);
  border-top: 1px solid var(--md-sys-color-outline-variant);
}

.config-management__title {
  margin: 0;
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-label-large-size);
  line-height: var(--md-sys-typescale-label-large-line-height);
  font-weight: var(--md-sys-typescale-label-large-weight);
}

.config-management__item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--md-sys-spacing-lg);
  padding: var(--md-sys-spacing-md) var(--md-sys-spacing-lg);
  border-radius: var(--md-sys-shape-corner-small);
  background: var(--md-sys-color-surface-container);
}

.config-management__copy {
  display: grid;
  gap: var(--md-sys-spacing-xs);
  min-width: 0;
}

.config-management__label {
  color: var(--md-sys-color-on-surface);
  font-size: var(--md-sys-typescale-body-medium-size);
  line-height: var(--md-sys-typescale-body-medium-line-height);
  font-weight: 600;
}

.config-management__desc {
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
}

@media (max-width: 640px) {
  .config-management__item {
    flex-direction: column;
    align-items: stretch;
  }

  .config-management__item :deep(.app-button) {
    width: 100%;
  }
}
</style>
