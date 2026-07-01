<script setup>
import { computed, nextTick, onBeforeUnmount, ref, watch } from "vue";

const props = defineProps({
  open: { type: Boolean, required: true },
  draft: { type: Object, required: true },
  message: { type: String, default: "" },
  isRecordingHotkey: { type: Function, default: null },
  getGestureMnemonic: { type: Function, default: null }
});

const emit = defineEmits(["close", "confirm", "record", "record-hotkey"]);

const canvas = ref(null);
const drawing = ref(false);
const points = ref([]);
const mouseButton = ref("right");
const canvasSize = ref({ width: 0, height: 0 });

const patternLabel = computed(() => props.getGestureMnemonic?.(props.draft) || "尚未录制");

watch(() => props.open, async (open) => {
  if (!open) {
    removeViewportListener();
    return;
  }

  points.value = [];
  await nextTick();
  syncCanvasSize();
  clearCanvas();
  addViewportListener();
});

onBeforeUnmount(() => {
  removeViewportListener();
});

function beginDraw(event) {
  if (event.button !== 1 && event.button !== 2) {
    return;
  }

  event.preventDefault();
  drawing.value = true;
  mouseButton.value = event.button === 1 ? "middle" : "right";
  points.value = [toPoint(event)];
  drawPath();
}

function moveDraw(event) {
  if (!drawing.value) {
    return;
  }

  event.preventDefault();
  const point = toPoint(event);
  const previous = points.value[points.value.length - 1];
  if (previous && distance(previous, point) < 3) {
    return;
  }

  points.value.push(point);
  drawPath();
}

function endDraw(event) {
  if (!drawing.value) {
    return;
  }

  event.preventDefault();
  drawing.value = false;
  emit("record", {
    button: mouseButton.value,
    points: points.value
  });
}

function toPoint(event) {
  const rect = canvas.value.getBoundingClientRect();
  return {
    x: event.clientX - rect.left,
    y: event.clientY - rect.top
  };
}

function syncCanvasSize() {
  if (!canvas.value) {
    return;
  }

  const width = Math.max(1, window.innerWidth);
  const height = Math.max(1, window.innerHeight);
  canvasSize.value = { width, height };
  canvas.value.width = width;
  canvas.value.height = height;
}

function addViewportListener() {
  window.addEventListener("resize", syncCanvasSize);
}

function removeViewportListener() {
  window.removeEventListener("resize", syncCanvasSize);
}

function drawPath() {
  const context = getContext();
  if (!context) {
    return;
  }

  clearCanvas();
  if (points.value.length < 2) {
    return;
  }

  context.lineWidth = 3;
  context.lineCap = "round";
  context.lineJoin = "round";
  context.strokeStyle = "rgba(70, 145, 210, 0.82)";
  context.beginPath();
  context.moveTo(points.value[0].x, points.value[0].y);
  for (const point of points.value.slice(1)) {
    context.lineTo(point.x, point.y);
  }
  context.stroke();
}

function clearCanvas() {
  const context = getContext();
  if (!context) {
    return;
  }

  context.clearRect(0, 0, canvas.value.width, canvas.value.height);
}

function getContext() {
  if (!canvas.value) {
    return null;
  }

  return canvas.value.getContext("2d");
}

function distance(a, b) {
  const dx = a.x - b.x;
  const dy = a.y - b.y;
  return Math.sqrt(dx * dx + dy * dy);
}
</script>

<template>
  <div v-if="open" class="modal-backdrop modal-backdrop--gesture" @click.self="$emit('close')">
    <section class="gesture-dialog" role="dialog" aria-modal="true" aria-labelledby="gesture-dialog-title">
      <canvas
        ref="canvas"
        class="gesture-recorder__canvas gesture-recorder__canvas--fullscreen"
        :width="canvasSize.width || 1"
        :height="canvasSize.height || 1"
        @contextmenu.prevent
        @mousedown="beginDraw"
        @mousemove="moveDraw"
        @mouseup="endDraw"
        @mouseleave="endDraw"
      />

      <div class="gesture-dialog__chrome">
        <div class="modal-panel gesture-dialog__panel">
          <div class="modal-panel__head">
            <div>
              <h3 id="gesture-dialog-title">手势</h3>
              <p>在整个屏幕上拖动鼠标绘制，右键或中键都可以。</p>
            </div>
            <button type="button" class="ghost-button" @click="$emit('close')">关闭</button>
          </div>

          <div class="gesture-dialog__grid">
            <label>
              <span>名称</span>
              <input v-model.trim="draft.actionName" class="scope-input" placeholder="例如：关闭标签">
            </label>

            <label>
              <span>命令</span>
              <button
                type="button"
                class="scope-input hotkey-record-button"
                :class="{ 'is-recording': isRecordingHotkey?.(draft) }"
                @click="$emit('record-hotkey', draft)"
              >
                {{ isRecordingHotkey?.(draft) ? "录制中..." : (draft.keysText || "点击录制") }}
              </button>
            </label>
          </div>

          <div class="gesture-dialog__result">
            <span>识别结果</span>
            <strong>{{ patternLabel }}</strong>
          </div>

          <p v-if="message" class="gesture-dialog__message">{{ message }}</p>

          <div class="modal-panel__actions">
            <button type="button" class="ghost-button" @click="$emit('close')">取消</button>
            <button type="button" class="primary-button" @click="$emit('confirm')">确认</button>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>
