<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api, clearSession, getSession, hasSession } from './api'

const router = useRouter()
const route = useRoute()
const authenticated = ref(hasSession())
const session = computed(() => getSession())

async function signOut() {
  try {
    await api.post('/auth/logout')
  } finally {
    clearSession()
    authenticated.value = false
    router.push({ name: 'login' })
  }
}

function handleSessionChange() {
  authenticated.value = hasSession()
  if (!authenticated.value && route.name !== 'login') {
    router.push({ name: 'login' })
  }
}

onMounted(() => window.addEventListener('auth-expired', handleSessionChange))
onBeforeUnmount(() => window.removeEventListener('auth-expired', handleSessionChange))
</script>

<template>
  <div class="app-shell" :class="{ 'app-shell--guest': !authenticated }">
    <aside v-if="authenticated" class="sidebar">
      <RouterLink class="brand" to="/" aria-label="Postgres Control Plane home">
        <span class="brand__mark">P</span>
        <span>
          <strong>Postgres</strong>
          <small>Control Plane</small>
        </span>
      </RouterLink>

      <nav class="navigation" aria-label="Main navigation">
        <RouterLink to="/" class="navigation__link">
          <span class="navigation__icon">ƒ</span>
          Routine gateway
        </RouterLink>
        <RouterLink to="/deployments" class="navigation__link">
          <span class="navigation__icon">⌘</span>
          Deployments
        </RouterLink>
      </nav>

      <div class="sidebar__footer">
        <div class="identity">
          <span class="identity__avatar">{{ session?.user?.username?.slice(0, 1).toUpperCase() }}</span>
          <span>
            <strong>{{ session?.user?.username }}</strong>
            <small>{{ session?.user?.roles?.join(' · ') }}</small>
          </span>
        </div>
        <button class="icon-button" type="button" title="Sign out" @click="signOut">↗</button>
      </div>
    </aside>

    <main class="workspace">
      <RouterView @authenticated="handleSessionChange" />
    </main>
  </div>
</template>
