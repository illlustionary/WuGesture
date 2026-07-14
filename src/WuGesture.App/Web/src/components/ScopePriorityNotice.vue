<script setup></script>

<template>
  <div class="scope-priority-notice">
    <div class="scope-priority-notice__order">
      <span>生效顺序</span>
      <strong>程序</strong>
      <span>›</span>
      <strong>分类</strong>
      <span>›</span>
      <strong>全局</strong>
    </div>
  </div>
</template>

<style scoped lang="scss">
.scope-priority-notice {
  display: grid;
  gap: 6px;
  padding: 12px 14px;
  border: 1px solid var(--accent-border);
  border-radius: 14px;
  background: var(--accent-soft);

  &__order {
    display: inline-flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 6px;
    color: var(--accent-strong);
    font-size: 12px;
    font-weight: 700;
  }

  &__order span {
    color: var(--muted);
    font-weight: 600;
  }

  p {
    color: var(--muted);
    font-size: 12px;
    line-height: 1.5;
  }
}
</style>
