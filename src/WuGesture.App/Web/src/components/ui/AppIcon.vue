<script setup lang="ts">
import { computed, type Component } from 'vue'
import { appIcons } from '@/icons/appIcons'

const props = withDefaults(
  defineProps<{
    name: string
    size?: number | string
  }>(),
  {
    size: 20
  }
)

const icons = appIcons as unknown as Record<string, Component>
const icon = computed(() => icons[props.name] ?? icons['circle-dashed'])
</script>

<template>
  <component
    :is="icon"
    :width="size"
    :height="size"
    aria-hidden="true"
  />
</template>
