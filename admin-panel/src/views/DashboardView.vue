<script setup>
import { computed, onMounted, ref } from 'vue'
import { api, problemMessage } from '../api'

const deployments = ref([])
const totalDeployments = ref(0)
const loading = ref(true)
const error = ref('')
const busyId = ref(null)

const activeCount = computed(() => deployments.value.filter(item => item.status?.toLowerCase() === 'running').length)
const simulatedCount = computed(() => deployments.value.filter(item => item.status?.toLowerCase() === 'simulated').length)

async function loadDeployments() {
  loading.value = true
  error.value = ''
  try {
    const { data } = await api.get('/deployments')
    deployments.value = data.items
    totalDeployments.value = data.total
  } catch (requestError) {
    error.value = problemMessage(requestError, 'Deployments could not be loaded.')
  } finally {
    loading.value = false
  }
}

async function restart(deployment) {
  busyId.value = deployment.id
  error.value = ''
  try {
    await api.post(`/deployments/${deployment.id}/restart`)
    await loadDeployments()
  } catch (requestError) {
    error.value = problemMessage(requestError, 'The deployment could not be restarted.')
  } finally {
    busyId.value = null
  }
}

async function remove(deployment) {
  if (!window.confirm(`Delete “${deployment.name}” and its namespaced workload resources?`)) return

  busyId.value = deployment.id
  error.value = ''
  try {
    await api.delete(`/deployments/${deployment.id}`)
    deployments.value = deployments.value.filter(item => item.id !== deployment.id)
    totalDeployments.value = Math.max(0, totalDeployments.value - 1)
  } catch (requestError) {
    error.value = problemMessage(requestError, 'The deployment could not be deleted.')
  } finally {
    busyId.value = null
  }
}

function formatDate(value) {
  return value ? new Intl.DateTimeFormat('en', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '—'
}

onMounted(loadDeployments)
</script>

<template>
  <div class="page">
    <header class="page-header">
      <div>
        <p class="eyebrow"><span></span> Infrastructure</p>
        <h1>Deployment overview</h1>
        <p class="muted">A calm, auditable view of every isolated application workload.</p>
      </div>
      <RouterLink class="button button--primary" to="/deployments/new">New deployment <span>＋</span></RouterLink>
    </header>

    <section class="metrics" aria-label="Deployment summary">
      <article class="metric metric--accent"><small>Total workloads</small><strong>{{ totalDeployments }}</strong><span>Tracked by the control plane</span></article>
      <article class="metric"><small>Running</small><strong>{{ activeCount }}</strong><span class="live-indicator">Healthy workloads</span></article>
      <article class="metric"><small>Simulation mode</small><strong>{{ simulatedCount }}</strong><span>Safe local previews</span></article>
    </section>

    <p v-if="error" class="notice notice--error" role="alert">{{ error }}</p>

    <section class="panel">
      <div class="panel__header">
        <div><h2>Workloads</h2><p>Credentials are never returned by this API.</p></div>
        <button class="button button--quiet" type="button" :disabled="loading" @click="loadDeployments">Refresh</button>
      </div>

      <div v-if="loading" class="empty-state"><span class="spinner"></span><p>Loading deployments…</p></div>
      <div v-else-if="deployments.length === 0" class="empty-state">
        <span class="empty-state__glyph">⌘</span>
        <h3>No deployments yet</h3>
        <p>Create a simulated workload locally or connect the Kubernetes adapter.</p>
        <RouterLink class="button button--primary" to="/deployments/new">Create the first one</RouterLink>
      </div>
      <div v-else class="table-wrap">
        <table>
          <thead><tr><th>Workload</th><th>Status</th><th>Database</th><th>Created</th><th><span class="sr-only">Actions</span></th></tr></thead>
          <tbody>
            <tr v-for="deployment in deployments" :key="deployment.id">
              <td><div class="resource-name"><span>{{ deployment.name.slice(0, 2).toUpperCase() }}</span><div><strong>{{ deployment.name }}</strong><small>{{ deployment.externalUrl || 'Internal access only' }}</small></div></div></td>
              <td><span class="status" :class="`status--${deployment.status.toLowerCase()}`"><i></i>{{ deployment.status }}</span></td>
              <td><strong>{{ deployment.databaseName }}</strong><small class="table-subtitle">{{ deployment.databaseHost }}:{{ deployment.databasePort }}</small></td>
              <td>{{ formatDate(deployment.createdAt) }}</td>
              <td class="row-actions">
                <button class="icon-button" title="Restart" :disabled="busyId === deployment.id" @click="restart(deployment)">↻</button>
                <button class="icon-button icon-button--danger" title="Delete" :disabled="busyId === deployment.id" @click="remove(deployment)">×</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>
