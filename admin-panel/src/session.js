const sessionKey = 'postgres-control-plane-session'

export function getSession() {
  try {
    return JSON.parse(sessionStorage.getItem(sessionKey))
  } catch {
    clearSession()
    return null
  }
}

export function hasSession() {
  const session = getSession()
  return Boolean(session?.accessToken && new Date(session.expiresAt).getTime() > Date.now())
}

export function saveSession(session) {
  sessionStorage.setItem(sessionKey, JSON.stringify(session))
}

export function clearSession() {
  sessionStorage.removeItem(sessionKey)
}
