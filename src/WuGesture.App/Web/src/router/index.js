import { createRouter, createWebHashHistory } from 'vue-router'
import routes from './routes/index'

export default createRouter({
  history: createWebHashHistory(),
  routes
})
