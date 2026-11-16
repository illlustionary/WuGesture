<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import BaseDialog from '@/components/dialog/BaseDialog.vue'

const props = withDefaults(
  defineProps<{
    open: boolean
    title: string
    description?: string
    label: string
    placeholder?: string
    modelValue?: string
  }>(),
  {
    description: '',
    placeholder: '',
    modelValue: ''
  }
)

const emit = defineEmits<{
  close: []
  confirm: []
  'update:modelValue': [value: string]
}>()

const titleId = 'scope-create-dialog-title'
const inputRef = ref<HTMLInputElement | null>(null)

const value = computed({
  get() {
    return props.modelValue
  },
  set(nextValue: string) {
    emit('update:modelValue', nextValue)
  }
})

function confirm() {
  emit('confirm')
}

watch(
  () => props.open,
  async isOpen => {
    if (!isOpen) {
      return
    }

    await nextTick()
    inputRef.value?.focus()
    inputRef.value?.select()
  }
)
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
    @close="emit('close')"
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
      />
    </label>
  </BaseDialog>
</template>

<style scoped lang="scss">
.scope-create-dialog__field {
  display: grid;
  gap: var(--md-sys-spacing-sm);
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-label-large-size);
  line-height: var(--md-sys-typescale-label-large-line-height);
}

:deep(.scope-create-dialog) {
  width: min(480px, calc(100vw - 36px));
}
</style>
