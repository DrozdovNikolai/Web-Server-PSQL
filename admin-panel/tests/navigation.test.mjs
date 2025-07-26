import assert from 'node:assert/strict'
import test from 'node:test'
import { resolveAuthNavigation } from '../src/navigation.js'

test('anonymous users are redirected to login with their destination', () => {
  const result = resolveAuthNavigation(
    { name: 'database', fullPath: '/database', meta: { requiresAuth: true } },
    false
  )

  assert.deepEqual(result, {
    name: 'login',
    query: { redirect: '/database' }
  })
})

test('authenticated users cannot return to the login screen', () => {
  const result = resolveAuthNavigation(
    { name: 'login', fullPath: '/login', meta: {} },
    true
  )

  assert.deepEqual(result, { name: 'routine-gateway' })
})

test('allowed navigation continues without a redirect', () => {
  assert.equal(resolveAuthNavigation(
    { name: 'routine-gateway', fullPath: '/', meta: { requiresAuth: true } },
    true
  ), true)
})
