<template>
    <div class="data-sources-page">
        <div class="page-header">
            <h1>Data Sources</h1>
            <p>Manage collections of assets for annotation. Data sources can be buckets from S3-compatible storage or other origins.</p>
        </div>
        
        <div class="data-sources-list">
            <div v-if="isLoading" class="loading-message">
                Loading data sources...
            </div>
            <template v-else>
                <DataSourceCard
                    v-for="source in dataSources"
                    :key="source.id"
                    :data-source="source"
                    @assets-imported="handleAssetsImported(source.id, $event)"
                />
                <p v-if="!dataSources || dataSources.length === 0" class="no-content-message">
                    No data sources have been created for this project yet.
                </p>
            </template>
        </div>

        <Button class="fab" @click="openModal" aria-label="Create New Data Source">+</Button>

        <ModalWindow :is-open="isModalOpen" title="Create New Data Source" @close="closeModal" :hide-footer="true">
            <CreateDataSourceForm 
                :project-id="Number(route.params.projectId)" 
                @cancel="closeModal" 
                @save="handleCreateDataSource" 
            />
        </ModalWindow>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import DataSourceCard from '@/components/project/DataSourceCard.vue';
import ModalWindow from '@/components/common/modal/ModalWindow.vue';
import CreateDataSourceForm from '@/components/project/CreateDataSourceForm.vue';
import Button from '@/components/common/Button.vue';
import { type DataSource, type CreateDataSourceRequest } from '@/types/dataSource';
import { dataSourceService } from '@/services/api/dataSourceService';
import { useAlert } from '@/composables/useAlert';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createComponentLogger('DataSourcesView');
const route = useRoute();
const { showAlert } = useAlert();

const dataSources = ref<DataSource[]>([]);
const isModalOpen = ref(false);
const isLoading = ref(false);
const isCreating = ref(false);

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
        
        // Optionally reload data sources to ensure we have the most up-to-date information
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

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.page-header {
    margin-bottom: vars.$margin-xlarge;
    h1 {
        font-size: vars.$font-size-xlarge;
        margin-bottom: vars.$padding-xsmall;
    }
    p {
        color: vars.$theme-text-light;
        margin-bottom: vars.$margin-medium;
        max-width: 80ch;
    }
}

.data-sources-list {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
    gap: vars.$gap-large;
}

.no-content-message,
.loading-message {
    color: vars.$theme-text-light;
    font-style: italic;
}

@keyframes fab-enter {
  from {
    transform: scale(0);
    opacity: 0;
  }
  to {
    transform: scale(1);
    opacity: 1;
  }
}

.fab {
    position: absolute;
    bottom: vars.$padding-xlarge;
    right: vars.$padding-xlarge;
    width: 60px;
    height: 60px;
    border-radius: 50%;
    background-color: vars.$color-primary;
    color: vars.$color-white;
    border: none;
    font-size: 2.5rem;
    line-height: 1;
    box-shadow: vars.$shadow-md;
    cursor: pointer;
    display: flex;
    justify-content: center;
    align-items: center;
    padding-bottom: 4px;
    transition: background-color 0.2s ease-in-out, transform 0.2s ease-in-out;
    animation: fab-enter 0.2s ease-out 0.35s backwards;

    &:hover {
        background-color: vars.$color-primary-hover;
        transform: scale(1.1);
        transition: transform 0.2s ease, background-color 0.3s ease;
    }
}

.data-sources-page.fade-slide-leave-active .fab {
    opacity: 0;
}
</style>