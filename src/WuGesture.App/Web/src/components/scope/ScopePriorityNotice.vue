<template>
  <div class="scope-priority-notice">
    <span class="scope-priority-notice__label">生效顺序</span>
    <strong>程序</strong>
    <span aria-hidden="true">›</span>
    <strong>分类</strong>
    <span aria-hidden="true">›</span>
    <strong>全局</strong>
  </div>
</template>

<style scoped lang="scss">
.scope-priority-notice {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--md-sys-spacing-xs);
  padding: var(--md-sys-spacing-md);
  border-radius: var(--md-sys-shape-corner-small);
  background: var(--md-sys-color-tertiary-container);
  color: var(--md-sys-color-on-tertiary-container);
  font-size: var(--md-sys-typescale-body-medium-size);
  line-height: var(--md-sys-typescale-body-medium-line-height);

  strong {
    font-weight: 600;
  }
}

.scope-priority-notice__label {
  margin-right: var(--md-sys-spacing-xs);
  color: var(--md-sys-color-on-tertiary-container);
  font-weight: 500;
  opacity: 0.8;
}
</style>
