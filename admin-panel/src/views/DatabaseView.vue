<script setup>
import { onMounted, ref, watch } from 'vue'
import { api, problemMessage } from '../api'
import {
  buildExcelRequest,
  buildRoutineRequest,
  createExcelSettings,
  createRoutineInputs,
  formatRoutineCell,
  normalizeRoutineResult,
  routineEndpoint,
  routineKey
} from '../routineInvocation'

const schemas = ref([])
const tables = ref([])
const routines = ref([])
const triggers = ref([])
const rows = ref([])
const rowsTruncated = ref(false)
const selectedSchema = ref('public')
const selectedTable = ref('')
const selectedRoutine = ref(null)
const routineInputs = ref([])
const routineResult = ref(null)
const invoking = ref(false)
const exporting = ref(false)
const excelSettings = ref(null)
const loading = ref(true)
const error = ref('')
const routineError = ref('')

async function loadSchemas() {
  loading.value = true
  error.value = ''
  try {
    const { data } = await api.get('/database/schemas')
    schemas.value = data
    if (!data.some(item => item.name === selectedSchema.value) && data.length) selectedSchema.value = data[0].name
    await loadObjects()
  } catch (requestError) {
    error.value = problemMessage(requestError, 'Database metadata could not be loaded.')
  } finally {
    loading.value = false
  }
}

async function loadObjects() {
  selectedTable.value = ''
  selectedRoutine.value = null
  rows.value = []
  rowsTruncated.value = false
  routineInputs.value = []
  routineResult.value = null
  excelSettings.value = null
  routineError.value = ''
  const params = { schema: selectedSchema.value }
  const [tableResult, routineResultResponse, triggerResult] = await Promise.all([
    api.get('/database/tables', { params }),
    api.get('/database/routines', { params }),
    api.get('/database/triggers', { params })
  ])
  tables.value = tableResult.data
  routines.value = routineResultResponse.data
  triggers.value = triggerResult.data
}

async function preview(table) {
  selectedTable.value = table.name
  error.value = ''
  try {
    const { data } = await api.get(`/database/tables/${encodeURIComponent(table.schema)}/${encodeURIComponent(table.name)}/rows`, {
      params: { page: 1, pageSize: 20 }
    })
    rows.value = data.rows
    rowsTruncated.value = data.truncated
  } catch (requestError) {
    error.value = problemMessage(requestError, 'The table preview could not be loaded.')
  }
}

function selectRoutine(routine) {
  selectedRoutine.value = routine
  routineInputs.value = createRoutineInputs(routine)
  excelSettings.value = routine.returnsCursor ? createExcelSettings(routine) : null
  routineResult.value = null
  routineError.value = ''
}

async function invokeRoutine() {
  routineError.value = ''
  routineResult.value = null
  invoking.value = true
  try {
    const response = await api.post(
      routineEndpoint(selectedRoutine.value),
      buildRoutineRequest(selectedRoutine.value, routineInputs.value)
    )
    routineResult.value = normalizeRoutineResult(
      response.data,
      selectedRoutine.value,
      response.headers
    )
  } catch (requestError) {
    routineError.value = requestError.response
      ? problemMessage(requestError, 'The routine could not be invoked.')
      : requestError.message
  } finally {
    invoking.value = false
  }
}

async function exportRoutine() {
  routineError.value = ''
  exporting.value = true
  try {
    const response = await api.post(
      routineEndpoint(selectedRoutine.value, 'export/xlsx'),
      buildExcelRequest(selectedRoutine.value, routineInputs.value, excelSettings.value),
      { responseType: 'blob', timeout: 30_000 }
    )
    const disposition = response.headers['content-disposition'] || ''
    const encodedName = disposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1]
    const quotedName = disposition.match(/filename="?([^";]+)"?/i)?.[1]
    const fileName = encodedName
      ? decodeURIComponent(encodedName)
      : quotedName || excelSettings.value.fileName
    const url = URL.createObjectURL(response.data)
    const link = document.createElement('a')
    link.href = url
    link.download = fileName
    document.body.appendChild(link)
    link.click()
    link.remove()
    URL.revokeObjectURL(url)
  } catch (requestError) {
    if (requestError.response?.data instanceof Blob) {
      try {
        requestError.response.data = JSON.parse(await requestError.response.data.text())
      } catch {
        // Keep the generic message when the response is not JSON problem details.
      }
    }
    routineError.value = requestError.response
      ? problemMessage(requestError, 'The Excel report could not be generated.')
      : requestError.message
  } finally {
    exporting.value = false
  }
}

watch(selectedSchema, async (_, previous) => {
  if (previous) {
    loading.value = true
    try { await loadObjects() } catch (requestError) { error.value = problemMessage(requestError) } finally { loading.value = false }
  }
})

onMounted(loadSchemas)
</script>

<template>
  <div class="page">
    <header class="page-header">
      <div>
        <p class="eyebrow"><span></span> PostgreSQL routine gateway</p>
        <h1>Database objects</h1>
        <p class="muted">Browse relations and invoke allowlisted stored functions or procedures through typed REST calls.</p>
      </div>
      <label class="schema-select"><span>Schema</span><select v-model="selectedSchema"><option v-for="schema in schemas" :key="schema.name" :value="schema.name">{{ schema.name }}</option></select></label>
    </header>

    <p v-if="error" class="notice notice--error" role="alert">{{ error }}</p>
    <div v-if="loading" class="panel empty-state"><span class="spinner"></span><p>Reading catalog metadata…</p></div>

    <template v-else>
      <section class="object-grid">
        <article class="panel object-card"><small>Tables & views</small><strong>{{ tables.length }}</strong><span>Read-only previews in {{ selectedSchema }}</span></article>
        <article class="panel object-card"><small>REST routines</small><strong>{{ routines.filter(routine => routine.canInvoke).length }}</strong><span>{{ routines.length }} catalogued functions and procedures</span></article>
        <article class="panel object-card"><small>Triggers</small><strong>{{ triggers.length }}</strong><span>Catalog definitions</span></article>
      </section>

      <section class="database-layout">
        <div class="panel object-list">
          <div class="panel__header"><div><h2>Relations</h2><p>Select a table for a 20-row preview.</p></div></div>
          <button v-for="table in tables" :key="`${table.schema}.${table.name}`" type="button" class="relation" :class="{ 'relation--active': selectedTable === table.name }" @click="preview(table)">
            <span class="relation__icon">▦</span><span><strong>{{ table.name }}</strong><small>{{ table.type.toLowerCase() }}</small></span><i>→</i>
          </button>
          <div v-if="tables.length === 0" class="compact-empty">No relations in this schema.</div>
        </div>

        <div class="panel preview-panel">
          <div class="panel__header"><div><h2>{{ selectedTable || 'Table preview' }}</h2><p>{{ selectedTable ? `First page · up to 20 rows${rowsTruncated ? ' · response limit reached' : ''}` : 'Choose a relation from the catalog.' }}</p></div></div>
          <div v-if="rows.length" class="table-wrap table-wrap--preview">
            <table><thead><tr><th v-for="column in Object.keys(rows[0])" :key="column">{{ column }}</th></tr></thead><tbody><tr v-for="(row, index) in rows" :key="index"><td v-for="column in Object.keys(rows[0])" :key="column"><code>{{ formatRoutineCell(row[column]) }}</code></td></tr></tbody></table>
          </div>
          <div v-else class="empty-state empty-state--small"><span class="empty-state__glyph">▦</span><p>{{ selectedTable ? 'This relation returned no rows.' : 'Nothing selected yet.' }}</p></div>
        </div>
      </section>

      <section class="routine-workbench">
        <div class="panel routine-list">
          <div class="panel__header"><div><h2>Stored routines</h2><p>Named JSON arguments; overload-safe.</p></div></div>
          <button v-for="routine in routines" :key="routineKey(routine)" type="button" class="routine-item" :class="{ 'routine-item--active': selectedRoutine && routineKey(selectedRoutine) === routineKey(routine) }" @click="selectRoutine(routine)">
            <span><strong>{{ routine.name }}</strong><small>{{ routine.type.toLowerCase() }} · {{ routine.volatility.toLowerCase() }}</small></span>
            <span class="routine-item__state" :class="{ 'routine-item__state--ready': routine.canInvoke }">{{ routine.canInvoke ? 'REST ready' : 'blocked' }}</span>
          </button>
          <div v-if="routines.length === 0" class="compact-empty">No functions or procedures in this schema.</div>
        </div>

        <div class="panel routine-executor">
          <template v-if="selectedRoutine">
            <div class="panel__header routine-executor__header">
              <div><h2>{{ selectedRoutine.name }}</h2><p><code>{{ selectedRoutine.signature }}</code></p></div>
              <span class="status" :class="{ 'status--running': selectedRoutine.canInvoke }"><i></i>{{ selectedRoutine.type }}</span>
            </div>

            <div v-if="!selectedRoutine.canInvoke" class="routine-executor__body">
              <p class="notice notice--error" role="alert">{{ selectedRoutine.invocationDisabledReason }}</p>
            </div>

            <form v-else class="routine-executor__body" @submit.prevent="invokeRoutine">
              <div class="routine-summary">
                <span>Returns <code>{{ selectedRoutine.returnType || 'procedure result' }}</code></span>
                <span v-if="selectedRoutine.returnsSet">Set-returning</span>
                <span v-if="selectedRoutine.returnsCursor">Cursor → JSON / Excel</span>
                <span v-if="selectedRoutine.securityDefiner">Security definer</span>
              </div>

              <div v-if="routineInputs.length" class="routine-arguments">
                <div v-for="input in routineInputs" :key="input.position" class="routine-argument">
                  <label class="field">
                    <span>{{ input.name }} · {{ input.dataType }} · {{ input.required ? 'required' : 'optional' }}</span>
                    <input v-model="input.value" :disabled="input.useDefault || input.useNull" :placeholder="input.useNull ? 'PostgreSQL NULL' : `Value cast to ${input.dataType}`">
                  </label>
                  <div class="routine-argument__options">
                    <label v-if="!input.required"><input v-model="input.useDefault" type="checkbox" @change="input.useNull = false"> Use PostgreSQL default</label>
                    <label><input v-model="input.useNull" type="checkbox" :disabled="input.useDefault"> Send NULL</label>
                  </div>
                </div>
              </div>
              <p v-else class="routine-no-arguments">This routine has no input arguments.</p>

              <section v-if="selectedRoutine.returnsCursor && excelSettings" class="excel-settings">
                <div class="excel-settings__intro">
                  <div><strong>Excel report</strong><span>The function cursor is fetched in one transaction and written to a bounded .xlsx workbook.</span></div>
                  <span class="excel-badge">refcursor</span>
                </div>
                <div class="excel-settings__grid">
                  <label class="field"><span>File name</span><input v-model="excelSettings.fileName" maxlength="120" required></label>
                  <label class="field"><span>Worksheet</span><input v-model="excelSettings.sheetName" maxlength="31" required></label>
                  <label class="field"><span>Start row</span><input v-model.number="excelSettings.startRow" type="number" min="1" max="1048576" required></label>
                  <label class="field"><span>Start column</span><input v-model.number="excelSettings.startColumn" type="number" min="1" max="16384" required></label>
                </div>
                <div class="excel-settings__options">
                  <label><input v-model="excelSettings.autoFitColumns" type="checkbox"> Auto-fit result columns</label>
                  <label><input v-model="excelSettings.freezeHeader" type="checkbox"> Freeze report header</label>
                </div>
              </section>

              <p v-if="routineError" class="notice notice--error" role="alert">{{ routineError }}</p>
              <div class="routine-actions">
                <span>Arguments are named and bound; the selected overload is catalog-derived, while SQL text and PostgreSQL OIDs stay internal.</span>
                <div class="routine-actions__buttons">
                  <button v-if="selectedRoutine.returnsCursor" class="button button--quiet" type="button" :disabled="invoking || exporting" @click="exportRoutine">{{ exporting ? 'Exporting…' : 'Export .xlsx' }}</button>
                  <button class="button button--primary" type="submit" :disabled="invoking || exporting">{{ invoking ? 'Invoking…' : `Invoke ${selectedRoutine.type.toLowerCase()}` }}</button>
                </div>
              </div>
            </form>

            <div v-if="routineResult" class="routine-result">
              <div class="routine-result__meta"><strong>Invocation completed</strong><span>{{ routineResult.rows.length }} row(s)<template v-if="routineResult.durationMilliseconds !== null"> · {{ routineResult.durationMilliseconds }} ms</template><span v-if="routineResult.truncated"> · response truncated</span></span></div>
              <div v-if="routineResult.rows.length" class="table-wrap routine-result__table">
                <table><thead><tr><th v-for="column in routineResult.columns" :key="column">{{ column }}</th></tr></thead><tbody><tr v-for="(row, index) in routineResult.rows" :key="index"><td v-for="column in routineResult.columns" :key="column"><code>{{ formatRoutineCell(row[column]) }}</code></td></tr></tbody></table>
              </div>
              <p v-else class="compact-empty">The procedure completed without result rows.</p>
            </div>
          </template>
          <div v-else class="empty-state"><span class="empty-state__glyph">ƒ</span><p>Select a stored function or procedure to inspect its signature and invoke it through REST.</p></div>
        </div>
      </section>

      <section class="panel trigger-panel">
        <div class="panel__header"><div><h2>Triggers</h2><p>Read-only catalog metadata</p></div></div>
        <ul class="catalog"><li v-for="trigger in triggers" :key="`${trigger.name}-${trigger.event}`"><span><strong>{{ trigger.name }}</strong><small>{{ trigger.table }}</small></span><code>{{ trigger.timing }} {{ trigger.event }}</code></li><li v-if="!triggers.length" class="compact-empty">No triggers.</li></ul>
      </section>
    </template>
  </div>
</template>
