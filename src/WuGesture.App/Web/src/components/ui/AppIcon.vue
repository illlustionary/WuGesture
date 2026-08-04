<script setup>
import { computed } from 'vue'
import { appIcons } from '@/icons/appIcons'

const props = defineProps({
  name: {
    type: String,
    required: true
  }
})

const icon = computed(() => appIcons[props.name] ?? appIcons['circle-dashed'])
</script>

<template>
  <component :is="icon" />
</template>
