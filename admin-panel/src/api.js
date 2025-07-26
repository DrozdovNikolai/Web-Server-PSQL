import axios from 'axios'
import { clearSession, getSession } from './session'

export { clearSession, getSession, hasSession, saveSession } from './session'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/server/api',
  timeout: 15_000,
  headers: { Accept: 'application/json' }
})

api.interceptors.request.use((config) => {
  const session = getSession()
  if (session?.accessToken) {
    config.headers.Authorization = `Bearer ${session.accessToken}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      clearSession()
      window.dispatchEvent(new Event('auth-expired'))
    }
    return Promise.reject(error)
  }
)

export function problemMessage(error, fallback = 'The request could not be completed.') {
  const message = error.response?.data?.detail || error.response?.data?.title || fallback
  const correlationId = error.response?.headers?.['x-correlation-id']
  return correlationId ? `${message} Reference: ${correlationId}` : message
}
