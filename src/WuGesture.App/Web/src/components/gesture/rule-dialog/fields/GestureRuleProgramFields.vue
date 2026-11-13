<script setup>
import { computed, ref } from 'vue'
import ApplicationListItem from '@/components/scope/ApplicationListItem.vue'
import AppChip from '@/components/ui/AppChip.vue'
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
          <AppChip
            v-for="(argument, index) in draft.programArguments"
            :key="`${argument}-${index}`"
            removable
            class="gesture-rule-program-fields__argument"
            :class="{ 'is-removing': removingArguments.has(index) }"
            tabindex="0"
            @remove="removeArgument(index)"
          >
            {{ argument }}
          </AppChip>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped lang="scss">
.gesture-rule-program-fields,
.gesture-rule-program-fields__section {
  display: grid;
  gap: var(--md-sys-spacing-sm);
}

.gesture-rule-program-fields__title {
  color: var(--md-sys-color-on-surface-variant);
  font-size: var(--md-sys-typescale-label-large-size);
  line-height: var(--md-sys-typescale-label-large-line-height);
  font-weight: var(--md-sys-typescale-label-large-weight);
}

.gesture-rule-program-fields__row {
  display: grid;
  border: 1px solid var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-small);
  background: var(--md-sys-color-surface-container);
}

.gesture-rule-program-fields__program-row {
  overflow: hidden;
}

.gesture-rule-program-fields__picker-row {
  grid-template-columns: 1fr auto;
  min-height: var(--md-sys-size-field-height);
  padding: var(--md-sys-spacing-sm) var(--md-sys-spacing-md) var(--md-sys-spacing-sm) var(--md-sys-spacing-lg);
  align-items: center;
}

.gesture-rule-program-fields__picker-hint {
  color: var(--md-sys-color-outline);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
}

.gesture-rule-program-fields__program {
  overflow: hidden;
}

.gesture-rule-program-fields__empty {
  display: flex;
  min-height: 64px;
  padding: var(--md-sys-spacing-md) var(--md-sys-spacing-lg);
  align-items: center;
  color: var(--md-sys-color-outline);
  font-size: var(--md-sys-typescale-body-medium-size);
  line-height: var(--md-sys-typescale-body-medium-line-height);
}

.gesture-rule-program-fields__add-button {
  align-self: center;
  margin: 0 var(--md-sys-spacing-md);
}

.gesture-rule-program-fields__argument-row {
  gap: var(--md-sys-spacing-md);
  min-height: var(--md-sys-size-field-height);
  padding: var(--md-sys-spacing-sm) var(--md-sys-spacing-md) var(--md-sys-spacing-sm) var(--md-sys-spacing-lg);
  align-items: center;
}

.gesture-rule-program-fields__argument-control {
  flex: 1;
  display: flex;
  gap: var(--md-sys-spacing-sm);
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
  gap: var(--md-sys-spacing-xs);
  min-height: 40px;
  padding: var(--md-sys-spacing-xs) 0;
  color: var(--md-sys-color-outline);
  font-size: var(--md-sys-typescale-body-small-size);
  line-height: var(--md-sys-typescale-body-small-line-height);
}

.gesture-rule-program-fields__argument-list {
  display: grid;
  gap: var(--md-sys-spacing-sm);
  min-height: var(--md-sys-size-field-height);
  padding: var(--md-sys-spacing-md) var(--md-sys-spacing-lg);
  align-content: center;
  border: 1px solid var(--md-sys-color-outline-variant);
  border-radius: var(--md-sys-shape-corner-small);
  background: var(--md-sys-color-surface-container);
}

.gesture-rule-program-fields__arguments {
  display: flex;
  min-height: 34px;
  flex-wrap: wrap;
  gap: var(--md-sys-spacing-sm);
}

.gesture-rule-program-fields__argument {
  max-width: 100%;
  transition:
    opacity var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard),
    transform var(--md-sys-motion-duration-short) var(--md-sys-motion-easing-standard);

  &:hover,
  &:focus-within {
    transform: scale(1.03);
  }

  &.is-removing {
    opacity: 0;
    transform: scale(0.82);
  }
}

:deep(.gesture-rule-program-fields__argument .app-chip__remove) {
  color: var(--md-sys-color-on-surface-variant);

  &:hover {
    color: var(--md-sys-color-error);
  }
}

:deep(.gesture-rule-program-fields__program .app-list__item) {
  border-radius: 0;
}

@media (max-width: 560px) {
  .gesture-rule-program-fields__argument-row {
    grid-template-columns: 1fr;
    gap: var(--md-sys-spacing-sm);
  }
}
</style>
