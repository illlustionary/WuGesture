const nestedRouteView = () => import('../../components/layout/NestedRouteView.vue')
const settingsPage = () => import('../../pages/settings/index.vue')

export default {
  path: '/settings',
  component: nestedRouteView,
  children: [
    { path: '', redirect: 'mouse-trail' },
    {
      path: 'mouse-trail',
      name: 'settings-mouse-trail',
      component: settingsPage,
      props: { section: 'mouse-trail' }
    },
    {
      path: 'gesture-hint',
      name: 'settings-gesture-hint',
      component: settingsPage,
      props: { section: 'gesture-hint' }
    },
    {
      path: 'level-osd',
      name: 'settings-level-osd',
      component: settingsPage,
      props: { section: 'level-osd' }
    },
    {
      path: 'sensitivity',
      name: 'settings-sensitivity',
      component: settingsPage,
      props: { section: 'sensitivity' }
    },
    {
      path: 'app-behavior',
      name: 'settings-app-behavior',
      component: settingsPage,
      props: { section: 'app-behavior' }
    },
    {
      path: 'webdav',
      name: 'settings-webdav',
      component: settingsPage,
      props: { section: 'webdav' }
    }
  ]
}
