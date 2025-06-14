<template>
    <div class="explorer-container">
        <header class="explorer-header">
            <div class="header-left">
                <nav class="breadcrumbs">
                    <router-link :to="`/projects/${projectId}`">Projects</router-link>
                    <span>/</span>
                    <router-link :to="`/projects/${projectId}/data-sources`">Data Sources</router-link>
                    <span>/</span>
                    <span class="current-source-name">{{ dataSourceName }}</span>
                </nav>
            </div>
            <div class="header-right">
                <Button variant="primary">Upload Assets</Button>
            </div>
        </header>

        <main class="explorer-main-content">
            <div class="asset-grid">
                <AssetThumbnail 
                    v-for="asset in assets" 
                    :key="asset.assetId"
                    :asset="asset"
                />
            </div>
            <div class="load-more-container">
                <Button variant="secondary">Load More</Button>
            </div>
        </main>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import type { Asset } from '@/types/project/asset';
import { AssetStatus } from '@/types/project/asset';
import AssetThumbnail from '@/components/project/AssetThumbnail.vue';
import Button from '@/components/common/Button.vue';

const route = useRoute();
const projectId = route.params.projectId as string;
const dataSourceId = Number(route.params.dataSourceId);

const dataSourceName = ref('Loading...');
const assets = ref<Asset[]>([]);

const mockDataSources = [
    { id: 1, name: 'default-source' },
    { id: 2, name: 'archive-2024-images' },
];

const mockAssets: Asset[] = [
    { assetId: 101, dataSourceId: 1, name: 'image_001.jpg', path: '/', thumbnailUrl: 'https://picsum.photos/400/250?random=1', status: AssetStatus.IMPORTED, annotationsCount: 0, createdAt: '2025-06-01T10:00:00Z' },
    { assetId: 102, dataSourceId: 1, name: 'image_002.png', path: '/', thumbnailUrl: 'https://picsum.photos/400/250?random=2', status: AssetStatus.ANNOTATED, annotationsCount: 3, createdAt: '2025-06-01T10:05:00Z' },
    { assetId: 103, dataSourceId: 1, name: 'photo-of-a-very-long-file-name-to-test-ellipsis.tiff', path: '/', thumbnailUrl: 'https://picsum.photos/400/250?random=3', status: AssetStatus.REVIEW_IN_PROGRESS, annotationsCount: 5, createdAt: '2025-06-02T11:00:00Z' },
    { assetId: 104, dataSourceId: 1, name: 'image_004.jpg', path: '/', thumbnailUrl: 'https://picsum.photos/400/250?random=4', status: AssetStatus.REVIEW_ACCEPTED, annotationsCount: 5, createdAt: '2025-06-03T14:00:00Z' },
];

onMounted(() => {
    // Simulate fetching data
    const currentSource = mockDataSources.find(ds => ds.id === dataSourceId);
    dataSourceName.value = currentSource ? currentSource.name : 'Unknown Data Source';
    
    // Simulate fetching assets for the current data source
    assets.value = mockAssets.filter(asset => asset.dataSourceId === dataSourceId || asset.dataSourceId === 1); // Show some assets for demo
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.explorer-container {
    display: flex;
    flex-direction: column;
    height: 100%;
    background-color: vars.$color-gray-100;
}

.explorer-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: vars.$padding-medium vars.$padding-large;
    background-color: vars.$color-white;
    border-bottom: vars.$border-width solid vars.$theme-border;
    flex-shrink: 0;
}

.breadcrumbs {
    display: flex;
    align-items: center;
    gap: vars.$gap-small;
    font-size: vars.$font_size_large;

    a {
        color: vars.$color-primary;
        text-decoration: none;
        &:hover {
            text-decoration: underline;
        }
    }
    
    span {
        color: vars.$theme-text-light;
    }

    .current-source-name {
        color: vars.$theme-text;
        font-weight: bold;
    }
}

.explorer-main-content {
    flex-grow: 1;
    overflow-y: auto;
    padding: vars.$padding-large;
}

.asset-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
    gap: vars.$gap-large;
}

.load-more-container {
    display: flex;
    justify-content: center;
    padding: vars.$padding-large 0;
}
</style>