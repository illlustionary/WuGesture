<script setup>
import AppShell from "../components/AppShell.vue";
import GestureRuleList from "../components/GestureRuleList.vue";
import { useGestureEditor } from "../composables/gestureEditor";

const editor = useGestureEditor();
</script>

<template>
  <AppShell
    title="全局规则"
    description="全局规则直接铺开编辑，没有左侧列表。"
    :config-path="editor.configPath"
    :layout-class="editor.workspaceClass"
  >
    <template #right>
      <section class="rules-panel">
        <div class="rules-panel__head">
          <div>
            <h3>全局规则</h3>
            <p>名称、手势、命令三列编辑，删除放在每行右侧。</p>
          </div>
          <button type="button" class="primary-button" @click="editor.addRule">新增规则</button>
        </div>

        <div v-if="editor.visibleRules.length === 0" class="empty-state empty-state--large">
          当前没有全局规则。
        </div>

        <GestureRuleList :rules="editor.visibleRules" @remove="editor.removeRule" />
      </section>
    </template>

    <template #left />
  </AppShell>
</template>
