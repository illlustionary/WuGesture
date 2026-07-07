import { createApp } from 'vue'
import { createRouter, createWebHashHistory } from 'vue-router'
import Toast from 'vue-toastification'
import App from './App.vue'
import GlobalRulesPage from './pages/global/index.vue'
import CategoryRulesPage from './pages/category/index.vue'
import AppRulesPage from './pages/app/index.vue'
import EdgeActionsPage from './pages/edge/index.vue'
import SettingsPage from './pages/settings/index.vue'
import 'vue-toastification/dist/index.css'
import 'virtual:uno.css'
import './styles.scss'

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    { path: '/', redirect: '/global' },
    { path: '/global', component: GlobalRulesPage },
    { path: '/category', component: CategoryRulesPage },
    { path: '/app', component: AppRulesPage },
    { path: '/edge', component: EdgeActionsPage },
    { path: '/settings', component: SettingsPage }
  ]
})

createApp(App)
  .use(router)
  .use(Toast, {
    position: 'top-right',
    timeout: 2400,
    closeOnClick: true,
    pauseOnFocusLoss: false,
    pauseOnHover: true,
    draggable: true,
    hideProgressBar: true
  })
  .mount('#app')
