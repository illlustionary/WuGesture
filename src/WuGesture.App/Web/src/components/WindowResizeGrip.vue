<script setup>
import { useGestureEditorLifecycleStore } from '@/gestureEditor/stores/useGestureEditorLifecycleStore'
import TriangleIcon from '@/assets/window/triangle.svg'

const lifecycle = useGestureEditorLifecycleStore()

function startResize() {
  lifecycle.startWindowResize()
}
</script>

<template>
  <button
    type="button"
    class="window-resize-grip"
    aria-label="调整窗口大小"
    @pointerdown.prevent="startResize"
  >
    <TriangleIcon
      class="window-resize-grip__icon"
      aria-hidden="true"
    />
  </button>
</template>

<style scoped lang="scss">
.window-resize-grip {
  position: absolute;
  right: 0.5vw;
  bottom: 0.5vw;
  z-index: 10;
  display: grid;
  place-items: center;
  width: 1vw;
  height: 1vw;
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--muted);
  cursor: nwse-resize;

  &:hover {
    color: var(--text);
  }

  :deep(.window-resize-grip__icon) {
    width: 100%;
    height: 100%;
    fill: currentColor;
    transform: rotate(180deg);
  }
}
</style>
