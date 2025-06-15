<template>
    <Form @submit="handleSubmit">
        <div class="form-group">
            <label for="dataSourceName">Data Source Name</label>
            <input id="dataSourceName" v-model="formData.name" type="text" required placeholder="e.g., training-images-q1" />
        </div>
        <div class="form-group">
            <label for="dataSourceDescription">Description (Optional)</label>
            <textarea id="dataSourceDescription" v-model="formData.description" rows="3" placeholder="A short description of this data source."></textarea>
        </div>
        <div class="form-group">
            <label for="dataSourceType">Data Source Type</label>
            <select id="dataSourceType" v-model="formData.sourceType" required :disabled="loading">
                <option v-if="loading" disabled>Loading available types...</option>
                <option v-for="type in availableDataSourceTypes" :key="type" :value="type">
                    {{ type.replace('_', ' ') }}
                </option>
            </select>
            <p v-if="!loading && availableDataSourceTypes.length === 0" class="no-types-message">
                No data source types are currently configured. Please contact your administrator.
            </p>
        </div>
        <div class="form-actions">
            <Button type="button" @click="$emit('cancel')" variant="secondary">Cancel</Button>
            <Button type="submit" variant="primary" :disabled="loading || availableDataSourceTypes.length === 0">
                Create Data Source
            </Button>
        </div>
    </Form>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import Form from '@/components/common/Form.vue';
import Button from '@/components/common/Button.vue';
import { DataSourceType, type CreateDataSourceRequest } from '@/types/dataSource';
import { useAlert } from '@/composables/useAlert';
import { dataSourceService } from '@/services/api/dataSourceService';
import { loggerInstance } from '@/utils/logger';

const { showAlert } = useAlert();
const log = loggerInstance.createServiceLogger('CreateDataSourceForm');

const props = defineProps<{
    projectId: number;
}>();

const emit = defineEmits<{
    (e: 'save', formData: CreateDataSourceRequest): void;
    (e: 'cancel'): void;
}>();

const availableDataSourceTypes = ref<DataSourceType[]>([]);
const loading = ref(false);

const formData = ref<CreateDataSourceRequest>({
    name: '',
    description: '',
    sourceType: DataSourceType.MINIO_BUCKET, // Will be updated when types are loaded
});

const fetchAvailableTypes = async () => {
    loading.value = true;
    try {
        availableDataSourceTypes.value = await dataSourceService.getAvailableTypes(props.projectId);
        // Set default to first available type
        if (availableDataSourceTypes.value.length > 0) {
            formData.value.sourceType = availableDataSourceTypes.value[0];
        }
    } catch (error) {
        log.error('Failed to fetch available data source types', error);
        await showAlert('Error', 'Failed to load available data source types');
    } finally {
        loading.value = false;
    }
};

onMounted(() => {
    fetchAvailableTypes();
});

const handleSubmit = async () => {
    if (!formData.value.name) {
        await showAlert('Missing Information', 'Data Source Name is required.');
        return;
    }
    
    if (availableDataSourceTypes.value.length === 0) {
        await showAlert('No Data Source Types', 'No data source types are available. Please contact your administrator.');
        return;
    }
    
    emit('save', { ...formData.value });
};
</script>

<style scoped>
.no-types-message {
    color: #e74c3c;
    font-size: 0.875rem;
    margin-top: 0.5rem;
    font-style: italic;
}

select:disabled {
    background-color: #f8f9fa;
    color: #6c757d;
    cursor: not-allowed;
}
</style>