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
          @blur="$emit('rename', rule, $event.target.value)"
        >
        <button type="button" class="gesture-table__cell gesture-pattern-button" @click="$emit('edit', rule.id)">
          {{ getGestureMnemonic?.(rule) || "未录制" }}
        </button>
        <button
          type="button"
          class="gesture-table__cell command-button"
          @click.stop="$emit('edit', rule.id)"
        >
          {{ getActionLabel?.(rule) || "点击设置" }}
        </button>
        <button type="button" class="ghost-button" @click="$emit('remove', rule.id)">删除</button>
      </article>
    </div>
  </div>
</template>

<script setup>
defineProps({
  rules: { type: Array, required: true },
  getGestureMnemonic: { type: Function, default: null },
  getActionLabel: { type: Function, default: null }
});

defineEmits(["remove", "edit", "rename"]);
</script>

<style scoped lang="scss">
.gesture-table {
  display: flex;
  flex-direction: column;
  gap: 0;
  overflow: hidden;
  border: 1px solid var(--border);
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.96);

  &__body {
    display: grid;
    gap: 0;
  }

  &__head,
  &__row {
    display: grid;
    grid-template-columns: 1.2fr 1fr 1.2fr auto;
    gap: 0;
    align-items: center;
  }

  &__head {
    padding: 10px 12px 8px;
    color: var(--accent-strong);
    font-size: 14px;
    background: rgba(29, 81, 109, 0.12);
    border-bottom: 1px solid rgba(20, 30, 40, 0.08);
  }

  &__row {
    padding: 8px 12px;
    border-top: 1px solid rgba(20, 30, 40, 0.06);

    &:hover {
      background: rgba(29, 81, 109, 0.05);
    }

    .ghost-button {
      border: 0;
      background: transparent;
      color: var(--accent-strong);

      &:hover {
        background: rgba(29, 81, 109, 0.08);
      }
    }
  }

  &__cell {
    width: 100%;
    min-height: 30px;
    padding: 4px 8px;
    border: 0;
    border-right: 1px solid rgba(20, 30, 40, 0.08);
    border-radius: 0;
    background: #ffffff;
    transition: border-color 120ms ease, box-shadow 120ms ease;

    &:focus {
      outline: none;
      border-color: rgba(29, 81, 109, 0.45);
      box-shadow: 0 0 0 3px rgba(29, 81, 109, 0.1);
    }

    &:last-of-type {
      border-right: 0;
    }
  }
}

.gesture-pattern-button {
  min-height: 30px;
  padding: 4px 8px;
  overflow: hidden;
  text-align: left;
  white-space: nowrap;
  text-overflow: ellipsis;
  border-top: 0;
  border-bottom: 0;
  border-left: 0;
  background: #fff;
}

.command-button {
  overflow: hidden;
  text-align: left;
  white-space: nowrap;
  text-overflow: ellipsis;
  cursor: pointer;
  color: var(--text);
  background: #fff;
}

.gesture-pattern-button:hover,
.command-button:hover {
  background: rgba(29, 81, 109, 0.08);
}

@media (max-width: 720px) {
  .gesture-table {
    &__head,
    &__row {
      grid-template-columns: 1fr;
    }
  }
}
</style>
