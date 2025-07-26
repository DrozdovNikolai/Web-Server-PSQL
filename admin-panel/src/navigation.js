export function resolveAuthNavigation(to, authenticated) {
  if (to.meta.requiresAuth && !authenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.name === 'login' && authenticated) {
    return { name: 'routine-gateway' }
  }

  return true
}
