export function isInputArgument(argument) {
  return argument.mode === 'IN' || argument.mode === 'INOUT'
}

export function createRoutineInputs(routine) {
  return routine.arguments
    .filter(isInputArgument)
    .map((argument) => ({
      ...argument,
      value: '',
      useNull: false,
      useDefault: !argument.required
    }))
}

export function buildRoutineArguments(inputs) {
  const firstDefault = inputs.findIndex(input => input.useDefault)
  if (firstDefault >= 0 && inputs.slice(firstDefault + 1).some(input => !input.useDefault)) {
    throw new Error('Optional arguments can use their PostgreSQL defaults only from the end of the signature.')
  }

  const supplied = firstDefault < 0 ? inputs : inputs.slice(0, firstDefault)
  return supplied.map(input => input.useNull ? null : input.value)
}

export function formatRoutineCell(value) {
  if (value === null || value === undefined) return 'null'
  return typeof value === 'object' ? JSON.stringify(value) : String(value)
}

export function routineKey(routine) {
  return `${routine.schema}.${routine.name}(${routine.identityArguments || ''})`
}

export function routineEndpoint(routine, action = 'invoke') {
  return `/database/routines/${encodeURIComponent(routine.schema)}/${encodeURIComponent(routine.name)}/${action}`
}

export function buildRoutineRequest(routine, inputs) {
  return {
    identityArguments: routine.identityArguments || null,
    arguments: buildRoutineArguments(inputs)
  }
}

export function normalizeRoutineResult(value, routine, headers = {}) {
  const result = {
    columns: [],
    rows: [],
    truncated: headers['x-result-truncated'] === 'true',
    durationMilliseconds: parseServerTiming(headers['server-timing'])
  }

  if (value === null || value === undefined || value === '') return result

  if (Array.isArray(value)) {
    if (value.length === 0) return result
    if (value.every(item => item && typeof item === 'object' && !Array.isArray(item))) {
      result.columns = [...new Set(value.flatMap(item => Object.keys(item)))]
      result.rows = value
      return result
    }

    result.columns = [routine.name]
    result.rows = value.map(item => ({ [routine.name]: item }))
    return result
  }

  if (typeof value === 'object') {
    result.columns = Object.keys(value)
    result.rows = [value]
    return result
  }

  result.columns = [routine.name]
  result.rows = [{ [routine.name]: value }]
  return result
}

function parseServerTiming(value) {
  const match = typeof value === 'string' ? value.match(/(?:^|,)\s*postgres-routine;dur=([0-9.]+)/) : null
  return match ? Number(match[1]) : null
}

export function createExcelSettings(routine) {
  return {
    fileName: `${routine.name}.xlsx`,
    sheetName: 'Report',
    startRow: 1,
    startColumn: 1,
    autoFitColumns: true,
    freezeHeader: true
  }
}

export function buildExcelRequest(routine, inputs, settings) {
  return {
    ...buildRoutineRequest(routine, inputs),
    excel: {
      fileName: settings.fileName,
      sheetName: settings.sheetName,
      reportStartPosition: {
        row: Number(settings.startRow),
        column: Number(settings.startColumn)
      },
      autoFitColumns: settings.autoFitColumns,
      freezePane: {
        rows: settings.freezeHeader ? Number(settings.startRow) : 0,
        columns: 0
      }
    }
  }
}
