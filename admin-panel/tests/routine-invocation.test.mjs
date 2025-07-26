import assert from 'node:assert/strict'
import test from 'node:test'
import {
  buildExcelRequest,
  buildRoutineArguments,
  buildRoutineRequest,
  createExcelSettings,
  createRoutineInputs,
  formatRoutineCell,
  normalizeRoutineResult,
  routineEndpoint,
  routineKey
} from '../src/routineInvocation.js'

test('routine inputs include IN and INOUT parameters but not OUT values', () => {
  const inputs = createRoutineInputs({
    arguments: [
      { name: 'source', mode: 'IN', required: true },
      { name: 'result', mode: 'OUT', required: false },
      { name: 'counter', mode: 'INOUT', required: true },
      { name: 'tax', mode: 'IN', required: false }
    ]
  })

  assert.deepEqual(inputs.map(input => input.name), ['source', 'counter', 'tax'])
  assert.deepEqual(inputs.map(input => input.useDefault), [false, false, true])
})

test('routine arguments preserve text values, nulls and trailing PostgreSQL defaults', () => {
  const argumentsList = buildRoutineArguments([
    { value: '42', useNull: false, useDefault: false },
    { value: '', useNull: true, useDefault: false },
    { value: 'ignored', useNull: false, useDefault: true }
  ])

  assert.deepEqual(argumentsList, ['42', null])
})

test('a positional REST invocation rejects gaps before supplied optional arguments', () => {
  assert.throws(() => buildRoutineArguments([
    { value: '', useNull: false, useDefault: true },
    { value: 'later', useNull: false, useDefault: false }
  ]), /only from the end/)
})

test('routine cells render structured and null values readably', () => {
  assert.equal(formatRoutineCell({ status: 'ok' }), '{"status":"ok"}')
  assert.equal(formatRoutineCell(null), 'null')
  assert.equal(formatRoutineCell(12.5), '12.5')
})

test('stable routine requests use schema, name and identity argument types instead of OIDs', () => {
  const routine = {
    schema: 'public',
    name: 'calculate_total',
    identityArguments: 'integer, numeric'
  }
  const inputs = [{ value: '2', useNull: false, useDefault: false }]

  assert.equal(routineKey(routine), 'public.calculate_total(integer, numeric)')
  assert.equal(routineEndpoint(routine), '/database/routines/public/calculate_total/invoke')
  assert.deepEqual(buildRoutineRequest(routine, inputs), {
    identityArguments: 'integer, numeric',
    arguments: ['2']
  })
})

test('raw scalar, object and set results are normalized only for table rendering', () => {
  const routine = { name: 'calculate_total' }
  assert.deepEqual(normalizeRoutineResult(12.5, routine).rows, [{ calculate_total: 12.5 }])
  assert.deepEqual(normalizeRoutineResult({ total: 12.5 }, routine).columns, ['total'])
  assert.deepEqual(
    normalizeRoutineResult([{ id: 1 }, { id: 2 }], routine, {
      'x-result-truncated': 'true',
      'server-timing': 'postgres-routine;dur=4'
    }),
    {
      columns: ['id'],
      rows: [{ id: 1 }, { id: 2 }],
      truncated: true,
      durationMilliseconds: 4
    }
  )
})

test('Excel export reuses routine arguments and emits bounded presentation settings', () => {
  const routine = {
    name: 'open_report',
    identityArguments: 'numeric'
  }
  const settings = createExcelSettings(routine)
  settings.startRow = 2
  settings.startColumn = 3

  assert.deepEqual(buildExcelRequest(
    routine,
    [{ value: '100', useNull: false, useDefault: false }],
    settings
  ), {
    identityArguments: 'numeric',
    arguments: ['100'],
    excel: {
      fileName: 'open_report.xlsx',
      sheetName: 'Report',
      reportStartPosition: { row: 2, column: 3 },
      autoFitColumns: true,
      freezePane: { rows: 2, columns: 0 }
    }
  })
})
