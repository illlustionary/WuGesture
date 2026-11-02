<script setup>
import SettingsSectionCard from './SettingsSectionCard.vue'

const emit = defineEmits(['export', 'import', 'reset'])
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
          <button
            type="button"
            class="config-management__button"
            @click="emit('export')"
          >
            导出
          </button>
        </div>

        <div class="config-management__item">
          <div class="config-management__copy">
            <span class="config-management__label">导入配置</span>
            <span class="config-management__desc">
              选择 JSON 文件覆盖当前配置；选择文件后会再次确认，导入后无法撤销。
            </span>
          </div>
          <button
            type="button"
            class="config-management__button"
            @click="emit('import')"
          >
            导入
          </button>
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
          <button
            type="button"
            class="config-management__button config-management__button--danger"
            @click="emit('reset')"
          >
            恢复默认
          </button>
        </div>
      </section>
    </div>
  </SettingsSectionCard>
</template>

<style scoped lang="scss">
.config-management {
  display: grid;
  gap: 22px;
}

.config-management__group {
  display: grid;
  gap: 10px;
}

.config-management__group--danger {
  padding-top: 18px;
  border-top: 1px solid var(--border-subtle);
}

.config-management__title {
  margin: 0;
  font-size: 12px;
  font-weight: 700;
  color: var(--muted);
}

.config-management__item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 16px;
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  background: var(--interactive-bg);
}

.config-management__copy {
  display: grid;
  gap: 4px;
  min-width: 0;
}

.config-management__label {
  font-size: 13px;
  font-weight: 700;
  color: var(--text);
}

.config-management__desc {
  font-size: 12px;
  line-height: 1.55;
  color: var(--muted);
}

.config-management__button {
  flex: 0 0 auto;
  min-height: 34px;
  padding: 0 16px;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  background: var(--panel-control);
  color: var(--text);
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  transition:
    border-color 120ms ease,
    background-color 120ms ease,
    color 120ms ease;

  &:hover {
    border-color: var(--border-strong);
    background: var(--interactive-hover-bg);
  }

  &:focus-visible {
    outline: none;
    box-shadow: 0 0 0 3px var(--focus-ring);
  }
}

.config-management__button--danger {
  color: var(--danger);
  border-color: var(--danger-border);

  &:hover {
    border-color: var(--danger);
  }
}

@media (max-width: 640px) {
  .config-management__item {
    flex-direction: column;
    align-items: stretch;
  }

  .config-management__button {
    width: 100%;
  }
}
</style>
