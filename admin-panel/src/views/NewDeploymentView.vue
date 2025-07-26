<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { api, problemMessage } from '../api'

const router = useRouter()
const loading = ref(false)
const error = ref('')
const form = reactive({
  name: '',
  databaseHost: '',
  databasePort: 5432,
  databaseName: '',
  databaseUser: '',
  databasePassword: '',
  applicationAdminUsername: '',
  applicationAdminPassword: ''
})

async function submit() {
  loading.value = true
  error.value = ''
  try {
    await api.post('/deployments', form)
    await router.push({ name: 'dashboard' })
  } catch (requestError) {
    error.value = problemMessage(requestError, 'The deployment could not be created.')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="page page--narrow">
    <header class="page-header">
      <div>
        <RouterLink class="back-link" to="/deployments">← Back to deployments</RouterLink>
        <p class="eyebrow"><span></span> Provisioning workflow</p>
        <h1>Create deployment</h1>
        <p class="muted">The control plane creates namespaced, non-root resources. Secrets are sent once and are not persisted in its database.</p>
      </div>
    </header>

    <form class="panel form-panel" @submit.prevent="submit">
      <div class="form-section">
        <div class="form-section__number">01</div>
        <div class="form-section__body">
          <h2>Workload identity</h2>
          <p>Use a DNS-safe name; it becomes the Kubernetes resource prefix.</p>
          <label class="field"><span>Deployment name</span><input v-model.trim="form.name" required maxlength="63" pattern="[a-z0-9]([-a-z0-9]*[a-z0-9])?" placeholder="analytics-sandbox"></label>
        </div>
      </div>

      <div class="form-section">
        <div class="form-section__number">02</div>
        <div class="form-section__body">
          <h2>PostgreSQL connection</h2>
          <p>Use a least-privileged database account dedicated to this workload.</p>
          <div class="form-grid">
            <label class="field field--wide"><span>Host</span><input v-model.trim="form.databaseHost" required placeholder="postgres.internal"></label>
            <label class="field"><span>Port</span><input v-model.number="form.databasePort" required min="1" max="65535" type="number"></label>
            <label class="field"><span>Database</span><input v-model.trim="form.databaseName" required placeholder="control_demo"></label>
            <label class="field"><span>Database user</span><input v-model.trim="form.databaseUser" required autocomplete="off" placeholder="control_app"></label>
            <label class="field field--wide"><span>Database password</span><input v-model="form.databasePassword" required minlength="8" type="password" autocomplete="new-password" placeholder="At least 8 characters"></label>
          </div>
        </div>
      </div>

      <div class="form-section">
        <div class="form-section__number">03</div>
        <div class="form-section__body">
          <h2>Initial application administrator</h2>
          <p>This account is bootstrapped once inside the isolated workload.</p>
          <div class="form-grid">
            <label class="field"><span>Username</span><input v-model.trim="form.applicationAdminUsername" required maxlength="100" autocomplete="off" placeholder="operator"></label>
            <label class="field"><span>Password</span><input v-model="form.applicationAdminPassword" required minlength="12" type="password" autocomplete="new-password" placeholder="At least 12 characters"></label>
          </div>
        </div>
      </div>

      <p v-if="error" class="notice notice--error" role="alert">{{ error }}</p>
      <div class="form-actions">
        <RouterLink class="button button--quiet" to="/deployments">Cancel</RouterLink>
        <button class="button button--primary" :disabled="loading" type="submit">{{ loading ? 'Provisioning…' : 'Provision workload' }} <span>→</span></button>
      </div>
    </form>
  </div>
</template>
