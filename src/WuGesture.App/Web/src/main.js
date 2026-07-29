import { createApp } from 'vue'
import Toast from 'vue-toastification'
import App from './App.vue'
import router from './router'
import 'vue-toastification/dist/index.css'
import 'virtual:uno.css'
import './styles.scss'

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
