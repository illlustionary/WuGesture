<script setup lang="ts">
import { ref } from 'vue'
import AppCheckbox from '@/components/form/AppCheckbox.vue'
import BaseInput from '@/components/form/BaseInput.vue'
import BaseRange from '@/components/form/BaseRange.vue'
import CustomSelect from '@/components/form/CustomSelect.vue'
import ToggleCheckbox from '@/components/form/ToggleCheckbox.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppChip from '@/components/ui/AppChip.vue'
import AppDivider from '@/components/ui/AppDivider.vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'
import type { SelectOption } from '@/types/ui'

interface TokenGroup {
  title: string
  roles: string[]
}

interface TypeScaleSample {
  token: string
  label: string
  fallbackSize: string
  fallbackWeight: number
}

interface ShapeSample {
  token: string
  label: string
  fallbackRadius: string
}

const colorGroups: TokenGroup[] = [
  {
    title: 'Primary',
    roles: ['primary', 'on-primary', 'primary-container', 'on-primary-container', 'inverse-primary']
  },
  {
    title: 'Secondary',
    roles: ['secondary', 'on-secondary', 'secondary-container', 'on-secondary-container']
  },
  {
    title: 'Tertiary',
    roles: ['tertiary', 'on-tertiary', 'tertiary-container', 'on-tertiary-container']
  },
  {
    title: 'Error',
    roles: ['error', 'on-error', 'error-container', 'on-error-container']
  },
  {
    title: 'Surface',
    roles: [
      'surface',
      'on-surface',
      'on-surface-variant',
      'surface-dim',
      'surface-bright',
      'surface-container-lowest',
      'surface-container-low',
      'surface-container',
      'surface-container-high',
      'surface-container-highest'
    ]
  },
  {
    title: 'Outline / Inverse',
    roles: ['outline', 'outline-variant', 'inverse-surface', 'inverse-on-surface', 'scrim', 'shadow']
  }
]

const typeScaleSamples: TypeScaleSample[] = [
  { token: 'headline-small', label: '页面标题 24/32', fallbackSize: '24px', fallbackWeight: 500 },
  { token: 'title-large', label: '弹窗标题 22/28', fallbackSize: '22px', fallbackWeight: 500 },
  { token: 'title-medium', label: '区块标题 16/24', fallbackSize: '16px', fallbackWeight: 500 },
  { token: 'title-small', label: '列表项 14/20', fallbackSize: '14px', fallbackWeight: 500 },
  { token: 'body-large', label: '正文 16/24', fallbackSize: '16px', fallbackWeight: 400 },
  { token: 'body-medium', label: '表单标签 14/20', fallbackSize: '14px', fallbackWeight: 400 },
  { token: 'body-small', label: '次要信息 12/16', fallbackSize: '12px', fallbackWeight: 400 },
  { token: 'label-large', label: '按钮文字 14/20', fallbackSize: '14px', fallbackWeight: 500 },
  { token: 'label-medium', label: '表头 / Chip 12/16', fallbackSize: '12px', fallbackWeight: 500 },
  { token: 'label-small', label: '快捷键 11/16', fallbackSize: '11px', fallbackWeight: 500 }
]

const shapeSamples: ShapeSample[] = [
  { token: 'extra-small', label: 'extra-small 4', fallbackRadius: '4px' },
  { token: 'small', label: 'small 8', fallbackRadius: '8px' },
  { token: 'medium', label: 'medium 12', fallbackRadius: '12px' },
  { token: 'large', label: 'large 16', fallbackRadius: '16px' },
  { token: 'extra-large', label: 'extra-large 28', fallbackRadius: '28px' },
  { token: 'full', label: 'full 999', fallbackRadius: '999px' }
]

const spacingSteps = [4, 8, 12, 16, 24, 32, 48]

/* 阶段 2 基础控件示例状态 */
const textValue = ref('')
const selectValue = ref('a')
const switchValue = ref(true)
const checkboxValue = ref(true)
const rangeValue = ref(40)
const CHIP_POOL = ['窗口控制', '音量控制', '启动程序']
const chips = ref([...CHIP_POOL])

const demoOptions: SelectOption[] = [
  { value: 'a', label: '窗口控制：最大化/还原' },
  { value: 'b', label: '音量控制：音量 + 5' },
  { value: 'c', label: '快捷键：Ctrl + Shift + Esc' },
  { value: 'd', label: '启动程序（禁用示例）', disabled: true }
]

function removeChip(chip: string) {
  chips.value = chips.value.filter(item => item !== chip)
}

function resetChips() {
  chips.value = [...CHIP_POOL]
}
</script>

<template>
  <section class="md3-preview">
    <header class="md3-preview__head">
      <p class="md3-preview__flag">仅开发环境可见，不会进入构建产物</p>
      <h1>MD3 设计令牌预览</h1>
      <p class="md3-preview__desc">
        阶段 0 建立页面骨架；阶段 1 注入 <code>--md-sys-color-*</code> / <code>--md-sys-typescale-*</code> /
        <code>--md-sys-shape-*</code> 后，本页会自动显示真实取值。
      </p>
    </header>

    <section
      v-for="group in colorGroups"
      :key="group.title"
      class="md3-preview__section"
    >
      <h2>{{ group.title }}</h2>
      <div class="md3-preview__swatches">
        <div
          v-for="role in group.roles"
          :key="role"
          class="md3-preview__swatch"
          :style="{ background: `var(--md-sys-color-${role}, transparent)` }"
        >
          <span>{{ role }}</span>
        </div>
      </div>
    </section>

    <section class="md3-preview__section">
      <h2>Typography</h2>
      <div class="md3-preview__stack">
        <p
          v-for="sample in typeScaleSamples"
          :key="sample.token"
          :style="{
            fontSize: `var(--md-sys-typescale-${sample.token}-size, ${sample.fallbackSize})`,
            fontWeight: `var(--md-sys-typescale-${sample.token}-weight, ${sample.fallbackWeight})`
          }"
        >
          {{ sample.label }} · 手势规则 WuGesture
        </p>
      </div>
    </section>

    <section class="md3-preview__section">
      <h2>Shape</h2>
      <div class="md3-preview__shapes">
        <div
          v-for="shape in shapeSamples"
          :key="shape.token"
          class="md3-preview__shape"
          :style="{ borderRadius: `var(--md-sys-shape-corner-${shape.token}, ${shape.fallbackRadius})` }"
        >
          {{ shape.label }}
        </div>
      </div>
    </section>

    <section class="md3-preview__section">
      <h2>Spacing（4dp 网格）</h2>
      <div class="md3-preview__spacing">
        <div
          v-for="step in spacingSteps"
          :key="step"
          class="md3-preview__spacing-item"
        >
          <span
            class="md3-preview__spacing-bar"
            :style="{ width: `${step}px` }"
          ></span>
          <span>{{ step }}</span>
        </div>
      </div>
    </section>

    <section class="md3-preview__section">
      <h2>Components（阶段 2）</h2>

      <h3>Button</h3>
      <div class="md3-preview__row">
        <AppButton variant="filled">确定</AppButton>
        <AppButton variant="tonal">次要操作</AppButton>
        <AppButton variant="outlined">描边按钮</AppButton>
        <AppButton variant="text">文字按钮</AppButton>
        <AppButton variant="danger">删除</AppButton>
        <AppButton
          variant="filled"
          size="sm"
          >Small</AppButton
        >
        <AppButton
          variant="filled"
          disabled
          >Disabled</AppButton
        >
      </div>

      <h3>IconButton / Tooltip</h3>
      <div class="md3-preview__row">
        <IconActionButton
          icon="add"
          label="添加规则"
          class="primary-button"
        />
        <IconActionButton
          icon="delete"
          label="删除规则"
          class="icon-button icon-button--danger"
        />
        <IconActionButton
          icon="test"
          label="悬停可看气泡提示"
          class="secondary-button"
          color="var(--md-sys-color-primary)"
        />
        <span class="md3-preview__hint">鼠标悬停或 Tab 聚焦图标按钮查看 Tooltip</span>
      </div>

      <h3>Chip</h3>
      <div class="md3-preview__row">
        <AppChip>辅助</AppChip>
        <AppChip selected>已选中</AppChip>
        <AppChip disabled>禁用</AppChip>
        <AppChip
          v-for="chip in chips"
          :key="chip"
          removable
          @remove="removeChip(chip)"
        >
          {{ chip }}
        </AppChip>
        <AppButton
          v-if="chips.length < CHIP_POOL.length"
          variant="text"
          size="sm"
          @click="resetChips"
        >
          恢复
        </AppButton>
      </div>

      <h3>TextField / Select</h3>
      <div class="md3-preview__grid">
        <BaseInput
          v-model="textValue"
          placeholder="输入手势名称"
        />
        <CustomSelect
          v-model="selectValue"
          :options="demoOptions"
          placeholder="选择命令类型"
        />
      </div>

      <h3>Switch / Checkbox / Slider</h3>
      <div class="md3-preview__stack">
        <ToggleCheckbox
          v-model="switchValue"
          label="启动时自动检查更新"
          note="只比较版本号，不会自动下载"
        />
        <AppCheckbox
          v-model="checkboxValue"
          label="启用手势轨迹提示"
        />
        <BaseRange
          v-model.number="rangeValue"
          :min="0"
          :max="100"
        />
      </div>

      <h3>Divider</h3>
      <div class="md3-preview__stack">
        <AppDivider />
        <AppDivider inset />
        <div class="md3-preview__row">
          <span>左侧</span>
          <AppDivider vertical />
          <span>右侧</span>
        </div>
      </div>
    </section>
  </section>
</template>

<style scoped>
.md3-preview {
  max-width: 1040px;
  padding: 32px;
  color: var(--md-sys-color-on-surface, #1a1c1e);
}

.md3-preview__head {
  display: grid;
  gap: 8px;
  margin-bottom: 32px;
}

.md3-preview__flag {
  display: inline-flex;
  width: fit-content;
  padding: 4px 10px;
  border-radius: 999px;
  background: var(--md-sys-color-error-container, #ffdad6);
  color: var(--md-sys-color-on-error-container, #410002);
  font-size: 12px;
  font-weight: 500;
}

.md3-preview h1 {
  margin: 0;
  font-size: var(--md-sys-typescale-headline-small-size, 24px);
  font-weight: var(--md-sys-typescale-headline-small-weight, 500);
}

.md3-preview__desc {
  margin: 0;
  max-width: 720px;
  color: var(--md-sys-color-on-surface-variant, #43474e);
  font-size: 14px;
  line-height: 20px;
}

.md3-preview__section {
  margin-bottom: 32px;
}

.md3-preview__section h2 {
  margin: 0 0 16px;
  font-size: var(--md-sys-typescale-title-medium-size, 16px);
  font-weight: var(--md-sys-typescale-title-medium-weight, 500);
}

.md3-preview__section h3 {
  margin: 24px 0 12px;
  color: var(--md-sys-color-on-surface-variant, #43474e);
  font-size: var(--md-sys-typescale-label-large-size, 14px);
  font-weight: var(--md-sys-typescale-label-large-weight, 500);
}

.md3-preview__row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
}

.md3-preview__grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
  align-items: start;
}

.md3-preview__hint {
  color: var(--md-sys-color-on-surface-variant, #43474e);
  font-size: var(--md-sys-typescale-body-small-size, 12px);
}

.md3-preview__swatches {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
  gap: 12px;
}

.md3-preview__swatch {
  display: flex;
  align-items: flex-end;
  min-height: 72px;
  padding: 8px;
  border: 1px solid var(--md-sys-color-outline-variant, #c3c7cf);
  border-radius: 12px;
  background-image:
    linear-gradient(45deg, #ece6f0 25%, transparent 25%), linear-gradient(-45deg, #ece6f0 25%, transparent 25%);
  background-size: 12px 12px;
}

.md3-preview__swatch span {
  padding: 2px 6px;
  border-radius: 4px;
  background: rgba(255, 255, 255, 0.72);
  color: #1a1c1e;
  font-size: 11px;
  font-family: 'Cascadia Mono', Consolas, monospace;
}

.md3-preview__stack {
  display: grid;
  gap: 12px;
}

.md3-preview__stack p {
  margin: 0;
}

.md3-preview__shapes {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.md3-preview__shape {
  display: grid;
  place-items: center;
  width: 120px;
  height: 72px;
  border: 1px solid var(--md-sys-color-outline-variant, #c3c7cf);
  background: var(--md-sys-color-surface-container-high, #ece6f0);
  font-size: 12px;
}

.md3-preview__spacing {
  display: grid;
  gap: 8px;
}

.md3-preview__spacing-item {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 12px;
  color: var(--md-sys-color-on-surface-variant, #43474e);
}

.md3-preview__spacing-bar {
  height: 8px;
  border-radius: 2px;
  background: var(--md-sys-color-primary, #0061a4);
}
</style>
