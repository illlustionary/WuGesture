<script setup>
import IconActionButton from '../IconActionButton.vue'

const props = defineProps({
  item: { type: Object, required: true },
  active: { type: Boolean, default: false },
  label: { type: String, default: '' },
  icon: { type: String, default: '' },
  fallbackGlyph: { type: String, default: '' },
  deleteLabel: { type: String, required: true },
  iconMode: { type: String, default: 'category' }
})

const emit = defineEmits(['select', 'rename', 'delete'])

function displayLabel() {
  return props.label || props.item.displayName || props.item.name
}
</script>

<template>
  <div
    class="scope-item"
    :class="{ active }"
    role="button"
    tabindex="0"
    @click="emit('select', item)"
    @dblclick="emit('rename', item)"
    @keydown.enter.prevent="emit('select', item)"
    @keydown.space.prevent="emit('select', item)"
  >
    <span
      class="scope-item__accent"
      aria-hidden="true"
    />
    <span class="scope-item__main">
      <img
        v-if="icon"
        class="app-icon app-icon--small scope-item__icon"
        :src="icon"
        alt=""
      />
      <span
        v-else
        class="scope-item__icon"
        :class="{
          'scope-item__icon--fallback': iconMode === 'app'
        }"
        aria-hidden="true"
      >
        {{ fallbackGlyph }}
      </span>
      <span class="scope-item__name">
        {{ displayLabel() }}
      </span>
    </span>
    <span class="scope-item__meta">
      <IconActionButton
        icon="delete"
        :label="deleteLabel"
        class="scope-item__delete"
        @click.stop="emit('delete', item)"
      />
    </span>
  </div>
</template>

<style scoped lang="scss">
.scope-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
  min-height: 58px;
  padding: 10px 12px 10px 14px;
  border: 0;
  border-radius: 18px;
  text-align: left;
  background: rgba(248, 251, 255, 0.96);
  border: 1px solid rgba(18, 30, 42, 0.08);
  box-shadow: 0 10px 24px rgba(18, 30, 42, 0.04);
  cursor: pointer;
  user-select: none;
  position: relative;
  overflow: hidden;
  transition:
    transform 120ms ease,
    background-color 120ms ease,
    border-color 120ms ease,
    box-shadow 120ms ease;

  &:hover,
  &:focus-visible {
    background: rgba(242, 247, 255, 1);
    border-color: rgba(0, 122, 255, 0.16);
    box-shadow: 0 14px 28px rgba(18, 30, 42, 0.06);
    transform: translateY(-1px);
  }

  &.active {
    background: linear-gradient(
      180deg,
      rgba(231, 241, 255, 1),
      rgba(241, 247, 255, 1)
    );
    border-color: rgba(0, 122, 255, 0.2);
    box-shadow: 0 14px 28px rgba(0, 122, 255, 0.08);
  }

  &.active .scope-item__accent {
    opacity: 1;
  }

  &__accent {
    position: absolute;
    inset: 0 auto 0 0;
    width: 4px;
    border-radius: 999px;
    background: linear-gradient(180deg, var(--accent), var(--accent-strong));
    opacity: 0;
  }

  &__main {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    min-width: 0;
    flex: 1 1 auto;
  }

  &__icon {
    display: grid;
    place-items: center;
    width: 28px;
    height: 28px;
    flex: 0 0 auto;
    border-radius: 10px;
    background: rgba(0, 122, 255, 0.1);
    color: var(--accent-strong);
    font-size: 15px;
    line-height: 1;
  }

  &__icon--fallback {
    font-weight: 700;
  }

  &__name {
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-weight: 600;
  }

  &__meta {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    flex: 0 0 auto;
  }

  &__delete {
    opacity: 0;
    transform: scale(0.92);
    transition:
      opacity 120ms ease,
      transform 120ms ease;
  }

  &:hover &__delete,
  &:focus-within &__delete {
    opacity: 1;
    transform: scale(1);
  }
}

@media (max-width: 720px) {
  .scope-item {
    align-items: flex-start;
    flex-wrap: wrap;
  }

  .scope-item__meta {
    margin-left: auto;
  }
}
</style>
