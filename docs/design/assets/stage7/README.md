# 阶段 7 走查产物

`docs/design/md3-redesign-plan.md` 阶段 7（图标与打磨）留下的静态对比材料，均为自包含 HTML，双击即可在浏览器打开，不依赖本地服务或外网资源。

| 文件 | 内容 |
|---|---|
| `icon-compare.html` | 19 个重绘图标的「旧 1024 实心 / 新 24dp 线性」对比，按 14 / 16 / 18 / 20 / 24 px 和浅色 / 深色背景排列，用于检查小尺寸清晰度与描边一致性。 |
| `palette-gallery.html` | 8 套预设（蓝 / 青 / 绿 / 紫 / 粉 / 橙 / 红 / 灰）× 浅色 / 深色，渲染按钮、chip、输入框、分隔线、列表行和错误文字等真实组件取样，用于配色与对比度走查。 |

## 复现方式

- 图标对比：旧图标取自重绘前的提交，新图标取自 `src/WuGesture.App/Web/src/assets/`，由一次性脚本拼成内联 SVG 表格。
- 配色走查：用 `src/WuGesture.App/Web/src/theme/scheme.ts` + `palettes.ts` 生成角色色值，注入内联 CSS 变量后渲染组件取样。

两张页面都是归档快照，配色引擎或图标调整后不会自动更新；需要重新走查时应重新生成，而不是直接改这里的 HTML。

对比度结论以 `src/WuGesture.App/Web/src/theme/contrast.test.ts` 为准，HTML 只用于人眼确认观感。
