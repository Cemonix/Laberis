<template>
    <div class="data-sources-page">
        <div class="page-header">
            <h1>Data Sources</h1>
            <p>Manage collections of assets for annotation. Data sources can be buckets from S3-compatible storage or other origins.</p>
        </div>
        
        <div class="data-sources-list">
            <DataSourceCard
                v-for="source in dataSources"
                :key="source.id"
                :data-source="source"
            />
            <p v-if="!dataSources || dataSources.length === 0" class="no-content-message">
                No data sources have been created for this project yet.
            </p>
        </div>

        <Button class="fab" @click="openModal" aria-label="Create New Data Source">+</Button>

        <ModalWindow :is-open="isModalOpen" title="Create New Data Source" @close="closeModal" :hide-footer="true">
            <CreateDataSourceForm :project-id="Number(route.params.projectId)" @cancel="closeModal" @save="handleCreateDataSource" />
        </ModalWindow>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import DataSourceCard from '@/components/project/DataSourceCard.vue';
import ModalWindow from '@/components/common/modals/ModalWindow.vue';
import CreateDataSourceForm from '@/components/project/CreateDataSourceForm.vue';
import Button from '@/components/common/Button.vue';
import { DataSourceType, DataSourceStatus, type DataSource, type CreateDataSourceRequest } from '@/types/dataSource';

const route = useRoute();
const dataSources = ref<DataSource[]>([]);
const isModalOpen = ref(false);

const openModal = () => isModalOpen.value = true;
const closeModal = () => isModalOpen.value = false;

const handleCreateDataSource = (formData: CreateDataSourceRequest) => {
    const projectId = Number(route.params.projectId);

    const newDataSource: DataSource = {
        id: Date.now(), // Mock ID
        name: formData.name,
        description: formData.description,
        sourceType: formData.sourceType,
        status: DataSourceStatus.ACTIVE,
        isDefault: false,
        projectId: projectId,
        createdAt: new Date().toISOString(),
    };
    dataSources.value.push(newDataSource);

    // TODO: Integrate with a real API service
    closeModal();
};

onMounted(() => {
    // Mock data for the view
    dataSources.value = [
        {
            id: 1,
            name: 'default-source',
            description: 'Default data source created with the project for initial uploads.',
            sourceType: DataSourceType.MINIO_BUCKET,
            status: DataSourceStatus.ACTIVE,
            isDefault: true,
            projectId: Number(route.params.projectId),
            createdAt: '2025-06-08T12:00:00Z',
        },
        {
            id: 2,
            name: 'archive-2024-images',
            description: 'A collection of historical images from the 2024 archive.',
            sourceType: DataSourceType.MINIO_BUCKET,
            status: DataSourceStatus.ACTIVE,
            isDefault: false,
            projectId: Number(route.params.projectId),
            createdAt: '2025-05-20T10:30:00Z',
        },
    ];
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.page-header {
    margin-bottom: vars.$margin-xlarge;
    h1 {
        font-size: vars.$font_size_xlarge;
        margin-bottom: vars.$padding-tiny;
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

.no-content-message {
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