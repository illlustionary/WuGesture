<script setup>
import { actionKey } from '@/pages/edge/composables/useEdgeActionDraft'
import EdgeActionCard from './EdgeActionCard.vue'

defineProps({
  group: { type: Object, required: true },
  actions: { type: Array, required: true },
  triggerLabels: { type: Object, required: true },
  locationLabel: { type: Function, required: true },
  wheelLabel: { type: Function, required: true },
  actionSummary: { type: Function, required: true },
  getEdgeActionLabel: { type: Function, required: true }
})

const emit = defineEmits(['open'])
</script>

<template>
  <section class="edge-section section-card">
    <div class="edge-section__head">
      <h3 class="section-title">{{ group.title }}</h3>
    </div>

    <div class="edge-grid">
      <EdgeActionCard
        v-for="action in actions"
        :key="actionKey(action)"
        :action="action"
        :title="locationLabel(action)"
        :subtitle="wheelLabel(action) || triggerLabels[action.triggerType]"
        :name="getEdgeActionLabel(action)"
        :summary="actionSummary(action)"
        @open="emit('open', $event)"
      />
    </div>
  </section>
</template>

<style scoped lang="scss">
.edge-section {
  display: flex;
  flex-direction: column;
  gap: 14px;

  &__head {
    display: flex;
    justify-content: space-between;
    gap: 12px;
  }
}

.edge-grid {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px 5px;
  align-content: start;
}

@media (max-width: 920px) {
  .edge-grid {
    grid-template-columns: 1fr;
  }
}
</style>
