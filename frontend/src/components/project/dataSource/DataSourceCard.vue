<template>
    <Card class="data-source-card" :class="`source-type-${dataSource.sourceType.toLowerCase()}`">
        <template #header>
            <div class="header-content">
                <div class="source-title">
                    <div class="source-icon">
                        <font-awesome-icon :icon="faDatabase" />
                    </div>
                    <h3 class="data-source-name">{{ dataSource.name }}</h3>
                </div>
                <div class="source-badges">
                    <span class="asset-count">{{ dataSource.assetCount || 0 }} Assets</span>
                    <span class="source-type-badge">{{ formattedSourceType }}</span>
                </div>
            </div>
        </template>

        <p class="data-source-description">
            {{ dataSource.description || "No description provided." }}
        </p>

        <template #footer>
            <div class="card-meta">
                <span>Type: {{ dataSource.sourceType.replace('_', ' ') }}</span>
                <span>Created: {{ formattedDate }}</span>
            </div>
            <div class="card-actions">
                <router-link :to="explorerUrl" custom v-slot="{ navigate }">
                    <Button @click="navigate" variant="primary" title="View assets in the Dataset Explorer" class="action-button">
                        <font-awesome-icon :icon="faEye" class="button-icon" />
                        View Assets
                    </Button>
                </router-link>
                <Button variant="success" @click="openUploadModal" class="action-button">
                    <font-awesome-icon :icon="faUpload" class="button-icon" />
                    Upload
                </Button>
            </div>
        </template>

        <UploadImagesModal
            v-model:isOpen="isUploadModalOpen"
            :dataSource="dataSource"
            @upload-complete="handleUploadComplete"
        />
    </Card>
</template>

<script setup lang="ts">
import {computed, ref} from "vue";
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {faEye, faUpload, faDatabase} from '@fortawesome/free-solid-svg-icons';
import type {DataSource} from "@/services/project/dataSource/dataSource.types";
import Card from "@/components/common/Card.vue";
import Button from "@/components/common/Button.vue";
import UploadImagesModal from "@/components/project/UploadImagesModal.vue";

const props = defineProps<{
    dataSource: DataSource;
}>();

const emit = defineEmits<{
    'assets-imported': [count: number];
}>();

const isUploadModalOpen = ref(false);

const explorerUrl = computed(() => `/projects/${props.dataSource.projectId}/data-explorer/${props.dataSource.id}`);

const formattedDate = computed(() => {
    return new Date(props.dataSource.createdAt).toLocaleDateString("en-US", {
        year: "numeric",
        month: "short",
        day: "numeric",
    });
});

const formattedSourceType = computed(() => {
    return props.dataSource.sourceType.replace('_', ' ').toLowerCase()
        .split(' ')
        .map(word => word.charAt(0).toUpperCase() + word.slice(1))
        .join(' ');
});

// Upload modal handlers
const openUploadModal = () => {
    isUploadModalOpen.value = true;
};

const handleUploadComplete = (count: number) => {
    isUploadModalOpen.value = false;
    emit('assets-imported', count);
    // TODO: Add success notification and auto-refresh asset count
};
</script>

<style scoped>
.data-source-card {
    transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
    border: 1px solid var(--color-gray-300);
}

.data-source-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.header-content {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    width: 100%;
}

.source-title {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex: 1;
}

.source-icon {
    border-radius: 0.5rem;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.125rem;
}

.source-type-minio_bucket .source-icon {
    background-color: var(--color-orange-500);
}

.source-type-s3_bucket .source-icon {
    background-color: var(--color-yellow-500);
}

.source-type-gsc_bucket .source-icon {
    background-color: var(--color-blue-500);
}

.data-source-name {
    font-size: 1.25rem;
    font-weight: 600;
    margin: 0;
    color: var(--color-gray-900);
}

.source-badges {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    align-items: flex-end;
}

.asset-count {
    font-size: 0.875rem;
    font-weight: bold;
    color: var(--color-blue-700);
    background-color: var(--color-blue-100);
    padding: 0.3em 0.75em;
    border-radius: 1rem;
}

.source-type-badge {
    font-size: 0.75rem;
    font-weight: 500;
    color: var(--color-gray-600);
    background-color: var(--color-gray-100);
    padding: 0.25em 0.5em;
    border-radius: 0.375rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.data-source-description {
    font-size: 1rem;
    margin: 1rem 0;
    flex-grow: 1;
    color: var(--color-gray-700);
    line-height: 1.5;
}

.card-meta {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    text-transform: capitalize;
    color: var(--color-gray-600);
    font-size: 0.875rem;
}

.card-actions {
    display: flex;
    flex-direction: row;
    gap: 0.75rem;
}

.action-button {
    display: flex;
    align-items: center;
    max-width: 100px;
    max-height: 45px;
    gap: 0.5rem;
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
    font-weight: 500;
    border-radius: 0.375rem;
    transition: all 0.2s ease-in-out;
}

.button-icon {
    font-size: 0.875rem;
}

@media (max-width: 768px) {
    .header-content {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
    }
    
    .source-badges {
        align-items: flex-start;
        flex-direction: row;
        justify-content: space-between;
    }
    
    .card-actions {
        flex-direction: column;
    }
    
    .action-button {
        justify-content: center;
    }
}
</style>