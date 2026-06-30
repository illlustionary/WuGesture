<template>
  <div class="gesture-table">
    <div class="gesture-table__head">
      <span>名称</span>
      <span>手势</span>
      <span>命令</span>
      <span></span>
    </div>

    <article v-for="rule in rules" :key="rule.id" class="gesture-table__row">
      <input v-model.trim="rule.actionName" class="gesture-table__cell" placeholder="名称">
      <input v-model.trim="rule.patternText" class="gesture-table__cell" placeholder="Left, Right">
      <input
        v-model.trim="rule.keysText"
        class="gesture-table__cell"
        placeholder="Control + W"
        @focus="$emit('record', $event.target)"
        @blur="$emit('stop-record')"
      >
      <button type="button" class="ghost-button" @click="$emit('remove', rule.id)">删除</button>
    </article>
  </div>
</template>

<script setup>
defineProps({
  rules: { type: Array, required: true }
});

defineEmits(["remove", "record", "stop-record"]);
</script>
