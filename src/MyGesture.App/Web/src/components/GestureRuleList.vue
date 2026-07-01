<template>
  <div class="gesture-table">
    <header class="gesture-table__head">
      <span>名称</span>
      <span>手势</span>
      <span>命令</span>
      <span></span>
    </header>

    <div class="gesture-table__body">
      <article
        v-for="rule in rules"
        :key="rule.id"
        class="gesture-table__row"
        @dblclick="$emit('edit', rule.id)"
      >
        <input
          :value="rule.actionName"
          class="gesture-table__cell"
          placeholder="名称"
          @input="$emit('rename', rule, $event.target.value)"
        >
        <button type="button" class="gesture-table__cell gesture-pattern-button" @click="$emit('edit', rule.id)">
          {{ getGestureMnemonic?.(rule) || "未录制" }}
        </button>
        <button
          type="button"
          class="gesture-table__cell hotkey-record-button"
          :class="{ 'is-recording': isRecording?.(rule) }"
          @click.stop="$emit('record', rule)"
        >
          {{ isRecording?.(rule) ? "录制中..." : (rule.keysText || "点击录制") }}
        </button>
        <button type="button" class="ghost-button" @click="$emit('remove', rule.id)">删除</button>
      </article>
    </div>
  </div>
</template>

<script setup>
defineProps({
  rules: { type: Array, required: true },
  isRecording: { type: Function, default: null },
  getGestureMnemonic: { type: Function, default: null }
});

defineEmits(["remove", "record", "edit", "rename"]);
</script>
