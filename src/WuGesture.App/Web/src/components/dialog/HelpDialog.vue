<script setup>
import BaseDialog from './BaseDialog.vue'
import ScopePriorityNotice from '@/components/scope/ScopePriorityNotice.vue'

defineProps({
  open: { type: Boolean, default: false }
})

const emit = defineEmits(['close', 'replay'])
</script>

<template>
  <BaseDialog
    :open="open"
    title="帮助"
    description="手势、规则范围和边缘操作的快速说明。"
    title-id="help-dialog-title"
    :show-close="true"
    :show-actions="false"
    panel-class="help-dialog"
    @close="emit('close')"
  >
    <div class="help-dialog__body">
      <section class="help-dialog__section">
        <h4>第一次使用</h4>
        <ol>
          <li>按住右键或中键，在任意窗口上划一道方向，松开即执行。</li>
          <li>在左侧「全局」页点新增，可以录制手势并选择要执行的命令。</li>
          <li>想让手势只对某个程序生效，先在「程序」页添加程序，再为它新建规则。</li>
        </ol>
      </section>

      <section class="help-dialog__section">
        <h4>规则生效顺序</h4>
        <p>同一个手势在多个范围都有规则时，按下面的顺序取第一条命中的规则：</p>
        <ScopePriorityNotice />
      </section>

      <section class="help-dialog__section">
        <h4>边缘操作</h4>
        <ul>
          <li><strong>触发角</strong>：鼠标移入屏幕四角触发，离开再进入可再次触发。</li>
          <li><strong>摩擦边</strong>：贴着屏幕边缘来回摩擦指定次数后触发。</li>
          <li><strong>边缘滚动</strong>：光标停在屏幕边缘滚动滚轮时触发。</li>
        </ul>
      </section>

      <section class="help-dialog__section">
        <h4>常见问题</h4>
        <details>
          <summary>全屏里看不到轨迹，但手势仍然生效</summary>
          <p>独占全屏会绕过桌面合成，任何分层窗口都无法叠加；把游戏改成无边框全屏即可。</p>
        </details>
        <details>
          <summary>某个程序里手势没反应</summary>
          <p>
            先确认它没有出现在「排除项」里，也没有开启「全屏时禁用手势」。像任务管理器这类以管理员权限运行的程序，需要
            WuGesture 也以管理员身份运行，否则系统会拦截跨权限级别的输入；可以在「设置 &gt;
            应用行为」里打开「以管理员身份打开」，重启后生效。
          </p>
        </details>
        <details>
          <summary>怎么备份或换电脑</summary>
          <p>「设置 &gt; 配置」可以导出 / 导入 JSON，窗口位置和大小不会一起导出。</p>
        </details>
      </section>
    </div>

    <template #actions>
      <div class="help-dialog__actions">
        <button
          type="button"
          class="ghost-button"
          @click="emit('replay')"
        >
          重新观看引导
        </button>
        <a
          class="help-dialog__link"
          href="https://github.com/illlustionary/WuGesture#readme"
          target="_blank"
          rel="noreferrer"
          >完整文档</a
        >
        <button
          type="button"
          class="primary-button"
          @click="emit('close')"
        >
          知道了
        </button>
      </div>
    </template>
  </BaseDialog>
</template>

<style scoped lang="scss">
:deep(.help-dialog) {
  width: min(560px, calc(100vw - 36px));
}

.help-dialog__body {
  display: grid;
  gap: 18px;
}

.help-dialog__section {
  display: grid;
  gap: 8px;

  &:not(:last-child) {
    padding-bottom: 16px;
    border-bottom: 1px solid var(--border);
  }

  h4 {
    font-size: 14px;
    font-weight: 600;
  }

  p,
  li {
    color: var(--muted);
    font-size: 13px;
    line-height: 1.6;
  }

  ol,
  ul {
    display: grid;
    gap: 6px;
    margin: 0;
    padding-left: 18px;
  }

  strong {
    color: var(--text);
    font-weight: 600;
  }
}

.help-dialog__section details {
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  background: var(--interactive-bg);

  summary {
    padding: 8px 10px;
    color: var(--text);
    font-size: 13px;
    cursor: pointer;
  }

  p {
    padding: 0 10px 8px;
  }
}

.help-dialog__actions {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.help-dialog__link {
  margin-right: auto;
  color: var(--muted);
  font-size: 13px;

  &:hover {
    color: var(--text);
  }
}
</style>
