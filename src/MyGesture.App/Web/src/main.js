import { createApp } from "vue";
import { createRouter, createWebHashHistory } from "vue-router";
import Toast from "vue-toastification";
import App from "./App.vue";
import GlobalRulesPage from "./pages/GlobalRulesPage.vue";
import CategoryRulesPage from "./pages/CategoryRulesPage.vue";
import AppRulesPage from "./pages/AppRulesPage.vue";
import EdgeActionsPage from "./pages/EdgeActionsPage.vue";
import SettingsPage from "./pages/SettingsPage.vue";
import "vue-toastification/dist/index.css";
import "./styles.scss";

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    { path: "/", redirect: "/global" },
    { path: "/global", component: GlobalRulesPage },
    { path: "/category", component: CategoryRulesPage },
    { path: "/app", component: AppRulesPage },
    { path: "/edge", component: EdgeActionsPage },
    { path: "/settings", component: SettingsPage }
  ]
});

createApp(App)
  .use(router)
  .use(Toast, {
    position: "top-right",
    timeout: 2400,
    closeOnClick: true,
    pauseOnFocusLoss: false,
    pauseOnHover: true,
    draggable: true,
    hideProgressBar: true
  })
  .mount("#app");
