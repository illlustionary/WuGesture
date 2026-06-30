<template>
  <aside class="scope-panel">
    <div class="scope-panel__head">
      <div>
        <h3>{{ title }}</h3>
        <p>{{ description }}</p>
      </div>
    </div>

    <div class="scope-panel__actions">
      <input
        v-model.trim="draft"
        class="scope-input"
        :placeholder="placeholder"
        @keydown.enter.prevent="$emit('create', draft)"
      >
      <button type="button" class="secondary-button" @click="$emit('create', draft)">新增</button>
    </div>

    <div v-if="items.length > 0" class="scope-list">
      <button
        v-for="item in items"
        :key="item.name"
        type="button"
        class="scope-item"
        :class="{ active: item.name === selectedName }"
        @click="$emit('select', item.name)"
      >
        <span>{{ item.name }}</span>
        <small>{{ item.count }} 条</small>
      </button>
    </div>

    <div v-else class="empty-state">{{ emptyText }}</div>

    <div v-if="selectedName" class="scope-editor">
      <label>
        当前{{ title }}
        <input :value="selectedName" @input="$emit('rename', $event.target.value)">
      </label>
      <div class="scope-editor__actions">
        <button type="button" class="danger-button" @click="$emit('delete')">删除当前项</button>
      </div>
    </div>
  </aside>
</template>

<script setup>
defineProps({
  title: { type: String, required: true },
  description: { type: String, required: true },
  placeholder: { type: String, required: true },
  emptyText: { type: String, required: true },
  items: { type: Array, required: true },
  selectedName: { type: String, default: "" },
  draft: { type: String, default: "" }
});

defineEmits(["create", "select", "rename", "delete"]);
</script>
