<script setup>
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api, problemMessage, saveSession } from '../api'

const emit = defineEmits(['authenticated'])
const route = useRoute()
const router = useRouter()
const username = ref('')
const password = ref('')
const loading = ref(false)
const error = ref('')

async function submit() {
  error.value = ''
  loading.value = true
  try {
    const { data } = await api.post('/auth/login', {
      username: username.value,
      password: password.value
    })
    saveSession(data)
    emit('authenticated')
    const destination = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    await router.replace(destination)
  } catch (requestError) {
    error.value = problemMessage(requestError, 'Check your username and password.')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="login-page">
    <div class="login-ambient login-ambient--one"></div>
    <div class="login-ambient login-ambient--two"></div>

    <div class="login-story">
      <div class="eyebrow"><span></span> Open-source control plane</div>
      <h1>PostgreSQL infrastructure,<br><em>without the noise.</em></h1>
      <p>Turn existing PostgreSQL functions and procedures into typed REST operations, inspect their results and keep every invocation behind a verified session.</p>
      <div class="signal-grid">
        <div><strong>Typed REST</strong><span>stored routines</span></div>
        <div><strong>Overload-safe</strong><span>stable signatures</span></div>
        <div><strong>Audited</strong><span>bounded execution</span></div>
      </div>
    </div>

    <form class="login-card" @submit.prevent="submit">
      <div class="login-card__mark">P</div>
      <p class="eyebrow">Secure workspace</p>
      <h2>Welcome back</h2>
      <p class="muted">Sign in with your control-plane account.</p>

      <label class="field">
        <span>Username</span>
        <input v-model.trim="username" required autocomplete="username" placeholder="operator" autofocus>
      </label>
      <label class="field">
        <span>Password</span>
        <input v-model="password" required type="password" autocomplete="current-password" placeholder="••••••••••••">
      </label>

      <p v-if="error" class="form-error" role="alert">{{ error }}</p>
      <button class="button button--primary button--wide" :disabled="loading" type="submit">
        {{ loading ? 'Verifying…' : 'Enter control plane' }}
        <span>→</span>
      </button>
      <small class="login-card__hint">Access tokens are kept only for this browser session.</small>
    </form>
  </section>
</template>
