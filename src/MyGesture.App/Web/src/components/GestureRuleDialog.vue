<script setup>
import { computed } from 'vue'

const props = defineProps({
  open: { type: Boolean, required: true },
  draft: { type: Object, required: true },
  message: { type: String, default: '' },
  isRecordingHotkey: { type: Function, default: null },
  isRecordingGesture: { type: Boolean, default: false },
  getGestureMnemonic: { type: Function, default: null },
  windowOperations: { type: Array, default: () => [] }
})

defineEmits(['close', 'confirm', 'record', 'record-hotkey'])

const patternLabel = computed(
  () => props.getGestureMnemonic?.(props.draft) || '尚未录制'
)
</script>

<template>
  <div
    v-if="open"
    class="modal-backdrop modal-backdrop--gesture"
    @click.self="$emit('close')"
  >
    <section
      class="gesture-dialog"
      role="dialog"
      aria-modal="true"
      aria-labelledby="gesture-dialog-title"
    >
      <!-- <div class="gesture-dialog__chrome"> -->
      <div class="modal-panel gesture-dialog__panel">
        <div class="modal-panel__head">
          <div>
            <h3 id="gesture-dialog-title">手势</h3>
            <p>点击开始录制后，按住右键或中键绘制，松开后由后端识别。</p>
          </div>
          <button
            type="button"
            class="ghost-button"
            @click="$emit('close')"
          >
            关闭
          </button>
        </div>

        <div class="gesture-dialog__grid">
          <label>
            <span>名称</span>
            <input
              v-model.trim="draft.actionName"
              class="scope-input"
              placeholder="例如：关闭标签"
            />
          </label>

          <label>
            <span>命令类型</span>
            <select
              v-model="draft.actionType"
              class="scope-input"
            >
              <option value="hotkey">快捷键</option>
              <option value="window">窗口控制</option>
            </select>
          </label>
        </div>

        <div class="gesture-dialog__command">
          <label v-if="draft.actionType === 'window'">
            <span>操作</span>
            <select
              v-model="draft.windowOperation"
              class="scope-input"
            >
              <option
                v-for="operation in windowOperations"
                :key="operation.value"
                :value="operation.value"
              >
                {{ operation.label }}
              </option>
            </select>
          </label>

          <label v-else>
            <span>操作</span>
            <button
              type="button"
              class="scope-input hotkey-record-button"
              :class="{ 'is-recording': isRecordingHotkey?.(draft) }"
              @click="$emit('record-hotkey', draft)"
            >
              {{
                isRecordingHotkey?.(draft)
                  ? '录制中...'
                  : draft.keysText || '点击录制快捷键'
              }}
            </button>
          </label>
        </div>

        <button
          type="button"
          class="gesture-recorder__trigger"
          :class="{ 'is-recording': isRecordingGesture }"
          @click="$emit('record')"
        >
          <strong>{{ isRecordingGesture ? '停止录制' : '开始录制' }}</strong>
          <span>{{
            isRecordingGesture
              ? '再次点击停止录制。'
              : '点击后立即开始录制，按住右键或中键绘制。'
          }}</span>
        </button>

        <div class="gesture-dialog__result">
          <span>识别结果</span>
          <strong>{{ patternLabel }}</strong>
        </div>

        <p
          v-if="message"
          class="gesture-dialog__message"
        >
          {{ message }}
        </p>

        <div class="modal-panel__actions">
          <button
            type="button"
            class="ghost-button"
            @click="$emit('close')"
          >
            取消
          </button>
          <button
            type="button"
            class="primary-button"
            @click="$emit('confirm')"
          >
            确认
          </button>
        </div>
      </div>
      <!-- </div> -->
    </section>
  </div>
</template>

<style scoped lang="scss">
.gesture-dialog {
  position: relative;
  width: 100%;
  height: 100%;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  // &__chrome {
  //   position: fixed;
  //   top: 18px;
  //   left: 18px;
  //   right: 18px;
  //   display: flex;
  //   justify-content: center;
  //   pointer-events: none;
  // }

  &__panel {
    width: min(760px, calc(100vw - 36px));
    pointer-events: auto;
  }

  &__grid {
    display: grid;
    gap: 10px;
    grid-template-columns: 1fr 1fr;
    margin-bottom: 12px;

    label {
      display: grid;
      gap: 4px;
      color: var(--muted);
      font-size: 13px;
    }
  }

  &__command {
    display: grid;
    gap: 8px;
    margin-bottom: 12px;

    label {
      display: grid;
      gap: 4px;
      color: var(--muted);
      font-size: 13px;
    }
  }

  &__result {
    display: flex;
    align-items: center;
    gap: 10px;
    min-height: 34px;
    padding: 8px 0 0;

    span {
      color: var(--muted);
      font-size: 13px;
    }

    strong {
      font-size: 14px;
    }
  }

  &__message {
    margin-top: 4px;
    color: var(--muted);
    font-size: 13px;
  }
}

.hotkey-record-button {
  overflow: hidden;
  text-align: left;
  white-space: nowrap;
  text-overflow: ellipsis;
  cursor: pointer;
  color: var(--text);
  background: #fff;

  &:hover {
    background: rgba(29, 81, 109, 0.08);
  }

  &.is-recording {
    color: #8a441f;
    background: #fff3df;
  }
}

.gesture-recorder__trigger {
  display: grid;
  gap: 4px;
  width: 100%;
  min-height: 68px;
  margin-bottom: 12px;
  padding: 14px 16px;
  text-align: left;
  border-radius: 18px;
  border: 1px solid rgba(29, 81, 109, 0.18);
  background: linear-gradient(180deg, #ffffff, #f2f7fb);
  box-shadow: 0 10px 20px rgba(18, 30, 42, 0.06);

  strong {
    color: var(--accent-strong);
    font-size: 15px;
  }

  span {
    color: var(--muted);
    font-size: 13px;
  }

  &.is-recording {
    border-color: rgba(173, 76, 57, 0.28);
    background: linear-gradient(180deg, #fff8f5, #ffeede);

    strong {
      color: #8a441f;
    }
  }
}

@media (max-width: 720px) {
  .gesture-dialog__grid {
    grid-template-columns: 1fr;
  }
}
</style>
