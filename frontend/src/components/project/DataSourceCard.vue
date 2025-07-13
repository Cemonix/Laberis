<template>
    <Card class="data-source-card">
        <template #header>
            <h3 class="data-source-name">{{ dataSource.name }}</h3>
            <span class="asset-count">{{ dataSource.assetCount || 0 }} Assets</span>
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
                    <Button @click="navigate" variant="secondary" title="View assets in the Dataset Explorer">
                        View Assets
                    </Button>
                </router-link>
                <Button variant="secondary" @click="openUploadModal">Upload</Button>
                <Button variant="secondary">Export</Button>
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
import type {DataSource} from "@/types/dataSource/dataSource";
import Card from "@/components/common/Card.vue";
import Button from "@/components/common/Button.vue";
import UploadImagesModal from "./UploadImagesModal.vue";

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

// Upload modal handlers
const openUploadModal = () => {
    isUploadModalOpen.value = true;
};

const handleUploadComplete = (count: number) => {
    isUploadModalOpen.value = false;
    emit('assets-imported', count);
};
</script>

<style scoped>
.data-source-name {
    font-size: 1.25rem;
}

.asset-count {
    font-size: 0.875rem;
    font-weight: bold;
    color: var(--color-gray-600);
    background-color: var(--color-gray-200);
    padding: 0.2em 0.6em;
    border-radius: 1em;
}

.data-source-description {
    font-size: 1rem;
    margin: 0;
    flex-grow: 1;
}

.card-meta {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    text-transform: capitalize;
}

.card-actions {
    display: flex;
    gap: 0.5rem;
}
</style>