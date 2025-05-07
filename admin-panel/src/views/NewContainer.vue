<template>
  <div>
    <Card>
      <template #title>
        <div class="flex justify-content-between align-items-center">
          <h2>{{ $t('newContainer.title') }}</h2>
          <Button :label="$t('newContainer.backToList')" icon="pi pi-arrow-left" @click="navigateToList" class="p-button-secondary" />
        </div>
      </template>
      <template #content>
        <form @submit.prevent="submitForm" class="p-fluid">
          <div class="p-grid p-formgrid">
            <div class="field col-12">
              <label for="name">{{ $t('newContainer.containerName') }}</label>
              <InputText id="name" v-model="form.name" :class="{'p-invalid': errors.name}" />
              <small v-if="errors.name" class="p-error">{{ errors.name }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbHost">{{ $t('newContainer.dbHost') }}</label>
              <InputText id="dbHost" v-model="form.dbHost" :class="{'p-invalid': errors.dbHost}" />
              <small v-if="errors.dbHost" class="p-error">{{ errors.dbHost }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbPort">{{ $t('newContainer.dbPort') }}</label>
              <InputText id="dbPort" v-model="form.dbPort" :class="{'p-invalid': errors.dbPort}" />
              <small v-if="errors.dbPort" class="p-error">{{ errors.dbPort }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbName">{{ $t('newContainer.dbName') }}</label>
              <InputText id="dbName" v-model="form.dbName" :class="{'p-invalid': errors.dbName}" />
              <small v-if="errors.dbName" class="p-error">{{ errors.dbName }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbUser">{{ $t('newContainer.dbUser') }}</label>
              <InputText id="dbUser" v-model="form.dbUser" :class="{'p-invalid': errors.dbUser}" />
              <small v-if="errors.dbUser" class="p-error">{{ errors.dbUser }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbPassword">{{ $t('newContainer.dbPassword') }}</label>
              <InputText id="dbPassword" v-model="form.dbPassword" type="password" :class="{'p-invalid': errors.dbPassword}" />
              <small v-if="errors.dbPassword" class="p-error">{{ errors.dbPassword }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbUsername">{{ $t('newContainer.dbUsername') }}</label>
              <InputText id="dbUsername" v-model="form.dbUsername" :class="{'p-invalid': errors.dbUsername}" />
              <small v-if="errors.dbUsername" class="p-error">{{ errors.dbUsername }}</small>
            </div>

            <div class="field col-12 md:col-6">
              <label for="dbPasswordUser">{{ $t('newContainer.dbPasswordUser') }}</label>
              <InputText id="dbPasswordUser" v-model="form.dbPasswordUser" type="password" :class="{'p-invalid': errors.dbPasswordUser}" />
              <small v-if="errors.dbPasswordUser" class="p-error">{{ errors.dbPasswordUser }}</small>
            </div>

            <div class="field col-12">
              <Button type="submit" :label="$t('newContainer.deployContainer')" icon="pi pi-check" :loading="loading" />
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
import { useI18n } from 'vue-i18n'

export default {
  name: 'NewContainer',
  setup() {
    const store = useStore()
    const router = useRouter()
    const toast = useToast()
    const { t } = useI18n()
    
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
      
      if (!form.value.name) newErrors.name = t('validation.required', { field: t('newContainer.containerName') })
      if (!form.value.dbHost) newErrors.dbHost = t('validation.required', { field: t('newContainer.dbHost') })
      if (!form.value.dbPort) newErrors.dbPort = t('validation.required', { field: t('newContainer.dbPort') })
      if (!form.value.dbName) newErrors.dbName = t('validation.required', { field: t('newContainer.dbName') })
      if (!form.value.dbUser) newErrors.dbUser = t('validation.required', { field: t('newContainer.dbUser') })
      if (!form.value.dbPassword) newErrors.dbPassword = t('validation.required', { field: t('newContainer.dbPassword') })
      if (!form.value.dbUsername) newErrors.dbUsername = t('validation.required', { field: t('newContainer.dbUsername') })
      if (!form.value.dbPasswordUser) newErrors.dbPasswordUser = t('validation.required', { field: t('newContainer.dbPasswordUser') })
      
      errors.value = newErrors
      return Object.keys(newErrors).length === 0
    }
    
    const submitForm = async () => {
      if (!validateForm()) {
        toast.add({
          severity: 'error',
          summary: t('newContainer.validationError'),
          detail: t('newContainer.validationMessage'),
          life: 3000
        })
        return
      }
      
      const result = await store.dispatch('createContainer', form.value)
      
      if (result.success) {
        toast.add({
          severity: 'success',
          summary: t('common.success'),
          detail: t('newContainer.deploySuccess'),
          life: 3000
        })
        router.push('/')
      } else {
        toast.add({
          severity: 'error',
          summary: t('common.error'),
          detail: result.error || t('newContainer.deployFail'),
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