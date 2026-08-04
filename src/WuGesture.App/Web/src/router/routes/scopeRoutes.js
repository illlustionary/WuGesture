const nestedRouteView = () => import('../../components/layout/NestedRouteView.vue')
const categoryRulesPage = () => import('../../pages/category/index.vue')
const appRulesPage = () => import('../../pages/app/index.vue')

export default [
  {
    path: '/category',
    component: nestedRouteView,
    children: [
      { path: '', name: 'category-overview', component: categoryRulesPage },
      {
        path: ':name',
        name: 'category-scope',
        component: categoryRulesPage,
        props: true
      }
    ]
  },
  {
    path: '/app',
    component: nestedRouteView,
    children: [
      { path: '', name: 'app-overview', component: appRulesPage },
      {
        path: ':name',
        name: 'app-scope',
        component: appRulesPage,
        props: true
      }
    ]
  }
]
