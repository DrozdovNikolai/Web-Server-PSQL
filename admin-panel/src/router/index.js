import { createRouter, createWebHistory } from 'vue-router'
import ContainerList from '../views/ContainerList.vue'
import NewContainer from '../views/NewContainer.vue'

const routes = [
  {
    path: '/',
    name: 'ContainerList',
    component: ContainerList
  },
  {
    path: '/new',
    name: 'NewContainer',
    component: NewContainer
  }
]

const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes
})

export default router 