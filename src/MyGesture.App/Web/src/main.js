import { createApp } from "vue";
import { createRouter, createWebHashHistory } from "vue-router";
import App from "./App.vue";
import GlobalRulesPage from "./pages/GlobalRulesPage.vue";
import CategoryRulesPage from "./pages/CategoryRulesPage.vue";
import AppRulesPage from "./pages/AppRulesPage.vue";
import "./styles.css";

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    { path: "/", redirect: "/global" },
    { path: "/global", component: GlobalRulesPage },
    { path: "/category", component: CategoryRulesPage },
    { path: "/app", component: AppRulesPage }
  ]
});

createApp(App).use(router).mount("#app");
