<template>
    <div class="explorer-container">
        <header class="explorer-header">
            <div class="header-left">
                <nav class="breadcrumbs">
                    <Button 
                        variant="secondary" 
                        @click="navigateToProject"
                        class="home-button"
                        aria-label="Back to Project"
                    >
                        <font-awesome-icon :icon="faHome" />
                    </Button>
                    <span class="current-source-name">{{ dataSourceName }}</span>
                </nav>
            </div>
            <div class="header-right">
                <Button variant="secondary" @click="refreshAssets" :disabled="isLoading">
                    Refresh
                </Button>
                <Button variant="primary" @click="openUploadModal">
                    Upload Assets
                </Button>
            </div>
        </header>

        <main class="explorer-main-content">
            <div v-if="isLoading" class="loading-container">
                <div class="loading-message">Loading assets...</div>
            </div>
            
            <div v-else-if="error" class="error-container">
                <div class="error-message">{{ error }}</div>
                <Button variant="secondary" @click="loadAssets">Try Again</Button>
            </div>
            
            <div v-else class="content-container">
                <div v-if="assets.length === 0" class="empty-state">
                    <h3>No assets found</h3>
                    <p>This data source doesn't contain any assets yet.</p>
                    <Button variant="primary" @click="openUploadModal">
                        Upload Your First Asset
                    </Button>
                </div>
                
                <div v-else>
                    <div class="assets-header">
                        <div class="assets-count">
                            {{ paginationInfo.totalItems }} asset{{ paginationInfo.totalItems !== 1 ? 's' : '' }}
                        </div>
                        <div class="view-controls">
                            <!-- Add sorting/filtering controls here later -->
                        </div>
                    </div>
                    
                    <div class="asset-grid">
                        <AssetThumbnail 
                            v-for="asset in assets" 
                            :key="asset.id"
                            :asset="asset"
                        />
                    </div>
                    
                    <div v-if="hasMoreAssets" class="load-more-container">
                        <Button 
                            variant="secondary" 
                            @click="loadMoreAssets"
                            :disabled="isLoadingMore"
                        >
                            {{ isLoadingMore ? 'Loading...' : 'Load More' }}
                        </Button>
                    </div>
                </div>
            </div>
        </main>

        <!-- Upload Modal placeholder -->
        <!-- <UploadModal v-if="isUploadModalOpen" @close="closeUploadModal" /> -->
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, ref} from 'vue';
import {useRoute, useRouter} from 'vue-router';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {faHome} from '@fortawesome/free-solid-svg-icons';
import type {Asset, AssetListParams} from '@/types/asset';
import AssetThumbnail from '@/components/project/AssetThumbnail.vue';
import Button from '@/components/common/Button.vue';
import assetService from '@/services/api/assetService';
import {dataSourceService} from '@/services/api/dataSourceService';
import {useAlert} from '@/composables/useAlert';
import {AppLogger} from '@/utils/logger';

const logger = AppLogger.createComponentLogger('DataExplorerView');
const route = useRoute();
const router = useRouter();
const { showAlert } = useAlert();

const projectId = Number(route.params.projectId);
const dataSourceId = Number(route.params.dataSourceId);

// Reactive state
const dataSourceName = ref('Loading...');
const assets = ref<Asset[]>([]);
const isLoading = ref(false);
const isLoadingMore = ref(false);
const error = ref<string | null>(null);
const isUploadModalOpen = ref(false);

// Pagination state
const currentPage = ref(1);
const pageSize = ref(25);
const paginationInfo = ref({
    totalItems: 0,
    totalPages: 0,
    currentPage: 1
});

// Computed properties
const hasMoreAssets = computed(() => 
    paginationInfo.value.currentPage < paginationInfo.value.totalPages
);

// Methods
const navigateToProject = () => {
    router.push(`/projects/${projectId}`);
};

const loadDataSource = async () => {
    try {
        const dataSource = await dataSourceService.getDataSource(projectId, dataSourceId);
        dataSourceName.value = dataSource.name;
        logger.info(`Loaded data source: ${dataSource.name}`);
    } catch (err) {
        logger.error('Failed to load data source', err);
        dataSourceName.value = 'Unknown Data Source';
        await showAlert('Error', 'Failed to load data source information.');
    }
};

const loadAssets = async (page: number = 1, append: boolean = false) => {
    if (!append) {
        isLoading.value = true;
        error.value = null;
    } else {
        isLoadingMore.value = true;
    }

    try {
        const params: AssetListParams = {
            pageNumber: page,
            pageSize: pageSize.value,
            dataSourceId: dataSourceId,
            sortBy: 'createdAt',
            sortOrder: 'desc'
        };

        const response = await assetService.getAssets(projectId, params);

        // TODO: Handle empty response gracefully

        if (append) {
            assets.value.push(...response.data);
        } else {
            assets.value = response.data;
        }

        paginationInfo.value = {
            totalItems: response.totalItems,
            totalPages: response.totalPages,
            currentPage: response.currentPage
        };

        currentPage.value = response.currentPage;
        
        logger.info(`Loaded ${response.data.length} assets (page ${page})`);
    } catch (err) {
        logger.error('Failed to load assets', err);
        error.value = 'Failed to load assets. Please try again.';
        
        if (!append) {
            await showAlert('Error', 'Failed to load assets. Please try again.');
        }
    } finally {
        isLoading.value = false;
        isLoadingMore.value = false;
    }
};

const loadMoreAssets = async () => {
    if (hasMoreAssets.value && !isLoadingMore.value) {
        await loadAssets(currentPage.value + 1, true);
    }
};

const refreshAssets = async () => {
    currentPage.value = 1;
    await loadAssets(1, false);
};

const openUploadModal = () => {
    isUploadModalOpen.value = true;
    // TODO: Implement upload modal
    logger.info('Upload modal would open here');
};

// Lifecycle
onMounted(async () => {
    await Promise.all([
        loadDataSource(),
        loadAssets()
    ]);
});
</script>

<style lang="scss" scoped>
.explorer-container {
    display: flex;
    flex-direction: column;
    height: 100vh;
    background-color: var(--color-gray-100);
}

.explorer-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.5rem;
    background-color: var(--color-white);
    border-bottom: 1px solid var(--color-gray-400);
    flex-shrink: 0;
    box-shadow: 0 1px 3px rgba(var(--color-black), 0.05);
}

.breadcrumbs {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 1.25rem;

    .home-button {
        min-width: 32px;
        height: 32px;
        padding: 0;
        margin-right: 0.5rem;
        font-size: 1.2rem;
        font-weight: bold;
    }

    a {
        color: var(--color-primary);
        text-decoration: none;
        &:hover {
            text-decoration: underline;
        }
    }
    
    .separator {
        color: var(--color-gray-600);
        margin: 0 0.25rem;
    }

    .current-source-name {
        color: var(--color-gray-800);
        font-weight: 600;
    }
}

.header-right {
    display: flex;
    gap: 1rem;
}

.explorer-main-content {
    flex-grow: 1;
    overflow-y: auto;
    padding: 1.5rem;
}

.loading-container,
.error-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 50vh;
    gap: 1rem;
}

.loading-message {
    font-size: 1.25rem;
    color: var(--color-gray-600);
}

.error-message {
    font-size: 1.25rem;
    color: var(--color-error);
    text-align: center;
}

.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 50vh;
    gap: 1.5rem;
    text-align: center;

    h3 {
        font-size: 1.5rem;
        color: var(--color-gray-800);
        margin: 0;
    }

    p {
        font-size: 1.25rem;
        color: var(--color-gray-600);
        margin: 0;
        max-width: 50ch;
    }
}

.assets-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.5rem;
    padding-bottom: 1rem;
    border-bottom: 1px solid var(--color-gray-400);
}

.assets-count {
    font-size: 1.25rem;
    font-weight: 600;
    color: var(--color-gray-800);
}

.asset-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 1.5rem;
    margin-bottom: 2rem;
}

.load-more-container {
    display: flex;
    justify-content: center;
    padding: 1.5rem 0;
}

// Responsive adjustments
@media (max-width: 768px) {
    .explorer-header {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
    }

    .breadcrumbs {
        font-size: 1rem;
    }

    .header-right {
        justify-content: center;
    }

    .asset-grid {
        grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
        gap: 1rem;
    }

    .explorer-main-content {
        padding: 1rem;
    }
}
</style>