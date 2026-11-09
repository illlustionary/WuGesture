import edgeRoutes from './edgeRoutes'
import scopeRoutes from './scopeRoutes'
import settingsRoutes from './settingsRoutes'

const globalRulesPage = () => import('../../pages/global/index.vue')
const exclusionsPage = () => import('../../pages/exclusions/index.vue')
const aboutPage = () => import('../../pages/about/index.vue')

// 设计令牌预览页只在开发环境注册；生产构建会把 import.meta.env.DEV 内联为 false，
// 连同动态 import 一起被 Rollup 移除，因此不会进入发布产物。
const devRoutes = import.meta.env.DEV
  ? [
      {
        path: '/dev/md3',
        name: 'dev-md3',
        component: () => import('../../pages/dev/md3/index.vue')
      }
    ]
  : []

export default [
  { path: '/', redirect: '/global' },
  { path: '/global', name: 'global', component: globalRulesPage },
  ...scopeRoutes,
  edgeRoutes,
  { path: '/exclusions', name: 'exclusions', component: exclusionsPage },
  { path: '/about', name: 'about', component: aboutPage },
  settingsRoutes,
  ...devRoutes
]
