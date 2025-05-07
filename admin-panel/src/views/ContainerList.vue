<template>
  <div>
    <Card>
      <template #title>
        <div class="flex justify-content-between align-items-center">
          <h2>{{ $t('container.title') }}</h2>
          <Button :label="$t('container.addNew')" icon="pi pi-plus" @click="navigateToNew" />
        </div>
      </template>
      <template #content>
        <DataTable :value="containers" :loading="loading" stripedRows 
                  responsiveLayout="scroll" class="p-datatable-sm">
          <Column field="name" :header="$t('container.name')"></Column>
          <Column field="status" :header="$t('container.status')">
            <template #body="slotProps">
              <span :class="getStatusClass(slotProps.data.status)">
                {{ $t(`status.${slotProps.data.status.toLowerCase()}`) }}
              </span>
            </template>
          </Column>
          <Column field="dbHost" :header="$t('container.dbHost')"></Column>
          <Column field="dbName" :header="$t('container.dbName')"></Column>
          <Column field="createdAt" :header="$t('container.created')">
            <template #body="slotProps">
              {{ formatDate(slotProps.data.createdAt) }}
            </template>
          </Column>
          <Column :header="$t('common.actions')">
            <template #body="slotProps">
              <Button icon="pi pi-trash" class="p-button-danger p-button-sm"
                      @click="confirmDelete(slotProps.data)" />
              <Button icon="pi pi-refresh" class="p-button-info p-button-sm ml-2"
                      @click="restartContainer(slotProps.data)" />
            </template>
          </Column>
        </DataTable>
      </template>
    </Card>

    <Dialog :header="$t('container.confirmDelete')" v-model:visible="deleteDialog" :style="{width: '450px'}">
      <div class="confirmation-content">
        <i class="pi pi-exclamation-triangle p-mr-3" style="font-size: 2rem" />
        <span>{{ $t('container.deleteConfirmMessage') }}</span>
      </div>
      <template #footer>
        <Button :label="$t('common.no')" icon="pi pi-times" class="p-button-text" @click="deleteDialog = false" />
        <Button :label="$t('common.yes')" icon="pi pi-check" class="p-button-text" @click="deleteContainer" />
      </template>
    </Dialog>
  </div>
</template>

<script>
import { ref, computed, onMounted } from 'vue'
import { useStore } from 'vuex'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { useI18n } from 'vue-i18n'

export default {
  name: 'ContainerList',
  setup() {
    const store = useStore()
    const router = useRouter()
    const toast = useToast()
    const { t } = useI18n()
    const deleteDialog = ref(false)
    const selectedContainer = ref(null)
    
    // Load containers when component mounts
    onMounted(() => {
      store.dispatch('fetchContainers')
    })
    
    // Computed properties from store
    const containers = computed(() => store.getters.getContainers)
    const loading = computed(() => store.getters.isLoading)
    
    // Methods
    const navigateToNew = () => {
      router.push('/new')
    }
    
    const confirmDelete = (container) => {
      selectedContainer.value = container
      deleteDialog.value = true
    }
    
    const deleteContainer = async () => {
      const result = await store.dispatch('deleteContainer', selectedContainer.value.id)
      deleteDialog.value = false
      
      if (result.success) {
        toast.add({
          severity: 'success',
          summary: t('common.success'),
          detail: t('container.deleteSuccess'),
          life: 3000
        })
      } else {
        toast.add({
          severity: 'error',
          summary: t('common.error'),
          detail: t('container.deleteFail'),
          life: 3000
        })
      }
    }
    
    const restartContainer = async (container) => {
      // API call to restart container would go here
      toast.add({
        severity: 'info',
        summary: t('common.info'),
        detail: t('container.restartMessage', { name: container.name }),
        life: 3000
      })
    }
    
    const getStatusClass = (status) => {
      switch (status?.toLowerCase()) {
        case 'running':
          return 'status-badge status-badge-success'
        case 'pending':
          return 'status-badge status-badge-warning'
        case 'failed':
          return 'status-badge status-badge-danger'
        default:
          return 'status-badge'
      }
    }
    
    const formatDate = (dateString) => {
      if (!dateString) return ''
      const date = new Date(dateString)
      return date.toLocaleDateString() + ' ' + date.toLocaleTimeString()
    }
    
    return {
      containers,
      loading,
      deleteDialog,
      selectedContainer,
      navigateToNew,
      confirmDelete,
      deleteContainer,
      restartContainer,
      getStatusClass,
      formatDate
    }
  }
}
</script>

<style scoped>
.status-badge {
  padding: 0.25rem 0.5rem;
  border-radius: 3px;
  font-weight: 700;
  text-transform: uppercase;
  font-size: 0.75rem;
}

.status-badge-success {
  background-color: #a5d6a7;
  color: #2e7d32;
}

.status-badge-warning {
  background-color: #ffe082;
  color: #f57f17;
}

.status-badge-danger {
  background-color: #ef9a9a;
  color: #d32f2f;
}
</style> 