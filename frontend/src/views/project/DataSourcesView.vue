<template>
    <div class="data-sources-page">
        <!-- Permission check for data sources management (Manager only) -->
        <div v-if="!canManageDataSources" class="access-denied">
            <h2>Access Denied</h2>
            <p>You don't have permission to manage data sources. This section is only available to project managers.</p>
        </div>
        
        <div v-else>
            <div class="page-header">
                <h1>Data Sources</h1>
                <p>Manage collections of assets for annotation. Data sources can be buckets from S3-compatible storage or other origins.</p>
            </div>
        
        <!-- TODO: Add bulk operations (select multiple, batch delete/archive) -->
        <div class="data-sources-list">
            <div v-if="isLoading" class="loading-message">
                Loading data sources...
            </div>
            <template v-else>
                <!-- TODO: Add data source health status indicators -->
                <DataSourceCard
                    v-for="source in dataSources"
                    :key="source.id"
                    :data-source="source"
                    @assets-imported="handleAssetsImported(source.id, $event)"
                />
                <p v-if="!dataSources || dataSources.length === 0" class="no-content-message">
                    No data sources have been created for this project yet.
                    <!-- TODO: Add quick setup guide for first data source -->
                </p>
            </template>
        </div>

            <FloatingActionButton 
                :permission="PERMISSIONS.DATA_SOURCE.CREATE"
                :onClick="openModal" 
                aria-label="Create New Data Source"
                title="Create New Data Source"
            />

            <ModalWindow :is-open="isModalOpen" title="Create New Data Source" @close="closeModal" :hide-footer="true">
                <CreateDataSourceForm 
                    :project-id="Number(route.params.projectId)" 
                    @cancel="closeModal" 
                    @save="handleCreateDataSource" 
                />
            </ModalWindow>
        </div>
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, ref} from 'vue';
import {useRoute} from 'vue-router';
import DataSourceCard from '@/components/project/dataSource/DataSourceCard.vue';
import ModalWindow from '@/components/common/modal/ModalWindow.vue';
import CreateDataSourceForm from '@/components/project/dataSource/CreateDataSourceForm.vue';
import FloatingActionButton from '@/components/common/FloatingActionButton.vue';
import {type CreateDataSourceRequest, type DataSource} from '@/services/project/dataSource/dataSource.types';
import {dataSourceService} from '@/services/project';
import {useAlert} from '@/composables/useAlert';
import {AppLogger} from '@/core/logger/logger';
import {usePermissions} from '@/composables/usePermissions';
import {PERMISSIONS} from '@/services/auth/permissions.types';

const logger = AppLogger.createComponentLogger('DataSourcesView');
const route = useRoute();
const { showAlert } = useAlert();
const { hasProjectPermission } = usePermissions();

const dataSources = ref<DataSource[]>([]);
const isModalOpen = ref(false);
const isLoading = ref(false);
const isCreating = ref(false);

// Permission checks - READ permission required as baseline to view data sources page
const canManageDataSources = computed(() => 
    hasProjectPermission(PERMISSIONS.DATA_SOURCE.READ)
);

const openModal = () => isModalOpen.value = true;
const closeModal = () => isModalOpen.value = false;

const loadDataSources = async () => {
    const projectId = Number(route.params.projectId);
    if (!projectId || isNaN(projectId)) {
        logger.error('Invalid project ID in route params', route.params.projectId);
        await showAlert('Error', 'Invalid project ID');
        return;
    }

    isLoading.value = true;
    try {
        const response = await dataSourceService.getDataSources({ projectId });
        dataSources.value = response.data;
        logger.info(`Loaded ${response.data.length} data sources for project ${projectId}`);
    } catch (error) {
        logger.error('Failed to load data sources', error);
        await showAlert('Error', 'Failed to load data sources. Please try again.');
    } finally {
        isLoading.value = false;
    }
};

const handleCreateDataSource = async (formData: CreateDataSourceRequest) => {
    const projectId = Number(route.params.projectId);
    if (!projectId || isNaN(projectId)) {
        logger.error('Invalid project ID in route params', route.params.projectId);
        await showAlert('Error', 'Invalid project ID');
        return;
    }

    isCreating.value = true;
    try {
        const newDataSource = await dataSourceService.createDataSource(projectId, formData);
        dataSources.value.push(newDataSource);
        logger.info(`Created data source: ${newDataSource.name} (ID: ${newDataSource.id})`);
        closeModal();
        
        // TODO: Implement smart refresh - only reload if necessary
        // await loadDataSources();
    } catch (error) {
        logger.error('Failed to create data source', error);
        await showAlert('Error', 'Failed to create data source. Please try again.');
    } finally {
        isCreating.value = false;
    }
};

const handleAssetsImported = async (dataSourceId: number, importedCount: number) => {
    // Find the data source and update its asset count
    const dataSource = dataSources.value.find(ds => ds.id === dataSourceId);
    if (dataSource) {
        // Update the local count immediately for responsive UI
        dataSource.assetCount = (dataSource.assetCount || 0) + importedCount;
        
        // Refresh from server to ensure accuracy
        try {
            const updatedDataSource = await dataSourceService.getDataSource(dataSource.projectId, dataSourceId);
            // Update with the server's asset count
            dataSource.assetCount = updatedDataSource.assetCount;
            logger.info(`Refreshed asset count for data source ${dataSourceId}: ${updatedDataSource.assetCount}`);
        } catch (error) {
            logger.warn(`Failed to refresh asset count from server for data source ${dataSourceId}`, error);
            // Keep the local count if server refresh fails
        }
    }
};

onMounted(() => {
    loadDataSources();
});
</script>

<style scoped>
.page-header {
    margin-bottom: 2rem;
    h1 {
        font-size: 1.5rem;
        margin-bottom: 0.25rem;
    }
    p {
        color: var(--color-gray-600);
        margin-bottom: 1rem;
        max-width: 80ch;
    }
}

.data-sources-list {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
    gap: 1.5rem;
}

.no-content-message,
.loading-message {
    color: var(--color-gray-600);
    font-style: italic;
}

.access-denied {
    text-align: center;
    padding: 3rem 2rem;
    color: var(--color-gray-600);
    
    h2 {
        color: var(--color-error);
        margin-bottom: 1rem;
    }
    
    p {
        max-width: 60ch;
        margin: 0 auto;
        line-height: 1.5;
    }
}

</style>