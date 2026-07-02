<script setup>
import { computed } from "vue";
import IconActionButton from "./IconActionButton.vue";

const props = defineProps({
  open: { type: Boolean, required: true },
  title: { type: String, required: true },
  description: { type: String, default: "" },
  label: { type: String, required: true },
  placeholder: { type: String, default: "" },
  confirmText: { type: String, default: "确认" },
  modelValue: { type: String, default: "" }
});

const emit = defineEmits(["close", "confirm", "update:modelValue"]);
const titleId = "scope-create-dialog-title";

const value = computed({
  get() {
    return props.modelValue;
  },
  set(nextValue) {
    emit("update:modelValue", nextValue);
  }
});
</script>

<template>
  <div v-if="open" class="modal-backdrop" @click.self="$emit('close')">
    <section class="modal-panel scope-create-dialog" role="dialog" aria-modal="true" :aria-labelledby="titleId">
      <div class="modal-panel__head">
        <div>
          <h3 :id="titleId">{{ title }}</h3>
          <p v-if="description">{{ description }}</p>
        </div>
        <IconActionButton
          icon="close"
          label="关闭"
          class="ghost-button"
          tone="muted"
          @click="$emit('close')"
        />
      </div>

      <label class="scope-create-dialog__field">
        <span>{{ label }}</span>
        <input
          v-model.trim="value"
          class="scope-input"
          :placeholder="placeholder"
          @keydown.enter.prevent="$emit('confirm')"
        >
      </label>

      <div class="modal-panel__actions">
        <button type="button" class="ghost-button" @click="$emit('close')">取消</button>
        <IconActionButton
          icon="add"
          :label="confirmText"
          class="primary-button"
          @click="$emit('confirm')"
        />
      </div>
    </section>
  </div>
</template>

<style scoped lang="scss">
.scope-create-dialog {
  width: min(480px, 100%);

  &__field {
    display: grid;
    gap: 6px;
    margin-top: 10px;
    color: var(--muted);
    font-size: 13px;
  }
}
</style>
