import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import UnoCSS from 'unocss/vite'
import svgLoader from 'vite-svg-loader'
import { readFileSync } from 'node:fs'
import { fileURLToPath, URL } from 'node:url'

// 测试用：按源码文本暴露指定文件（`virtual:raw-source?path=<相对路径>&raw`）。
// `import './x.scss?raw'` 会被 Vite/Vitest 的 CSS 管线接管，Vitest 默认不处理 CSS，拿到的是空字符串；
// 虚拟模块直接读文件字节，保证测试断言的是仓库里的真实令牌源码。
// 末尾的 `&raw` 是必要的：specifier 若以 `.scss` 结尾，Vitest 仍会按 CSS 模块处理并返回空导出。
const RAW_SOURCE_PREFIX = 'virtual:raw-source?'

function rawSourcePlugin() {
  return {
    name: 'wugesture-raw-source',
    resolveId(id) {
      return id.startsWith(RAW_SOURCE_PREFIX) ? `\0${id}` : null
    },
    load(id) {
      if (!id.startsWith(`\0${RAW_SOURCE_PREFIX}`)) {
        return null
      }

      const relativePath = new URLSearchParams(id.slice(`\0${RAW_SOURCE_PREFIX}`.length)).get('path')
      if (!relativePath) {
        throw new Error(`virtual:raw-source 缺少 path 参数：${id}`)
      }

      const absolutePath = fileURLToPath(new URL(relativePath, import.meta.url))
      return `export default ${JSON.stringify(readFileSync(absolutePath, 'utf8'))}`
    }
  }
}

export default defineConfig({
  plugins: [
    vue(),
    UnoCSS(),
    rawSourcePlugin(),
    svgLoader({
      svgoConfig: {
        plugins: [{ name: 'preset-default', params: { overrides: { removeViewBox: false } } }]
      }
    })
  ],
  base: './',
  build: {
    emptyOutDir: true
  },
  test: {
    server: {
      deps: {
        // material-color-utilities 0.4.0 的 2025 spec 文件使用了无扩展名的相对导入，
        // Node ESM 无法直接加载，交给 Vite 的解析器处理后才能被 Vitest 使用。
        inline: ['@material/material-color-utilities']
      }
    }
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      '@resources': fileURLToPath(new URL('../Resources', import.meta.url))
    }
  }
})
