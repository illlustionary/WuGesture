import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import UnoCSS from 'unocss/vite'
import svgLoader from 'vite-svg-loader'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [vue(), UnoCSS(), svgLoader()],
  base: './',
  build: {
    emptyOutDir: true
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      '@resources': fileURLToPath(new URL('../Resources', import.meta.url))
    }
  }
})
