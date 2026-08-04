<script setup>
import { computed, ref } from 'vue'
import ApplicationListItem from '@/components/scope/ApplicationListItem.vue'
import IconActionButton from '@/components/ui/IconActionButton.vue'

const props = defineProps({
  draft: { type: Object, required: true },
  showProgram: { type: Boolean, default: true },
  showPicker: { type: Boolean, default: true },
  showArguments: { type: Boolean, default: true }
})

const emit = defineEmits(['select-program', 'open-application-folder'])
const argumentDraft = ref('')
const removingArguments = ref(new Set())

const selectedProgram = computed(() => {
  const path = String(props.draft.programPath ?? '').trim()
  const fallbackName =
    path
      .split(/[\\/]/)
      .at(-1)
      ?.replace(/\.exe$/i, '') || '未选择程序'
  return {
    name: fallbackName,
    displayName: String(props.draft.programName ?? '').trim() || fallbackName,
    path,
    icon: String(props.draft.programIcon ?? '').trim(),
    pathMissing: Boolean(props.draft.programMissing)
  }
})

function addArgument() {
  const argument = argumentDraft.value.trim()
  if (!argument) {
    return
  }

  props.draft.programArguments.push(argument)
  argumentDraft.value = ''
}

function removeArgument(index) {
  if (removingArguments.value.size > 0) {
    return
  }

  removingArguments.value.add(index)
  window.setTimeout(() => {
    props.draft.programArguments.splice(index, 1)
    removingArguments.value = new Set()
  }, 160)
}
</script>

<template>
  <div class="gesture-rule-program-fields">
    <section
      v-if="showProgram"
      class="gesture-rule-program-fields__section"
    >
      <span class="gesture-rule-program-fields__title">程序</span>
      <div class="gesture-rule-program-fields__row gesture-rule-program-fields__program-row">
        <div class="gesture-rule-program-fields__program">
          <ApplicationListItem
            v-if="selectedProgram.path"
            :app="selectedProgram"
            :removable="false"
            @open-path="emit('open-application-folder', selectedProgram.path)"
          />
          <div
            v-else
            class="gesture-rule-program-fields__empty"
          >
            尚未添加程序
          </div>
        </div>
      </div>
    </section>

    <section
      v-if="showPicker"
      class="gesture-rule-program-fields__section"
    >
      <span class="gesture-rule-program-fields__title">添加程序</span>
      <div class="gesture-rule-program-fields__row gesture-rule-program-fields__picker-row">
        <span class="gesture-rule-program-fields__picker-hint">选择要运行的程序</span>
        <IconActionButton
          icon="add"
          label="添加程序"
          class="secondary-button gesture-rule-program-fields__add-button"
          @click="emit('select-program')"
        />
      </div>
    </section>

    <section
      v-if="showArguments"
      class="gesture-rule-program-fields__section"
    >
      <span class="gesture-rule-program-fields__title">执行参数</span>
      <div class="gesture-rule-program-fields__row gesture-rule-program-fields__argument-row">
        <div class="gesture-rule-program-fields__argument-control">
          <input
            v-model="argumentDraft"
            class="scope-input"
            placeholder="运行程序时的额外参数，如无必要，留空即可"
            @keydown.enter.prevent="addArgument"
          />
          <IconActionButton
            icon="add"
            label="添加运行参数"
            class="secondary-button gesture-rule-program-fields__add-button"
            @click="addArgument"
          />
        </div>
      </div>
    </section>

    <section
      v-if="showArguments"
      class="gesture-rule-program-fields__section"
    >
      <span class="gesture-rule-program-fields__title">参数列表</span>
      <div class="gesture-rule-program-fields__argument-list">
        <div
          v-if="draft.programArguments.length === 0"
          class="gesture-rule-program-fields__argument-empty"
        >
          <span>在上方添加运行参数，参数将按顺序传入程序</span>
        </div>
        <div
          v-else
          class="gesture-rule-program-fields__arguments"
        >
          <span
            v-for="(argument, index) in draft.programArguments"
            :key="`${argument}-${index}`"
            class="gesture-rule-program-fields__argument"
            :class="{ 'is-removing': removingArguments.has(index) }"
            tabindex="0"
          >
            <span class="gesture-rule-program-fields__argument-text">{{ argument }}</span>
            <IconActionButton
              icon="close"
              label="删除运行参数"
              :show-tooltip="false"
              class="gesture-rule-program-fields__argument-remove"
              @click="removeArgument(index)"
            />
          </span>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped lang="scss">
.gesture-rule-program-fields,
.gesture-rule-program-fields__section {
  display: grid;
  gap: 8px;
}

.gesture-rule-program-fields__title {
  color: var(--muted);
  font-size: 13px;
  font-weight: 600;
}

.gesture-rule-program-fields__row {
  display: grid;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--interactive-bg);
}

.gesture-rule-program-fields__program-row {
  overflow: hidden;
}

.gesture-rule-program-fields__picker-row {
  grid-template-columns: 1fr auto;
  min-height: 52px;
  padding: 8px 10px 8px 14px;
  align-items: center;
}

.gesture-rule-program-fields__picker-hint {
  color: var(--text-subtle);
  font-size: 13px;
}

.gesture-rule-program-fields__program {
  overflow: hidden;
}

.gesture-rule-program-fields__empty {
  display: flex;
  min-height: 64px;
  padding: 12px 14px;
  align-items: center;
  color: var(--text-subtle);
}

.gesture-rule-program-fields__add-button {
  align-self: center;
  margin: 0 10px;
}

.gesture-rule-program-fields__argument-row {
  gap: 12px;
  min-height: 52px;
  padding: 8px 10px 8px 14px;
  align-items: center;
}

.gesture-rule-program-fields__argument-control {
  flex: 1;
  display: flex;
  gap: 8px;
  align-items: center;
}

.gesture-rule-program-fields__argument-control .scope-input {
  border-color: transparent;
  background: transparent;
  display: block;
  flex: 1;
}

.gesture-rule-program-fields__argument-empty {
  display: grid;
  gap: 4px;
  min-height: 40px;
  padding: 2px 0;
  color: var(--text-subtle);
  font-size: 12px;
}

.gesture-rule-program-fields__argument-list {
  display: grid;
  gap: 8px;
  min-height: 52px;
  padding: 12px 14px;
  align-content: center;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--interactive-bg);
}

.gesture-rule-program-fields__arguments {
  display: flex;
  min-height: 34px;
  flex-wrap: wrap;
  gap: 8px;
}

.gesture-rule-program-fields__argument {
  display: inline-flex;
  max-width: 100%;
  min-height: 32px;
  padding: 3px 4px 3px 10px;
  align-items: center;
  gap: 4px;
  border: 1px solid var(--border-subtle);
  border-radius: 8px;
  background: var(--interactive-bg);
  transition:
    opacity 160ms ease,
    transform 160ms ease,
    border-color 120ms ease,
    background-color 120ms ease;

  &:hover,
  &:focus-within {
    border-color: var(--accent-strong);
    background: var(--interactive-hover-bg);
    transform: scale(1.03);
  }

  &.is-removing {
    opacity: 0;
    transform: scale(0.82);
  }
}

.gesture-rule-program-fields__argument-text {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

:deep(.gesture-rule-program-fields__argument-remove) {
  min-width: 24px;
  min-height: 24px;
  color: var(--text-subtle);

  &:hover {
    color: var(--danger);
  }
}

:deep(.gesture-rule-program-fields__program .app-list__item) {
  border-radius: 0;
}

@media (max-width: 560px) {
  .gesture-rule-program-fields__argument-row {
    grid-template-columns: 1fr;
    gap: 6px;
  }
}
</style>
