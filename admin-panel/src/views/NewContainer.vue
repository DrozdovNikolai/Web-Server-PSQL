<template>
  <div>
    <Card>
      <template #title>
        <div class="flex justify-content-between align-items-center">
          <h2>Deploy New Container</h2>
          <Button label="Back to List" icon="pi pi-arrow-left" @click="navigateToList" class="p-button-secondary" />
        </div>
      </template>
      <template #content>
        <form @submit.prevent="submitForm" class="p-fluid">
          <div class="p-grid p-formgrid">
            <div class="field col-12">
              <label for="name">Container Name</label>
              <InputText id="name" v-model="form.name" :class="{'p-invalid': errors.name}" />
              <small v-if="errors.name" class="p-error">{{ errors.name }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbHost">DB Host</label>
              <InputText id="dbHost" v-model="form.dbHost" :class="{'p-invalid': errors.dbHost}" />
              <small v-if="errors.dbHost" class="p-error">{{ errors.dbHost }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbPort">DB Port</label>
              <InputText id="dbPort" v-model="form.dbPort" :class="{'p-invalid': errors.dbPort}" />
              <small v-if="errors.dbPort" class="p-error">{{ errors.dbPort }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbName">DB Name</label>
              <InputText id="dbName" v-model="form.dbName" :class="{'p-invalid': errors.dbName}" />
              <small v-if="errors.dbName" class="p-error">{{ errors.dbName }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbUser">DB User</label>
              <InputText id="dbUser" v-model="form.dbUser" :class="{'p-invalid': errors.dbUser}" />
              <small v-if="errors.dbUser" class="p-error">{{ errors.dbUser }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbPassword">DB Password</label>
              <InputText id="dbPassword" v-model="form.dbPassword" type="password" :class="{'p-invalid': errors.dbPassword}" />
              <small v-if="errors.dbPassword" class="p-error">{{ errors.dbPassword }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbUsername">DB Username (Admin)</label>
              <InputText id="dbUsername" v-model="form.dbUsername" :class="{'p-invalid': errors.dbUsername}" />
              <small v-if="errors.dbUsername" class="p-error">{{ errors.dbUsername }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbPasswordUser">DB Password (Admin)</label>
              <InputText id="dbPasswordUser" v-model="form.dbPasswordUser" type="password" :class="{'p-invalid': errors.dbPasswordUser}" />
              <small v-if="errors.dbPasswordUser" class="p-error">{{ errors.dbPasswordUser }}</small>
            </div>

            <div class="field col-12">
              <Button type="submit" label="Deploy Container" icon="pi pi-check" :loading="loading" />
            </div>
          </div>
        </form>
      </template>
    </Card>
  </div>
</template>

<script>
import { ref, computed } from 'vue'
import { useStore } from 'vuex'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'

export default {
  name: 'NewContainer',
  setup() {
    const store = useStore()
    const router = useRouter()
    const toast = useToast()
    
    // Form state
    const form = ref({
      name: '',
      dbHost: '195.93.252.168',
      dbPort: '5432',
      dbName: '',
      dbUser: 'postgres',
      dbPassword: '',
      dbUsername: 'Admin',
      dbPasswordUser: 'Admin'
    })
    
    // Error state
    const errors = ref({})
    
    // Loading state from store
    const loading = computed(() => store.getters.isLoading)
    
    // Methods
    const navigateToList = () => {
      router.push('/')
    }
    
    const validateForm = () => {
      const newErrors = {}
      
      if (!form.value.name) newErrors.name = 'Container name is required'
      if (!form.value.dbHost) newErrors.dbHost = 'DB Host is required'
      if (!form.value.dbPort) newErrors.dbPort = 'DB Port is required'
      if (!form.value.dbName) newErrors.dbName = 'DB Name is required'
      if (!form.value.dbUser) newErrors.dbUser = 'DB User is required'
      if (!form.value.dbPassword) newErrors.dbPassword = 'DB Password is required'
      if (!form.value.dbUsername) newErrors.dbUsername = 'DB Username is required'
      if (!form.value.dbPasswordUser) newErrors.dbPasswordUser = 'DB Password (Admin) is required'
      
      errors.value = newErrors
      return Object.keys(newErrors).length === 0
    }
    
    const submitForm = async () => {
      if (!validateForm()) {
        toast.add({
          severity: 'error',
          summary: 'Validation Error',
          detail: 'Please check the form for errors',
          life: 3000
        })
        return
      }
      
      const result = await store.dispatch('createContainer', form.value)
      
      if (result.success) {
        toast.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Container deployed successfully',
          life: 3000
        })
        router.push('/')
      } else {
        toast.add({
          severity: 'error',
          summary: 'Error',
          detail: result.error || 'Failed to deploy container',
          life: 3000
        })
      }
    }
    
    return {
      form,
      errors,
      loading,
      navigateToList,
      submitForm
    }
  }
}
</script> 