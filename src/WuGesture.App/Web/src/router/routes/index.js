import edgeRoutes from './edgeRoutes'
import scopeRoutes from './scopeRoutes'
import settingsRoutes from './settingsRoutes'

const globalRulesPage = () => import('../../pages/global/index.vue')
const exclusionsPage = () => import('../../pages/exclusions/index.vue')
const aboutPage = () => import('../../pages/about/index.vue')

export default [
  { path: '/', redirect: '/global' },
  { path: '/global', name: 'global', component: globalRulesPage },
  ...scopeRoutes,
  edgeRoutes,
  { path: '/exclusions', name: 'exclusions', component: exclusionsPage },
  { path: '/about', name: 'about', component: aboutPage },
  settingsRoutes
]
