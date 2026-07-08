<script setup>
import CustomSelect from '../../../components/CustomSelect.vue'
import IconActionButton from '../../../components/IconActionButton.vue'
import ToggleCheckbox from '../../../components/ToggleCheckbox.vue'
import EdgeActionCommandFields from './EdgeActionCommandFields.vue'

defineProps({
  action: { type: Object, required: true },
  draft: { type: Object, required: true },
  editor: { type: Object, required: true },
  title: { type: String, required: true },
  operationOptions: { type: Function, required: true },
  operationModel: { type: Function, required: true }
})

const emit = defineEmits(['close', 'record-hotkey', 'update-operation'])

const actionTypeOptions = [
  { value: 'hotkey', label: '快捷键' },
  { value: 'window', label: '窗口控制' },
  { value: 'volume', label: '音量控制' },
  { value: 'brightness', label: '亮度控制' }
]
</script>

<template>
  <div
    class="modal-backdrop"
    @click.self="emit('close')"
  >
    <section
      class="modal-panel edge-dialog"
      role="dialog"
      aria-modal="true"
      aria-labelledby="edge-dialog-title"
      tabindex="-1"
      @keydown.esc.prevent="emit('close')"
    >
      <div class="modal-panel__head">
        <div>
          <h3 id="edge-dialog-title">{{ title }}</h3>
          <p>关闭弹窗后自动保存。</p>
        </div>
        <IconActionButton
          icon="close"
          label="关闭"
          class="ghost-button"
          @click="emit('close')"
        />
      </div>

      <div class="edge-dialog__grid">
        <ToggleCheckbox
          v-model="draft.enabled"
          label="启用"
          class="edge-dialog__enabled"
        />

        <label
          v-if="draft.triggerType === 'friction'"
          class="edge-field"
        >
          <span>摩擦次数</span>
          <input
            v-model.number="draft.frictionCount"
            class="scope-input"
            type="number"
            min="1"
            max="20"
          />
        </label>

        <label class="edge-field">
          <span>命令类型</span>
          <CustomSelect
            v-model="draft.actionType"
            :options="actionTypeOptions"
            placeholder="选择命令类型"
          />
        </label>
      </div>

      <EdgeActionCommandFields
        :draft="draft"
        :editor="editor"
        :operation-options="operationOptions"
        :operation-model="operationModel"
        @record-hotkey="emit('record-hotkey')"
        @update-operation="emit('update-operation', $event)"
      />
    </section>
  </div>
</template>

<style scoped lang="scss">
.edge-dialog {
  width: min(620px, calc(100vw - 36px));

  &__grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 12px;
  }
}

.edge-field {
  display: grid;
  gap: 6px;
  color: var(--muted);
  font-size: 13px;
}

.edge-dialog__enabled {
  min-height: 42px;
}

@media (max-width: 920px) {
  .edge-dialog__grid {
    grid-template-columns: 1fr;
  }
}
</style>
