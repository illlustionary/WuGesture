<script setup>
import { computed, nextTick, ref, watch } from "vue";

const props = defineProps({
  open: { type: Boolean, required: true },
  title: { type: String, required: true },
  description: { type: String, default: "" },
  label: { type: String, required: true },
  placeholder: { type: String, default: "" },
  modelValue: { type: String, default: "" }
});

const emit = defineEmits(["close", "confirm", "update:modelValue"]);
const titleId = "scope-create-dialog-title";
const inputRef = ref(null);

const value = computed({
  get() {
    return props.modelValue;
  },
  set(nextValue) {
    emit("update:modelValue", nextValue);
  }
});

function confirm() {
  emit("confirm");
}

watch(
  () => props.open,
  async (isOpen) => {
    if (!isOpen) {
      return;
    }

    await nextTick();
    inputRef.value?.focus?.();
    inputRef.value?.select?.();
  }
);
</script>

<template>
  <div v-if="open" class="modal-backdrop" @click.self="$emit('close')">
    <section class="modal-panel scope-create-dialog" role="dialog" aria-modal="true" :aria-labelledby="titleId">
      <div class="modal-panel__head">
        <div>
          <h3 :id="titleId">{{ title }}</h3>
          <p v-if="description">{{ description }}</p>
        </div>
        <button type="button" class="scope-create-dialog__close" aria-label="关闭" title="关闭" @click="$emit('close')">
          <span aria-hidden="true">×</span>
        </button>
      </div>

      <label class="scope-create-dialog__field">
        <span>{{ label }}</span>
        <input
          ref="inputRef"
          v-model.trim="value"
          class="scope-input"
          :placeholder="placeholder"
          @blur="confirm"
          @keydown.enter.prevent="confirm"
        >
      </label>
    </section>
  </div>
</template>

<style scoped lang="scss">
.scope-create-dialog {
  width: min(480px, 100%);

  .modal-panel__head {
    align-items: flex-start;
  }

  &__field {
    display: grid;
    gap: 6px;
    margin-top: 10px;
    color: var(--muted);
    font-size: 13px;
  }

  &__close {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 34px;
    height: 34px;
    margin: 0;
    padding: 0;
    border: none;
    border-radius: 999px;
    background: transparent;
    color: var(--muted);
    font-size: 22px;
    line-height: 1;
    cursor: pointer;

    &:hover {
      background: rgba(18, 30, 42, 0.06);
      color: var(--text);
    }
  }
}
</style>
