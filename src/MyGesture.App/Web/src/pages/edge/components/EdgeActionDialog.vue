<script setup>
import IconActionButton from '../../../components/IconActionButton.vue'
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
        <label class="edge-toggle-field">
          <span>启用</span>
          <span class="edge-toggle">
            <input
              v-model="draft.enabled"
              type="checkbox"
            />
            <span />
          </span>
        </label>

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
          <select
            v-model="draft.actionType"
            class="scope-input"
          >
            <option value="hotkey">快捷键</option>
            <option value="window">窗口控制</option>
            <option value="volume">音量控制</option>
            <option value="brightness">亮度控制</option>
          </select>
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

.edge-toggle-field {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 42px;
  color: var(--muted);
  font-size: 13px;
}

.edge-toggle {
  position: relative;
  display: inline-flex;
  width: 42px;
  height: 24px;
  flex: 0 0 auto;

  input {
    position: absolute;
    opacity: 0;
  }

  span {
    width: 100%;
    border-radius: 999px;
    background: var(--toggle-off-bg);
    transition: background-color 120ms ease;

    &::after {
      content: '';
      position: absolute;
      top: 3px;
      left: 3px;
      width: 18px;
      height: 18px;
      border-radius: 999px;
      background: var(--panel-solid);
      box-shadow: var(--shadow-thumb);
      transition: transform 120ms ease;
    }
  }

  input:checked + span {
    background: var(--accent);

    &::after {
      transform: translateX(18px);
    }
  }
}

@media (max-width: 920px) {
  .edge-dialog__grid {
    grid-template-columns: 1fr;
  }
}
</style>
