<template>
  <div class="rule-list">
    <div class="rule-list__head">
      <div class="rule-list__labels">
        <span>名称</span>
        <span>手势</span>
        <span>命令</span>
        <span></span>
      </div>
    </div>

    <article v-for="rule in rules" :key="rule.id" class="rule-row">
      <input v-model.trim="rule.actionName" class="rule-cell" placeholder="名称">
      <input v-model.trim="rule.patternText" class="rule-cell" placeholder="Left, Right">
      <input v-model.trim="rule.keysText" class="rule-cell" placeholder="Control + W">
      <button type="button" class="ghost-button" @click="$emit('remove', rule.id)">删除</button>
    </article>
  </div>
</template>

<script setup>
defineProps({
  rules: { type: Array, required: true }
});

defineEmits(["remove"]);
</script>
