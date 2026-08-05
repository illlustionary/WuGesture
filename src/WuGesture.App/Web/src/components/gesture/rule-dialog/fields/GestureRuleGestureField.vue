<script setup>
import AppIcon from '@/components/ui/AppIcon.vue'

defineProps({
  patternLabel: { type: String, required: true },
  isEmpty: { type: Boolean, default: false },
  isRecording: { type: Boolean, default: false },
  message: { type: String, default: '' }
})

defineEmits(['record'])
</script>

<template>
  <section
    class="gesture-rule-gesture-field"
    aria-labelledby="gesture-dialog-gesture-label"
  >
    <span
      id="gesture-dialog-gesture-label"
      class="gesture-rule-gesture-field__label"
      >手势</span
    >
    <div class="gesture-rule-gesture-field__control">
      <strong :class="{ 'is-empty': isEmpty }">{{ patternLabel }}</strong>
      <button
        type="button"
        class="gesture-rule-gesture-field__record"
        :class="{ 'is-recording': isRecording }"
        @click="$emit('record')"
      >
        <AppIcon
          name="record"
          class="gesture-rule-gesture-field__record-icon"
          aria-hidden="true"
        />
        <span>{{ isRecording ? '停止录制' : isEmpty ? '录制手势' : '重新录制' }}</span>
      </button>
    </div>
    <p
      v-if="message"
      class="gesture-rule-gesture-field__message"
    >
      {{ message }}
    </p>
  </section>
</template>

<style scoped lang="scss">
.gesture-rule-gesture-field {
  display: grid;
  gap: 6px;
}

.gesture-rule-gesture-field__label {
  color: var(--muted);
  font-size: 13px;
  font-weight: 600;
}

.gesture-rule-gesture-field__control {
  display: flex;
  align-items: center;
  min-height: 52px;
  overflow: hidden;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  background: var(--panel-inset);

  strong {
    min-width: 0;
    flex: 1 1 auto;
    padding: 0 14px;
    overflow: hidden;
    color: var(--text);
    font-size: 14px;
    text-overflow: ellipsis;
    white-space: nowrap;

    &.is-empty {
      color: var(--text-subtle);
      font-weight: 500;
    }
  }
}

.gesture-rule-gesture-field__record {
  display: flex;
  align-self: stretch;
  align-items: center;
  justify-content: center;
  gap: 7px;
  min-width: 128px;
  padding: 0 16px;
  border: 0;
  border-left: 1px solid var(--border-muted);
  background: transparent;
  color: var(--accent-strong);
  cursor: pointer;
  transition:
    background-color 120ms ease,
    color 120ms ease;

  &:hover {
    background: var(--accent-soft);
  }

  &.is-recording {
    background: var(--recording-bg);
    color: var(--recording-text);
  }
}

.gesture-rule-gesture-field__record-icon {
  width: 16px;
  height: 16px;
}

.gesture-rule-gesture-field__message {
  color: var(--muted);
  font-size: 13px;
  line-height: 1.45;
}

@media (max-width: 720px) {
  .gesture-rule-gesture-field__control {
    flex-direction: column;

    .gesture-rule-gesture-field__record {
      width: 100%;
      min-height: 40px;
      border-top: 1px solid var(--border-muted);
      border-left: 0;
    }
  }
}
</style>
