const nestedRouteView = () => import('../../components/layout/NestedRouteView.vue')
const edgeActionsPage = () => import('../../pages/edge/index.vue')

export default {
  path: '/edge',
  component: nestedRouteView,
  children: [
    { path: '', redirect: 'corner' },
    {
      path: 'corner',
      name: 'edge-corner',
      component: edgeActionsPage,
      props: { section: 'corner' }
    },
    {
      path: 'friction',
      name: 'edge-friction',
      component: edgeActionsPage,
      props: { section: 'friction' }
    },
    {
      path: 'wheel',
      name: 'edge-wheel',
      component: edgeActionsPage,
      props: { section: 'wheel' }
    }
  ]
}
