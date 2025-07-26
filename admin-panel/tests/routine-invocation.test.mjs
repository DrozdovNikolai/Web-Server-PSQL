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

test('routine arguments use names, preserve nulls and allow independent PostgreSQL defaults', () => {
  const argumentsList = buildRoutineArguments([
    { name: 'quantity', dataType: 'integer', value: '42', useNull: false, useDefault: false },
    { name: 'comment', dataType: 'text', value: '', useNull: true, useDefault: false },
    { name: 'tax', dataType: 'numeric', value: 'ignored', useNull: false, useDefault: true },
    { name: 'fee', dataType: 'numeric', value: '0.2', useNull: false, useDefault: false }
  ])

  assert.deepEqual(argumentsList, { quantity: 42, comment: null, fee: 0.2 })
})

test('routine argument parsing preserves precise big integers and parses typed JSON', () => {
  assert.deepEqual(buildRoutineArguments([
    { name: 'id', dataType: 'bigint', value: '9007199254740993', useNull: false, useDefault: false },
    { name: 'enabled', dataType: 'boolean', value: 'true', useNull: false, useDefault: false },
    { name: 'payload', dataType: 'jsonb', value: '{"status":"ok"}', useNull: false, useDefault: false }
  ]), {
    id: '9007199254740993',
    enabled: true,
    payload: { status: 'ok' }
  })
})

test('routine cells render structured and null values readably', () => {
  assert.equal(formatRoutineCell({ status: 'ok' }), '{"status":"ok"}')
  assert.equal(formatRoutineCell(null), 'null')
  assert.equal(formatRoutineCell(12.5), '12.5')
})

test('workbench requests use named arguments and an optional selected overload instead of OIDs', () => {
  const routine = {
    schema: 'public',
    name: 'calculate_total',
    overload: 'integer, numeric'
  }
  const inputs = [{ name: 'quantity', dataType: 'integer', value: '2', useNull: false, useDefault: false }]

  assert.equal(routineKey(routine), 'public.calculate_total(integer, numeric)')
  assert.equal(routineEndpoint(routine), '/database/routines/public/calculate_total/invoke')
  assert.deepEqual(buildRoutineRequest(routine, inputs), {
    overload: 'integer, numeric',
    arguments: { quantity: 2 }
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
    overload: 'numeric'
  }
  const settings = createExcelSettings(routine)
  settings.startRow = 2
  settings.startColumn = 3

  assert.deepEqual(buildExcelRequest(
    routine,
    [{ name: 'minimum', dataType: 'numeric', value: '100', useNull: false, useDefault: false }],
    settings
  ), {
    overload: 'numeric',
    arguments: { minimum: 100 },
    excel: {
      fileName: 'open_report.xlsx',
      sheetName: 'Report',
      reportStartPosition: { row: 2, column: 3 },
      autoFitColumns: true,
      freezePane: { rows: 2, columns: 0 }
    }
  })
})
