import { createRouter, createWebHistory } from 'vue-router'
import { hasSession } from '../api'
import { resolveAuthNavigation } from '../navigation'
import DashboardView from '../views/DashboardView.vue'
import DatabaseView from '../views/DatabaseView.vue'
import LoginView from '../views/LoginView.vue'
import NewDeploymentView from '../views/NewDeploymentView.vue'

const routes = [
  {
    path: '/',
    name: 'routine-gateway',
    component: DatabaseView,
    meta: { requiresAuth: true }
  },
  {
    path: '/deployments',
    name: 'dashboard',
    component: DashboardView,
    meta: { requiresAuth: true }
  },
  {
    path: '/deployments/new',
    name: 'new-deployment',
    component: NewDeploymentView,
    meta: { requiresAuth: true }
  },
  {
    path: '/database',
    redirect: { name: 'routine-gateway' }
  },
  {
    path: '/login',
    name: 'login',
    component: LoginView
  }
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes
})

router.beforeEach((to) => {
  return resolveAuthNavigation(to, hasSession())
})

export default router
