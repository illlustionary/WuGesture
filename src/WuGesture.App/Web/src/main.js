import { createApp } from 'vue'
import Toast from 'vue-toastification'
import App from './App.vue'
import router from './router'
import { applyCachedScheme } from './theme/applyScheme'
import 'vue-toastification/dist/index.css'
import 'virtual:uno.css'
import './styles.scss'

// 首屏加速：先用上次生成的色板覆盖静态基线，宿主 rules 消息到达后再校正。
applyCachedScheme()

createApp(App)
  .use(router)
  .use(Toast, {
    position: 'top-center',
    timeout: 2400,
    closeOnClick: true,
    pauseOnFocusLoss: false,
    pauseOnHover: true,
    draggable: true,
    hideProgressBar: true
  })
  .mount('#app')
