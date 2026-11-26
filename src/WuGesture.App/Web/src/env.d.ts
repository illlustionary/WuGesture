/// <reference types="vite/client" />

declare module '*.svg?component' {
  import type { DefineComponent } from 'vue'

  const component: DefineComponent<{}, {}, any>
  export default component
}

declare module '*.svg' {
  const src: string
  export default src
}

declare module '*.svg?url' {
  const src: string
  export default src
}

/** 由 `vite.config.js` 的 `rawSourcePlugin` 提供，按源码文本返回仓库文件（测试使用）。 */
declare module 'virtual:raw-source?*' {
  const source: string
  export default source
}
