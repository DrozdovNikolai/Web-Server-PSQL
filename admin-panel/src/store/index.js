import { createStore } from 'vuex'
import axios from 'axios'

// Set the base API URL to use the NodePort service
const apiUrl = import.meta.env.VITE_API_URL || 'https://ncatbird.ru/ums/server/api'

export default createStore({
  state: {
    containers: [],
    loading: false,
    error: null
  },
  getters: {
    getContainers: state => state.containers,
    isLoading: state => state.loading,
    hasError: state => state.error !== null
  },
  mutations: {
    SET_CONTAINERS(state, containers) {
      state.containers = containers
    },
    SET_LOADING(state, loading) {
      state.loading = loading
    },
    SET_ERROR(state, error) {
      state.error = error
    },
    ADD_CONTAINER(state, container) {
      state.containers.push(container)
    }
  },
  actions: {
    async fetchContainers({ commit }) {
      commit('SET_LOADING', true)
      try {
        const response = await axios.get(`${apiUrl}/containers`)
        commit('SET_CONTAINERS', response.data)
        commit('SET_ERROR', null)
      } catch (error) {
        console.error('Error fetching containers:', error)
        commit('SET_ERROR', 'Failed to fetch containers')
      } finally {
        commit('SET_LOADING', false)
      }
    },
    async createContainer({ commit }, containerData) {
      commit('SET_LOADING', true)
      try {
        const response = await axios.post(`${apiUrl}/containers`, containerData)
        commit('ADD_CONTAINER', response.data)
        commit('SET_ERROR', null)
        return { success: true, data: response.data }
      } catch (error) {
        console.error('Error creating container:', error)
        commit('SET_ERROR', 'Failed to create container')
        return { success: false, error: error.response?.data || 'Unknown error' }
      } finally {
        commit('SET_LOADING', false)
      }
    },
    async deleteContainer({ commit, state }, containerId) {
      commit('SET_LOADING', true)
      try {
        await axios.delete(`${apiUrl}/containers/${containerId}`)
        const updatedContainers = state.containers.filter(container => container.id !== containerId)
        commit('SET_CONTAINERS', updatedContainers)
        commit('SET_ERROR', null)
        return { success: true }
      } catch (error) {
        console.error('Error deleting container:', error)
        commit('SET_ERROR', 'Failed to delete container')
        return { success: false, error: error.response?.data || 'Unknown error' }
      } finally {
        commit('SET_LOADING', false)
      }
    }
  }
}) 