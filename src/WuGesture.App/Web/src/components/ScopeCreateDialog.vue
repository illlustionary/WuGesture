<script setup>
import { computed, nextTick, ref, watch } from "vue";
import BaseDialog from "./BaseDialog.vue";

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
  <BaseDialog
    :open="open"
    :title="title"
    :description="description"
    :title-id="titleId"
    :show-close="true"
    confirm-text="保存"
    panel-class="scope-create-dialog"
    @close="$emit('close')"
    @confirm="confirm"
  >
    <label class="scope-create-dialog__field">
      <span>{{ label }}</span>
      <input
        ref="inputRef"
        v-model.trim="value"
        class="scope-input"
        :placeholder="placeholder"
        @keydown.enter.prevent="confirm"
      >
    </label>
  </BaseDialog>
</template>

<style scoped lang="scss">
.scope-create-dialog__field {
  display: grid;
  gap: 6px;
  margin-top: 10px;
  color: var(--muted);
  font-size: 13px;
}

:deep(.scope-create-dialog) {
  width: min(480px, 100%);
}
</style>
