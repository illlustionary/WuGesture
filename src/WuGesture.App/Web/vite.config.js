import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import UnoCSS from 'unocss/vite'
import svgLoader from 'vite-svg-loader'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [
    vue(),
    UnoCSS(),
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
