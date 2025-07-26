import assert from 'node:assert/strict'
import test from 'node:test'

class MemoryStorage {
  #values = new Map()

  getItem(key) {
    return this.#values.get(key) ?? null
  }

  setItem(key, value) {
    this.#values.set(key, String(value))
  }

  removeItem(key) {
    this.#values.delete(key)
  }
}

globalThis.sessionStorage = new MemoryStorage()
const { clearSession, getSession, hasSession, saveSession } = await import('../src/session.js')

test('session storage accepts a non-expired access token', () => {
  clearSession()
  const session = {
    accessToken: 'short-lived-test-token',
    expiresAt: new Date(Date.now() + 60_000).toISOString(),
    user: { username: 'operator', roles: ['superadmin'] }
  }

  saveSession(session)

  assert.deepEqual(getSession(), session)
  assert.equal(hasSession(), true)
})

test('expired or malformed sessions never authenticate the browser', () => {
  saveSession({
    accessToken: 'expired-test-token',
    expiresAt: new Date(Date.now() - 1_000).toISOString()
  })
  assert.equal(hasSession(), false)

  globalThis.sessionStorage.setItem('postgres-control-plane-session', '{invalid json')
  assert.equal(getSession(), null)
  assert.equal(hasSession(), false)
})
